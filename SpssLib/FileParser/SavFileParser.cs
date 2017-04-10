using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.IO;
using SpssLib.Compression;
using SpssLib.FileParser.Records;
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
            IRecord record;
            IRecordParser recordParser;

            // Read the header record and validate this file
            try
            {
                readRecordType = _reader.ReadRecordType();
                if (readRecordType != RecordType.HeaderRecord)
                {
                    throw new SpssFileFormatException("No header record is present. A header record is required. Is this a valid SPSS file?");
                }
                recordParser = parsers.GetParser(readRecordType);
                record = recordParser.ParseRecord(_reader);
                record.RegisterMetadata(MetaData);
                records.Add(record);
            }
            catch (EndOfStreamException)
            {
                throw new SpssFileFormatException("No header record is present. A header record is required. Is your file empty?");
            }

            // Read the rest of the records
            do
            {
                readRecordType = _reader.ReadRecordType();
                recordParser = parsers.GetParser(readRecordType);
                record = recordParser.ParseRecord(_reader);
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

        public IEnumerable<IEnumerable<object>> ParsedDataRecords => DataRecords.Select(RecordToObjects);

        public byte[][] ReadNextDataRecord()
        {
            byte[][] record = new byte[MetaData.VariableRecords.Count][];
            for (int i = 0; i < MetaData.VariableRecords.Count; i++)
			{   // TODO check unexpected eof?
			    record[i]= _reader.ReadBytes(Constants.BLOCK_BYTE_SIZE);
                if (record[i].Length < Constants.BLOCK_BYTE_SIZE)
                {
                    return null;
                }
			}
            return record;            
        }

        /// <summary>
        /// Convert a row of raw data to proper objects. (strings or doubles)
        /// </summary>
        /// <param name="record">The complete row data, as an array of byte[8] (the block of a single VariableRecord)</param>
        /// <returns>The enumeration of objects for this row</returns>
        public IEnumerable<object> RecordToObjects(byte[][] record)
        {
            // Decoder for strings
            Decoder dec = MetaData.DataEncoding.GetDecoder();
            // Buffer to write the decoded chars to
            var charBuffer = new char[MetaData.DataEncoding.GetMaxCharCount(256)];
            // String builder to get the full string result (mainly for VLS)
            StringBuilder sBuilder = new StringBuilder();
            // The dictionary with the lengths for each VLS variable
            var veryLongStrings = MetaData.VeryLongStringsDictionary;

            // All raw variable records and it's count (i's also the count of 8 bytes blocks in the row)
            var variableRecords = MetaData.VariableRecords;
            var variableCount = variableRecords.Count;

            // Read the values, guided by it's VariableRecord
            for (int variableIndex = 0; variableIndex < variableCount; )
            {
                // Variable record that correspond to the current 8 bytes block
                VariableRecord variableRecord;
                // Currrent 8 bytes block
                byte[] element = MoveNext(record, variableRecords, out variableRecord, ref variableIndex);

                // Numeric variable (also date, etc)
                if (variableRecord.Type == 0)
                {
                    // Convert value to double and check Sysmiss
                    yield return ParseDoubleValue(element);
                }
                // String variable
                else if (variableRecord.Type > 0)
                {
                    // Count of segments of up to 255 bytes. 1 for not VLS
                    int segments = 1;
                    // If VLS, calculate total count of segments according to the value for this var in the VLS dictionary
                    if (veryLongStrings.ContainsKey(variableRecord.Name))
                    {
                        segments = VariableRecord.GetLongStringSegmentsCount(veryLongStrings[variableRecord.Name]);
                    }

                    // Ok, so let's start reading all the string...
                    do
                    {
                        // The index of the char buffer. How many chars have been read
                        var bufferIndex = 0;
                        // The length of this segment, in bytes
                        var length = variableRecord.Type;
                        // Count of bytes read of the string
                        int bytesRead = element.Length;

                        // The index for the 8 byte block inside the VLS segment
                        int inSegmentIndex = 1;

                        // Start reading the segment
                        // Decode the characters into the charBuffer array and increment the index
                        bufferIndex += dec.GetChars(element, 0, element.Length, charBuffer, bufferIndex, false);
                        
                        // While we haven't read all bytes, is still a SCR and not the end of the row
                        while (bytesRead < length && variableRecords[variableIndex].Type == -1 && variableIndex < variableCount)
                        {
                            // Read next block
                            element = MoveNext(record, variableRecords, out variableRecord, ref variableIndex);
                            
                            // When we get to the 32nd segment, we have to ignote the 8th byte, as it is the 
                            // number 256 and segments are only 255. If this byte is not skiped, spaces will
                            // appear where they shouldn't be.
                            int lengthRead = element.Length;
                            if (++inSegmentIndex == 32)
                            {
                                // Substract one from the read length to ignore the last bye
                                lengthRead--;
                                // Reset the counter for a new segment
                                inSegmentIndex = 0;
                            }

                            // Decode the characters into the charBuffer array and increment the index
                            bufferIndex += dec.GetChars(element, 0, lengthRead, charBuffer, bufferIndex, false);
                            bytesRead += element.Length;
                        }
                        
                        // If the type of variable changed before the end of the string length or before the end of the file
                        // there must be something wrong
                        if (length > 8 && variableRecord.Type != -1)
                            throw new SpssFileFormatException("Long string terminated early. "+
                                "There must be missing some of the needed string continuation record. Dictionary index "+
                                variableIndex);
                        
                        // Flush the buffer of the decoder
                        bufferIndex += dec.GetChars(element, 0, 0, charBuffer, bufferIndex, true);
                        // take the segment's string we were building (the buffer up to the writen index)
                        sBuilder.Append(charBuffer, 0, bufferIndex);
                        // If there afe more records, move next and continue
                        if (--segments > 0)
                        {
                            element = MoveNext(record, variableRecords, out variableRecord, ref variableIndex);
                        }
                        else
                        {
                            // if all segments are processed, exit the loop
                            break;
                        }
                    } while (true);
                    
                    // Clear decoder for future use
                    dec.Reset();    
                    // Get full string and clear the string builder
                    var s = sBuilder.ToString();
                    sBuilder.Clear();

                    // Finally, return the string
                    yield return s;
                }
                // String Continuation record (either we read something wrong or the file is not very well formed)
                else if(variableRecord.Type == -1)
                {
                    throw new SpssFileFormatException("Unexpected string continuation record. To start reading the record must be either string or numeric (dates, etc). "+
                        "Dictionary index "+variableIndex);
                }
                // I don't know any more VariableRecord's types
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

        public Collection<Variable> Variables
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

        private Collection<Variable> _variables;
        
        /// <summary>
        /// Creates a <see cref="Variable"/> object with it's actual informantion
        /// </summary>
        /// <param name="variableIndex">The actual index of the variable</param>
        /// <param name="dictionaryIndex">The index of the varible's <see cref="VariableRecord"/></param>
        /// <param name="metaData">The parsed metada with all needed info from the file</param>
        /// <param name="length">The string lenght in bytes (only needed for string vars)</param>
        /// <param name="segmentIndex">The variable index, counting also the extra segments of Very Long Strings</param>
        /// <returns>The variable with all it's information inside</returns>
        private Variable GetVariable(int variableIndex, int dictionaryIndex, MetaData metaData, int length, int segmentIndex)
        {
            // Get variable record data:
            var variableRecord = metaData.VariableRecords[dictionaryIndex];
            var variableName = GetLongName(metaData, variableRecord);

            var variable = new Variable(variableName)
                           {
                               Index = variableIndex,
                               PrintFormat = variableRecord.PrintFormat,
                               WriteFormat = variableRecord.WriteFormat,
                               Type = variableRecord.Type == 0 ? DataType.Numeric : DataType.Text,
                               MissingValueType = (MissingValueType) variableRecord.MissingValueType,
                               Label = variableRecord.HasVariableLabel ? variableRecord.Label : null
                           };

            for (int i = 0; i < variableRecord.MissingValues.Count && i < variable.MissingValues.Length; i++)
            {
                variable.MissingValues[i] = variableRecord.MissingValues[i];
            }

            
            if (variable.Type == DataType.Text)
            {
                int longLength;
                if (metaData.VeryLongStringsDictionary.TryGetValue(variableRecord.Name, out longLength))
                {
                    variable.TextWidth = longLength;
                }
                else
                {
                    variable.TextWidth = length;
                }
            }

            // TODO: There can be one value label for multiple variables, we might want to only cerate one and reference it from all variables
            // Get value labels:
            var valueLabelRecord = metaData.ValueLabelRecords.FirstOrDefault(record => record.Variables.Contains(dictionaryIndex + 1));
            if (valueLabelRecord != null)
            {
                foreach (var label in valueLabelRecord.Labels)
                {
                    var key = BitConverter.ToDouble(label.Key, 0);
                    var value = label.Value.Replace("\0", string.Empty).Trim();

                    if (variable.ValueLabels.ContainsKey(key))
                    {
                        var existingValue = variable.ValueLabels[key];
                        throw new SpssFileFormatException(
                            $"Variable {variableName} has a duplicate key for value label {key}, found values \"{existingValue}\" and \"{value}\"", dictionaryIndex);
                    }

                    variable.ValueLabels.Add(key, value);
                }
            }

            // Get display info:
            if (metaData.VariableDisplayParameters  != null)
            {
                var displayInfo = metaData.VariableDisplayParameters[segmentIndex];
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

            return variable;
        }

        private static string GetLongName(MetaData metaData, VariableRecord variableRecord)
        {
            string longName;
            // Look for the right (long) name if there is one
            if (metaData.LongVariableNames != null
                && metaData.LongVariableNames.Dictionary.TryGetValue(variableRecord.Name, out longName))
            {
                return longName;
            }
            // If not, just return the short name
            return variableRecord.Name;
        }


        /// <summary>
        /// Fills the variables collection with just the actual variables (no string continuation records or very long
        /// strings extra segments)
        /// </summary>
		private void GetVariablesFromRecords()
        {
            _variables = new Collection<Variable>();
            // Get the longs strings dictionary
		    var veryLongStrings = MetaData.VeryLongStringsDictionary;
            
            // Ammount of variables to jump (to skip variable continuation records and additional very long string segments)
            int delta;
            // Index of variable with out string continuation records but INCLUDING very long string record variables (segments)
            // This will be used for things like finding the VariableDisplayInfoRecord
		    int segmentIndex = 0;
            // Index of variable with out string continuation records AND very long string record variables (segments)
            int variableIndex = 0;
            // Dictionary index is the VariableRecord index that contains the header info for this variable
		    for (int dictionaryIndex = 0; dictionaryIndex < MetaData.VariableRecords.Count; dictionaryIndex+=delta)
		    {
                var record = MetaData.VariableRecords[dictionaryIndex];
                // If it's a string continuation record (SCR). This ones should have been skiped.
		        if (record.Type < 0)
		        {
                    throw  new SpssFileFormatException("String continuation record out of place. Dictonary index "+dictionaryIndex);
		        }

                // Actual byte lenght for variable
		        int length;
                // Ammount of segments (for VeryLongString variables, 1 for all other vars).
		        int segments;
                if (veryLongStrings.ContainsKey(record.Name))
                {
                    // Variable is a VeryLongString variable
                    // Take actual length from VLSR dictionary
                    length = veryLongStrings[record.Name];
                    // Calculate the ammount of segments and the amount of VariableRecords to skip (SCR)
                    segments = VariableRecord.GetLongStringSegmentsCount(length);
                    delta = VariableRecord.GetLongStringContinuationRecordsCount(length);
                }
                else
                {
                    // Variable is NOT a VeryLongString variable
                    // numeric type is 0 so lenght is 1, > 0 for the lenght of strings
                    length = record.Type == 0 ? 1 : record.Type;
                    // This ones have only one segment
                    segments = 1;
                    // Skip the string continuation records if any, or just move next.
                    delta =  VariableRecord.GetStringContinuationRecordsCount(length);
                }
                
                _variables.Add(GetVariable(variableIndex++, dictionaryIndex, MetaData, length, segmentIndex));
                // Increment the segment count by how many segments this var had.
		        segmentIndex += segments;
		    }
        }
    }
}
