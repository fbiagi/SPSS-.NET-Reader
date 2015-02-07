using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SpssLib.Compression;
using SpssLib.FileParser.Records;
using System.Data;
using SpssLib.SpssDataset;

namespace SpssLib.FileParser
{
    public class SavFileParser: IDisposable
    {
        public Stream Stream { get; private set; }

        public bool MetaDataParsed { get; private set; }
        public MetaData MetaData { get; private set; }
        public double SysmisValue { get; set; }

        private BinaryReader _reader;
        private Stream _dataRecordStream;
        private long _dataStartPosition;

        public SavFileParser(Stream fileStream)
        {
            Stream = fileStream;
        }

        public void ParseMetaData()
        {
            _reader = new BinaryReader(Stream, Encoding.ASCII);

            var parsers = new ParserProvider();
            IList<IRecord> records = new List<IRecord>(1000);

            MetaData = new MetaData();
            
            RecordType readRecordType;
            do
            {
                readRecordType = _reader.ReadRecordType();
                var recordParser = parsers.GetParser(readRecordType);
                var record = recordParser.ParseRecord(_reader);
                record.RegisterMetadata(MetaData);
                records.Add(record);
            } while (readRecordType != RecordType.End);
            

            try
	        {
				_dataStartPosition = Stream.Position;
	        }
	        catch (NotSupportedException)
	        {
				// Some stream types don't support the Position property (CryptoStream...)
				_dataStartPosition = 0;
	        }
            
            SetDataRecordStream();
            MetaDataParsed = true;
        }

        private void SetDataRecordStream()
        {
            _dataRecordStream = MetaData.HeaderRecord.Compressed ? 
                new DecompressedDataStream(Stream, MetaData.HeaderRecord.Bias, MetaData.SystemMissingValue) 
                : Stream;
            _reader = new BinaryReader(_dataRecordStream, Encoding.ASCII);
        }

        public IEnumerable<byte[][]> DataRecords
        {
            get
            {
                if (!MetaDataParsed)
                {
                    ParseMetaData();
                }
                lock (Stream)
                {
                    // dataStartPosition == 0 -> Stream.Position not suported
                    if (_dataStartPosition != 0)
                    {
	                    long position;
	                    try
	                    {
		                    position = Stream.Position;
	                    }
	                    catch (NotSupportedException ex)
	                    {
							throw new NotSupportedException("Re-reading the data is not allowed on this stream because it doesn't support position.", ex);
	                    }
						if (position != _dataStartPosition)
						{
							if (Stream.CanSeek)
							{
								Stream.Seek(_dataStartPosition, 0);
							}
							else
							{
								throw new NotSupportedException("Re-reading the data is not allowed on this stream because it doesn't allow seeking.");
							}
						}
					}
					else
					{
						// If position could not be read initialy, set as -1 to avoid start reading the records again with out rewinding the stream
						_dataStartPosition = -1;
					}

                    byte[][] record = ReadNextDataRecord();
                    while (record != null)
                    {
                        yield return record;
                        record = ReadNextDataRecord();
                    }
                }
            }
        }

        public IEnumerable<IEnumerable<object>> ParsedDataRecords
        {
            get
            {
                foreach (var rawrecord in DataRecords)
                {
                    yield return RecordToObjects(rawrecord);
                }
            }
        }

        public byte[][] ReadNextDataRecord()
        {
            byte[][] record = new byte[MetaData.VariableRecords.Count][];
            for (int i = 0; i < MetaData.VariableRecords.Count; i++)
			{   // TODO check unexpected eof?
			    record[i]= _reader.ReadBytes(Constants.BlockByteSize);
                if (record[i].Length < Constants.BlockByteSize)
                {
                    return null;
                }
			}
            return record;            
        }

        public IEnumerable<object> RecordToObjects(byte[][] record)
        {
            Decoder dec = MetaData.DataEncoding.GetDecoder();
            var charBuffer = new char[MetaData.DataEncoding.GetMaxCharCount(256)];

            var variableRecords = MetaData.VariableRecords;
            var variableCount = variableRecords.Count;

            for (int variableIndex = 0; variableIndex < variableCount; )
            {
                VariableRecord variableRecord;
                byte[] element = MoveNext(record, variableRecords, out variableRecord, ref variableIndex);

                if (variableRecord.Type == 0)
                {
                    // Convert value to double and check Sysmiss
                    yield return ParseDoubleValue(element);
                }
                else if (variableRecord.Type > 0)
                {

                    var bufferIndex = 0;
                    // String variable starts
                    var length = variableRecord.Type;
                    int bytesRead = element.Length;

                    do
                    {
                        // Decode the characters into the charBuffer array and increment the index
                        bufferIndex += dec.GetChars(element, 0, element.Length, charBuffer, bufferIndex, false);
                        element = MoveNext(record, variableRecords, out variableRecord, ref variableIndex);
                        bytesRead += element.Length;
                    } while (bytesRead < length && variableRecord.Type == -1 && variableIndex < variableCount);
                        
                    // If the type of variable changed before the end of the string length or before the end of the file
                    // there must be something wrong
                    if(variableRecord.Type != -1)
                        throw new SpssFileFormatException("Wrong termination for string. Dictionary index "+variableIndex);
                        
                    // Decode last chars and flush decoder buffer. In case the bytes read where more than  the lenght,
                    // we must read just the remainig bytes on the 8 byte array, otherwise, just read all.
                    bufferIndex += dec.GetChars(element, 0, bytesRead > length 
                                                                ? length - bytesRead + element.Length
                                                                : element.Length, 
                                                charBuffer, bufferIndex, true);

                    // return the complete string we were building (the buffer up to the writen index)
                    yield return new string(charBuffer, 0, bufferIndex);

                    // Clear decoder for future use
                    dec.Reset();
                }
                else if(variableRecord.Type == -1)
                {
                    throw new SpssFileFormatException("Unexpected long string variable record");
                }
                else
                {
                    throw new SpssFileFormatException("Unrecognized variable type: "+variableRecord.Type);
                }
            }
        }

        private static byte[] MoveNext(byte[][] record, IList<VariableRecord> variableRecords, out VariableRecord variableRecord, ref int variableIndex)
        {
            variableRecord = variableRecords[variableIndex];
            return record[variableIndex++];
        }

        /// <summary>
        /// Converts the byte pattern to it's double representation and compares it to 
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private object ParseDoubleValue(byte[] element)
        {
            var value = BitConverter.ToDouble(element, 0);
            // ReSharper disable CompareOfFloatsByEqualityOperator SysMiss is an exact value
            if (value == MetaData.SystemMissingValue)
            // ReSharper restore CompareOfFloatsByEqualityOperator
            {
                return null;
            }
            return value;
        }

        [Obsolete("Use SpssDataset constructor directly")]
        public SpssDataset.SpssDataset ToSpssDataset()
        {
            return new SpssDataset.SpssDataset(this);
        }

		[Obsolete("Use SpssDataReader constructor directly")]
        public IDataReader GetDataReader()
        {
            return new DataReader.SpssDataReader(this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_reader != null)
                {
                    _reader.Close();
                    _reader = null;
                }
                if (Stream != null)
                {
                    Stream.Close();
                    Stream = null;
                }
                if (_dataRecordStream != null)
                {
                    _dataRecordStream.Close();
                    _dataRecordStream = null;
                }
            }
        }

        public VariablesCollection Variables
        {
            get
            {
                if (MetaData == null)
                    ParseMetaData();
                if (_variables == null)
                {
                    GetVariablesFromRecords();
                }
                return _variables;
            }
        }

        private VariablesCollection _variables;

        private Variable GetVariable(int variableIndex, int dictionaryIndex, MetaData metaData)
        {
            var variable = new Variable();
            variable.Index = variableIndex;

            // Get variable record data:
            var variableRecord = metaData.VariableRecords[dictionaryIndex];
            variable.Label = variableRecord.HasVariableLabel ? variable.Label = variableRecord.Label : null;
	        variable.MissingValueType = variableRecord.MissingValueType;
	        for (int i = 0; i < variableRecord.MissingValues.Count && i < variable.MissingValues.Length; i++)
	        {
		        variable.MissingValues[i] = variableRecord.MissingValues[i];
	        }

            variable.PrintFormat = variableRecord.PrintFormat;
            variable.WriteFormat = variableRecord.WriteFormat;
            variable.Type = variableRecord.Type == 0 ? DataType.Numeric : DataType.Text;
            if (variable.Type == DataType.Text)
            {
                variable.TextWidth = variableRecord.Type;
            }

            // Get value labels:
            var valueLabelRecord = metaData.ValueLabelRecords.FirstOrDefault(record => record.Variables.Contains(dictionaryIndex + 1));
            
            if (valueLabelRecord != null)
            {
                foreach (var label in valueLabelRecord.Labels)
                {
                    variable.ValueLabels.Add(BitConverter.ToDouble(label.Key, 0), label.Value.Replace("\0", string.Empty).Trim());
                }
            }

            // Get display info:
            if (metaData.VariableDisplayParameters  != null)
            {
                var displayInfo = metaData.VariableDisplayParameters[variableIndex];
                variable.Alignment = displayInfo.Alignment;
                variable.MeasurementType = displayInfo.MeasurementType;
                variable.Width = displayInfo.Width; // TODO this field might not be present, check this and use the printFormat's
            }
            else
            {
                // defaults
                variable.Alignment = Alignment.Right;
                variable.MeasurementType = MeasurementType.Scale;
                variable.Width = variable.PrintFormat.FieldWidth;
            }

            variable.Name = variableRecord.Name;
            string longName;
            // Look for the right name
            if (metaData.LongVariableNames != null 
                    && metaData.LongVariableNames.Dictionary.TryGetValue(variable.Name, out longName))
            {
                variable.Name = longName;
            }

            // TODO digest very long string info.
            return variable;
        }

		private void GetVariablesFromRecords()
        {
            _variables = new VariablesCollection();

            int dictionaryIndex = 0;
            int variableIndex = 0;
            foreach (var variableRecord in MetaData.VariableRecords)
            {
                if (variableRecord.Type >= 0)
                {
                    _variables.Add(GetVariable(variableIndex, dictionaryIndex, MetaData));
                    variableIndex++;
                }
                dictionaryIndex++;
            }
        }
    }
}
