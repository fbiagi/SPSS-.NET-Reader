using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using SpssLib.SpssDataset;
using System.Collections.ObjectModel;
using System.Security.Cryptography;

namespace SpssLib.FileParser.Records
{
    /// <summary>
    /// Represents the record data for a variable, this contains name, type, labels, value type, formatting, missing value type, print format and write format
    /// </summary>
    public class VariableRecord : EncodeEnabledRecord, IRecord
    {
        private byte[] _nameRaw;
        private byte[] _labelRaw;

        /// <summary>
        /// List of characters that can be appended to the end of a repeated short variable
        /// </summary>
        private static readonly char[] AppendableChars = Enumerable.Range('A', 'Z').Select(i => (char)i).ToArray();

		public RecordType RecordType => RecordType.VariableRecord;
        public int Type { get; private set; }
        public bool HasVariableLabel { get; private set; }
        public int MissingValueType { get; private set; }
	    private readonly int _missingValueCount;

        private static readonly VariableRecord StringContinuationRecord = new VariableRecord
        {
                _nameRaw = Encoding.ASCII.GetBytes("".PadRight(8)), // All  spaces
                Type = -1,
                PrintFormat = new OutputFormat(FormatType.A, 0x1d, 1), // SPSS writes this a the print and write formats
                WriteFormat = new OutputFormat(FormatType.A, 0x1d, 1)  // 01 0d 01 00
        };

        public OutputFormat PrintFormat { get; private set; }
        public OutputFormat WriteFormat { get; private set; }
        
        public string Name
        {
            get
            {
                return _nameRaw != null ? Encoding.GetTrimmed(_nameRaw) : null;
            }
            private set
            {
                // The varuiable short name must be an 8 bytes encoded upercase string, padded with spaces if needed.
                _nameRaw = value != null ? Encoding.GetPadded(value.ToUpperInvariant(), 8) : new byte[8];
            }
        }

        public int LabelLength { get; private set; }
        
        public string Label
        {
            get
            {
                return Encoding.GetTrimmed(_labelRaw);
            }
            private set
            {
                HasVariableLabel = !string.IsNullOrEmpty(value);
                if (!HasVariableLabel) return;

                int length;
                _labelRaw = Encoding.GetPaddedRounded(value, 4, out length, 252);
                LabelLength = length;
            }
        }

        public IList<double> MissingValues { get; private set; }

        private VariableRecord()
	    {}

        /// <summary>
        /// Constructor for loading the reord info before writing the file
        /// </summary>
        /// <param name="variable">The created variable information</param>
        /// <param name="headerEncoding">The encoding used for the header. <see cref="MachineIntegerInfoRecord.CharacterCode"/></param>
        private VariableRecord(Variable variable, Encoding headerEncoding)
	    {
            Encoding = headerEncoding;

			// if type is numeric, write 0, if not write the string lenght for short string fields
            Type = variable.Type == DataType.Numeric ? 0 : variable.TextWidth;
			// Set the max string lenght for the type
			if (Type > 255)
			{
				Type = 255;
			}

			MissingValues = variable.MissingValues;
			
			MissingValueType = (int)variable.MissingValueType;
		    _missingValueCount = Math.Abs(MissingValueType);
			PrintFormat = variable.PrintFormat;
			WriteFormat = variable.WriteFormat;

            // Verify that a name for the variable is provided, this to avoid a NullRef
            if (string.IsNullOrWhiteSpace(variable.Name))
            {
                throw new ArgumentException("The name of a variable can not be null, empty or consist only of whitespace", nameof(variable));
            }

            Name = variable.Name;
			Label = variable.Label;
		}

        /// <summary>
        /// Creates all variable records needed for this variable
        /// </summary>
        /// <param name="variable">The variable matadata to create the new variable</param>
        /// <param name="headerEncoding">The encoding to use on the header</param>
        /// <param name="previousVariableNames">
        ///     A list of the variable names that were already 
        ///     created, to avoid the short name colition
        /// </param>
        /// <param name="longNameCounter">
        ///     The counter of variables with name replaced, to create
        ///     a proper long name that won't collide
        /// </param>
        /// <param name="longStringVariables"></param>
        /// <param name="segmentsNamesList"></param>
        /// <returns>
        /// 		Only one var for numbers or text of lenght 8 or less, or the 
        /// 		main variable definition, followed by string continuation "dummy"
        /// 		variables. There should be one for each 8 chars after the first 8.
        ///  </returns>
        internal static VariableRecord[] GetNeededVariables(Variable variable, Encoding headerEncoding, SortedSet<byte[]> previousVariableNames, ref int longNameCounter, IDictionary<string, int> longStringVariables, SortedList<byte[], int> segmentsNamesList)
        {
            var header = new VariableRecord(variable, headerEncoding)
                         {
                             DisplayInfo = GetVariableDisplayInfo(variable)
                         };

            var originalName = header._nameRaw;

            CheckShortName(header, previousVariableNames, ref longNameCounter);

            // Set output format for text variables to be equal to textwidth
            // Also detect if a variable is extra wide and needs to be cut up over multiple variables (long string variables)
            if (variable.Type == DataType.Text)
            {
                if (variable.TextWidth <= 255)
                {
                    header.WriteFormat = new OutputFormat(FormatType.A, variable.TextWidth);
                    header.PrintFormat = new OutputFormat(FormatType.A, variable.TextWidth);
                }
                else
                {
                    longStringVariables.Add(header.Name, variable.TextWidth);
                    header.WriteFormat = new OutputFormat(FormatType.A, 255);
                    header.PrintFormat = new OutputFormat(FormatType.A, 255);
                }
            }

            // If it's numeric or a string of lenght 8 or less, no dummy vars are needed
            if (variable.Type == DataType.Numeric || variable.TextWidth <= 8)
			{
				return new []{header};
			}


            var segments = GetLongStringSegmentsCount(variable.TextWidth);
            if(!(segments > 0))
                throw new SpssFileFormatException("String variables can no have less than one segment");

            // Create all the variable continuation records that for each extra 8 bytes of string data
            // The actual count of needed VariableRecords
            var varCount = GetLongStringContinuationRecordsCount(variable.TextWidth);
            var result = new VariableRecord[varCount];

            var dummyVar = GetStringContinuationRecord();

            var fullSegmentBlocks = GetStringContinuationRecordsCount(255);
            
            var segmentLength = segments > 1 ? 255 : GetFinalSegmentLenght(variable.TextWidth, segments);
            var segmentBlocks = GetStringContinuationRecordsCount(segmentLength);

            header.Type = segmentLength;
            result[0] = header;
            
            var currentSegment = 0;
            var i = 1;
            while (true)
            {
                var segmentBaseIndex = fullSegmentBlocks*currentSegment;
                for (; i < segmentBaseIndex + segmentBlocks; i++)
                {
                    result[i] = dummyVar;
                }

                currentSegment++;
                var segmentsLeft = segments - currentSegment;

                if (segmentsLeft <= 0)
                {
                    break;
                }

                segmentLength = segmentsLeft > 1 ? 255 : GetFinalSegmentLenght(variable.TextWidth, segments);
                segmentBlocks = GetStringContinuationRecordsCount(segmentLength);

                result[i++] = GetVlsExtraVariable(header, headerEncoding, segmentLength, previousVariableNames, segmentsNamesList, originalName);
            }
            
            return result;
		}

        private static VariableRecord GetVlsExtraVariable(VariableRecord variable, Encoding encoding, int segmentLength, SortedSet<byte[]> previousVariableNames, SortedList<byte[], int> segmentsNamesList, byte[] originalName)
        {
            var outputFormat = new OutputFormat(FormatType.A, segmentLength);
            var record = new VariableRecord
                {
                    Encoding = encoding,
                    _nameRaw = GenerateContinuationSegmentShortName(originalName, previousVariableNames, segmentsNamesList),
                    Label = variable.Label,
                    Type = segmentLength,
                    PrintFormat = outputFormat,
                    WriteFormat = outputFormat,
                    DisplayInfo = variable.DisplayInfo
                };
            
            return record;
        }
        
        private static VariableDisplayInfo GetVariableDisplayInfo(Variable variable)
        {
            return new VariableDisplayInfo
                {
                    Alignment = variable.Alignment,
                    MeasurementType = variable.MeasurementType,
                    Width = GetDisplayInfoWith(variable),
                };
        }

        private static int GetDisplayInfoWith(Variable variable)
        {
            if (variable.TextWidth > 0)
            {
                return variable.Width;
            }
            
            if (variable.TextWidth > 0)
            {
                return variable.TextWidth;
            }

            var format = variable.PrintFormat ?? variable.WriteFormat;
            if (format != null)
            {
                return format.FieldWidth;
            }

            return 0;
        }

        internal VariableDisplayInfo DisplayInfo { get; set; }

        /// <summary>
        /// Gets the amount of dummy variables (string continuation records) needed for a string of 
        /// length
        /// </summary>
        /// <param name="length">The length (in bytes, not chars) for the string</param>
        /// <exception cref="ArgumentException">
        /// If the lenght is more than 255 bytes. If the variable needs to hold longer text use
        /// <see cref="VeryLongStringRecord"/>
        /// </exception>
        /// <returns>Number of string continuation records needed (variables of type -1 and width 8)</returns>
        internal static int GetStringContinuationRecordsCount(int length)
        {
            if(length > 255)
                throw new ArgumentException("Continuation records are for string variables up to 255 bytes. For more, use VeryLongStringsRecords",
                    nameof(length));
            return (int)Math.Ceiling(length / 8d);
        }

        /// <summary>
        /// Gives the number of segments for a very long string (<see cref="VeryLongStringRecord"/>),
        /// or just 1 if there's no need for the VLS 
        /// </summary>
        /// <param name="lenght">The length (in bytes, not chars) for the string</param>
        /// <remarks>
        /// Up to 255 bytes, theres only one segment, for more there should be one segment 
        /// for each 252 bytes of lenght, each segment will have one variable with a name, 
        /// the string length and multiple string continuation records.
        /// </remarks>
        /// <returns>The number of segments for <see cref="lenght"/> of bytes</returns>
        internal static int GetLongStringSegmentsCount(int lenght)
        {
            if (lenght <= 255)
                return 1;
            return (int)Math.Ceiling(lenght / 252d);
        }

        /// <summary>
        /// Gives the total number of variable records that a very long string (<see cref="VeryLongStringRecord"/>)
        /// needs, including the extra variables for each extra segment.
        /// </summary>
        /// <param name="lenght">The length (in bytes, not chars) for the string</param>
        /// <remarks>
        /// There's one segment for each 252 of <see cref="lenght"/> of bytes.
        /// Each segment but the last has a width of 255.
        /// The las segment has the suposed remider, as if the previous ones had only 252
        /// (but that's not actualy the case). Why?? For the glory of Satan, of course.
        /// </remarks> 
        /// <returns>Number of VariableRecords needed for <see cref="lenght"/> of bytes</returns>
        internal static int GetLongStringContinuationRecordsCount(int lenght)
        {
            // Get the total segments count
            var segments = GetLongStringSegmentsCount(lenght);
            // All except the last segment have a with of 255.
            var normalSegmentLength = GetStringContinuationRecordsCount(255);
            // The last segment has the suposed remider (see remarks)
            var finalSegmentLenght = GetStringContinuationRecordsCount(GetFinalSegmentLenght(lenght, segments));
            
            return (segments - 1) * normalSegmentLength + finalSegmentLenght;
        }

        /// <summary>
        /// Gives the total ammount of bytes the variable of <see cref="lenght"/> occupies, taking into account the weird 
        /// rules that LSV have and an unused byte each 255 bytes (to complete the 8 byte block of the long string variable)
        /// </summary>
        /// <param name="lenght">The length in bytes of the string</param>
        /// <returns>The total ammount of bytes the variable of <see cref="lenght"/> occupies</returns>
        internal static int GetLongStringBytesCount(int lenght)
        {
            return GetLongStringContinuationRecordsCount(lenght)*8;
        }

        /// <summary>
        /// Gets the supposed lenght of the last segment.
        /// This works the same for VeryLongStrings or just LongStrings
        /// </summary>
        /// <param name="lenght">The total length of the string in bytes</param>
        /// <param name="segments">The number of VeryLongStrings segments needed (1 for just normal LongStrings)</param>
        /// <returns>The length of the reminding segment</returns>
        private static int GetFinalSegmentLenght(int lenght, int segments)
        {
            return lenght - (segments - 1) * 252;
        }

        /// <summary>
        /// Checks if the name that was set (after slicing it to 8 chars and encoding it properly) is not repeated on the names
        /// of the variables created before this one. 
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="previousVariableNames"></param>
        /// <param name="longNameCounter"></param>
        private static void CheckShortName(VariableRecord variable, SortedSet<byte[]> previousVariableNames, ref int longNameCounter)
        {
            // Check if it's already on the variable records names (compare with raw encoded name byte array)
            if (previousVariableNames.Contains(variable._nameRaw))
            {
                // Algorithm to create a variable with a short name.
                // As produced by "IBM SPSS STATISTICS 64-bit MS Windows 22.0.0.0"
                var currentLongNameIndex = ++longNameCounter;

                // Avoid collisions in case there is already a var called VXX_A
                var appendCharIndex = 0;
                do
                {
                    variable.Name = $"V{currentLongNameIndex}_{AppendableChars[appendCharIndex++]}";
                } while (previousVariableNames.Contains(variable._nameRaw));
            }
            // Add the raw encoded name byte array to avoid collitions in following variables
            previousVariableNames.Add(variable._nameRaw);
        }

        
        /// <summary>
        /// Generates a new short name for a VLS continuation variable, checking that the name is not already used.
        /// This tries to replicate how SPSS does it, by shrinking the original var name to at most 5 chars and appending 
        /// a number to make it unique. The nuber is at most 3 chars, padded to the left with spaces.
        /// </summary>
        /// <param name="variable">The head variable reacord to get the name from</param>
        /// <param name="previousVariableNames">The set of all other used short variable names</param>
        /// <param name="segmentsNamesList">The cache to store up to which number has been used for each up to 5 char name</param>
        /// <returns>The encoded short name for the VLS continuation segment</returns>
        /// <remarks>
        /// Apparently, for SPSS to reads VLS continuation segments consistenly, the segments should have the same first 5 characters.
        /// If segments have different names that don't match, SPSS could not interpret them as part of the same variable
        /// </remarks>
        private static byte[] GenerateContinuationSegmentShortName(byte[] variable, SortedSet<byte[]> previousVariableNames, SortedList<byte[], int> segmentsNamesList)
        {
            // Get a new array with the encoded name
            var segmentName = (byte[])variable.Clone();
            
            // Find out the ending position of the name, or cut it to 5
            var nameEndIndex = Array.IndexOf<byte>(segmentName, 0x20);
            if (nameEndIndex == -1 || nameEndIndex > 5)
            {
                nameEndIndex = 5;
            }

            // Get the short name (of up to 5 chars) to use as key for the numeration cache
            byte[] shortSegmentName = new byte[5];
            Array.Copy(segmentName, shortSegmentName, nameEndIndex);

            // Get the current number for the short name from the cache, or start by 0
            int segmentContinuationNumber;
            if (!segmentsNamesList.TryGetValue(shortSegmentName, out segmentContinuationNumber))
            {
                // Set to -1 so the first iteration would set it at 0
                segmentContinuationNumber = -1;
            }

            do
            {
                // Get the bytes for the next number padded with spaces to 3, this way name will be overriten with the number at the end
                segmentContinuationNumber++;
                var numberBytes = Encoding.ASCII.GetBytes(segmentContinuationNumber.ToString().PadRight(3));

                if (numberBytes.Length > 3)
                {
                    throw new SpssFileFormatException(
                        "There can be no more than 999 segment continuation variables with se same 5 letter name");
                }
                // Set the number at the end of the name
                Array.Copy(numberBytes, 0, segmentName, nameEndIndex, 3);
                // Check if that name is already used
            } while (previousVariableNames.Contains(segmentName));

            // If not used, add the current number to the segments names number cache
            segmentsNamesList[shortSegmentName] = segmentContinuationNumber;

            // Add the raw encoded name byte array to avoid collitions in following variables
            previousVariableNames.Add(segmentName);

            return segmentName;
        }

        /// <summary>
		/// Creates and returns a variable that contains the info to be written as a continuation of a string 
		/// variable. 
		/// This variable is needed imediatelly after text vatiables of more than 8 chars, and there should 
		/// be one for each 8 bytes of text exiding the first 8
		/// </summary>
		private static VariableRecord GetStringContinuationRecord()
		{
            return StringContinuationRecord;
		}

	    public void WriteRecord(BinaryWriter writer)
	    {
		    writer.Write((int)RecordType);
		    writer.Write(Type);
		    writer.Write(HasVariableLabel ? 1 : 0);
			writer.Write(MissingValueType);
			writer.Write(PrintFormat?.GetInteger() ?? 0);
			writer.Write(WriteFormat?.GetInteger() ?? 0);
            
			writer.Write(_nameRaw);
			
		    if (HasVariableLabel)
		    {   
			    writer.Write(LabelLength);
				writer.Write(_labelRaw);
			}

			if (MissingValueType != 0)
			{
				for(int i = 0; i < MissingValues.Count && i < _missingValueCount; i++)
				{
					writer.Write(MissingValues[i]);
				}
			}
		    
	    }

        public void FillRecord(BinaryReader reader)
        {
            Type = reader.ReadInt32();
            HasVariableLabel = reader.ReadInt32() == 1;
            MissingValueType = reader.ReadInt32();
            PrintFormat = new OutputFormat(reader.ReadInt32());
            WriteFormat = new OutputFormat(reader.ReadInt32());
            _nameRaw = reader.ReadBytes(8);
            if (HasVariableLabel)
            {
                LabelLength = reader.ReadInt32();

                //Rounding up to nearest multiple of 32 bits.
                //This is the original rounding version. But this leads to a wrong result with record.LabelLength=0
                //This is the strange situation where HasVariableLabel is true, but in fact does not have a label.
                //(((record.LabelLength - 1) / 4) + 1) * 4;
                //New round up version from stackoverflow
                int labelBytes = Common.RoundUp(LabelLength, 4);
                _labelRaw = reader.ReadBytes(labelBytes);
            }

            var missingValues = new List<double>(Math.Abs(MissingValueType));
            for (int i = 0; i < Math.Abs(MissingValueType); i++)
            {
                missingValues.Add(reader.ReadDouble());
            }
            MissingValues = new Collection<double>(missingValues);
        }

        public void RegisterMetadata(MetaData metaData)
        {
            metaData.VariableRecords.Add(this);
            Metadata = metaData;
        }
    }
}
