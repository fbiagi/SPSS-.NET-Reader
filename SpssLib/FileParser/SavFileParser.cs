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

        private BinaryReader reader;
        private Stream dataRecordStream;
        private long dataStartPosition = 0;

        public SavFileParser(Stream fileStream)
        {
            this.Stream = fileStream;
        }

        public void ParseMetaData()
        {
            reader = new BinaryReader(Stream, Encoding.ASCII);

            var parsers = new ParserProvider();
            IList<IRecord> records = new List<IRecord>(1000);

            MetaData = new MetaData();
            
            RecordType readRecordType;
            do
            {
                readRecordType = ReadRecordType();
                var recordParser = parsers.GetParser(readRecordType);
                // TODO pass the metadata to the parsers & records.fillRecord to self add into it
                var record = recordParser.ParseRecord(reader);
                record.RegisterMetadata(MetaData);
                records.Add(record);

            } while (readRecordType != RecordType.End);
            

            try
	        {
				this.dataStartPosition = this.Stream.Position;
	        }
	        catch (NotSupportedException)
	        {
				// Some stream types don't support the Position property (CryptoStream...)
				this.dataStartPosition = 0;
	        }
            
            SetDataRecordStream();
            MetaDataParsed = true;
        }

        private RecordType ReadRecordType()
        {
            int recordTypeNum = reader.ReadInt32();
            if (!Enum.IsDefined(typeof (RecordType), recordTypeNum))
            {
                throw new SpssFileFormatException("Record type not recognized: "+recordTypeNum);
            }

            return (RecordType)Enum.ToObject(typeof(RecordType), recordTypeNum);
        }


        private void SetDataRecordStream()
        {
            dataRecordStream = MetaData.HeaderRecord.Compressed ? 
                new DecompressedDataStream(Stream, MetaData.HeaderRecord.Bias, MetaData.SystemMissingValue) 
                : Stream;
            reader = new BinaryReader(dataRecordStream, Encoding.ASCII);
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
                    if (dataStartPosition != 0)
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
						if (position != dataStartPosition)
						{
							if (Stream.CanSeek)
							{
								Stream.Seek(dataStartPosition, 0);
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
						dataStartPosition = -1;
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
			{
			    record[i]= reader.ReadBytes(Constants.BlockByteSize);
                if (record[i].Length < Constants.BlockByteSize)
                {
                    return null;
                }
			}
            return record;            
        }

		public object ValueToObject(byte[] value, VariableRecord variable)
        {
            if (variable.Type == 0)
            {
                var doubleValue = BitConverter.ToDouble(value, 0);
                if (doubleValue == SysmisValue)
                {
                    return null;
                }
                else
                {
                    return doubleValue;
                }
            }
            else
            {
                return Encoding.ASCII.GetString(value);
            }
        }

        public IEnumerable<object> RecordToObjects(byte[][] record)
        {
            StringBuilder stringBuilder = new StringBuilder();
            bool buildingString = false;
            int variableIndex = 0;
            int stringLength = 0;

            foreach (var variableRecord in this.MetaData.VariableRecords)
            {
                byte[] element = record[variableIndex++];

                if (buildingString && variableRecord.Type != -1)
                {
                    // return the complete string we were building
                    yield return (stringBuilder.ToString()).Substring(0, stringLength);

                    // Clear:
                    stringBuilder.Length = 0;
                    buildingString = false;
                }

                if (variableRecord.Type == 0)
                {
                    // Return numeric value
                    var value =  BitConverter.ToDouble(element, 0);
                    if (value == SysmisValue)
                    {
                        yield return null;
                    }
                    else
                    {
                        yield return value;
                    }
                }
                else
                {
                    if (variableRecord.Type > 0)
                        stringLength = variableRecord.Type;
                        // Add string to string we were building
                    stringBuilder.Append(Encoding.ASCII.GetString(element));
                    buildingString = true;
                }
            }
            // return the complete string we were building
            if (buildingString)
            {
                yield return stringBuilder.ToString();
            }
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
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.reader != null)
                {
                    this.reader.Close();
                    this.reader = null;
                }
                if (this.Stream != null)
                {
                    this.Stream.Close();
                    this.Stream = null;
                }
                if (this.dataRecordStream != null)
                {
                    this.dataRecordStream.Close();
                    this.dataRecordStream = null;
                }
            }
        }

        public VariablesCollection Variables
        {
            get
            {
                if (this.MetaData == null)
                    this.ParseMetaData();
                if (this.variables == null)
                {
                    GetVariablesFromRecords();
                }
                return this.variables;
            }
        }

        private VariablesCollection variables;

        private Variable GetVariable(int variableIndex, int dictionaryIndex, FileParser.MetaData metaData)
        {
            var variable = new Variable();
            variable.Index = variableIndex;

            // Get variable record data:
            var variableRecord = metaData.VariableRecords[dictionaryIndex];
            variable.ShortName = variableRecord.Name;
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
            

            // Get (optional) long variable name:
            if (metaData.LongVariableNames != null)
            {
                var longNameDictionary = metaData.LongVariableNames.LongNameDictionary;
                if (longNameDictionary.ContainsKey(variable.ShortName.Trim()))
                {
                    variable.Name = longNameDictionary[variable.ShortName.Trim()].Trim();
                }
                else
                {
                    variable.Name = variable.ShortName.Trim();
                }
            }
            else
            {
                variable.Name = variable.ShortName.Trim();
            }

            // TODO digest very long string info.
            return variable;
        }

        public double SysmisValue { get; set; }

		private void GetVariablesFromRecords()
        {
            this.variables = new VariablesCollection();

            int dictionaryIndex = 0;
            int variableIndex = 0;
            foreach (var variableRecord in this.MetaData.VariableRecords)
            {
                if (variableRecord.Type >= 0)
                {
                    variables.Add(GetVariable(variableIndex, dictionaryIndex, this.MetaData));
                    variableIndex++;
                }
                dictionaryIndex++;
            }
        }
    }
}
