using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using SpssLib.SpssDataset;
using System.Collections.ObjectModel;

namespace SpssLib.FileParser.Records
{
    public class VariableRecord : IRecord
    {
		public RecordType RecordType { get { return RecordType.VariableRecord; } }
        public Int32 Type { get; private set; }
        public bool HasVariableLabel { get; private set; }
        public Int32 MissingValueType { get; private set; }
	    private int _missingValueCount;
        public OutputFormat PrintFormat { get; private set; }
        public OutputFormat WriteFormat { get; private set; }
        public string Name { get; private set; }
        public Int32 LabelLength { get; private set; }
        public string Label { get; private set; }
        public IList<double> MissingValues { get; private set; }

	    internal VariableRecord()
	    {}

	    internal VariableRecord(Variable variable)
		{
			// if type is numeric, write 0, if not write the string lenght for short string fields
			Type = variable.Type == 0 ? 0 : variable.TextWidth;
			// Set the max string lenght for the type
			if (Type > 255)
			{
				Type = 255;
			}
			HasVariableLabel = !string.IsNullOrEmpty(variable.Label);

			MissingValues = variable.MissingValues;
			
			MissingValueType = variable.MissingValueType;
		    _missingValueCount = Math.Abs(MissingValueType);
			PrintFormat = variable.PrintFormat;
			WriteFormat = variable.WriteFormat;

			Name = variable.ShortName;
			Label = variable.Label;
		}

		/// <summary>
		/// Creates all variable records needed for this variable
		/// </summary>
		/// <returns>
		///		Only one var for numbers or text of lenght 8 or less, or the 
		///		main variable definition, followed by string continuation "dummy"
		///		variables. There should be one for each 8 chars after the first 8.
		/// </returns>
		internal static VariableRecord[] GetNeededVaraibles(Variable variable)
		{
			var headVariable = new VariableRecord(variable);

			// If it's numeric or a string of lenght 8 or less, no dummy vars are needed
			if (variable.Type == DataType.Numeric || variable.TextWidth <= 8)
			{
				return new []{headVariable};
			}

			// TODO longer strings not supported by now. need to create multiple vars with name and length (up to 255 by named var)
			if (variable.TextWidth > 255)
			{
				variable.TextWidth = 255;
			}

			var varCount =  (int)Math.Ceiling(variable.TextWidth/8d);
			var result = new VariableRecord[varCount];
			result[0] = headVariable;
			var dummyVar = GetStringContinuationRecord();
			for (int i = 1; i < varCount; i++)
			{
				result[i] = dummyVar;
			}
			return result;
		}

		/// <summary>
		/// Creates and returns a variable that contains the info to be written as a continuation of a string 
		/// variable. 
		/// This variable is needed imediatelly after text vatiables of more than 8 chars, and there should 
		/// be one for each 8 bytes of text exiding the first 8
		/// </summary>
		private static VariableRecord GetStringContinuationRecord()
		{
			return new VariableRecord
				{
					Type = -1,
				};
		}

	    public void WriteRecord(BinaryWriter writer)
	    {
		    writer.Write((int)RecordType);
		    writer.Write(Type);
		    writer.Write(HasVariableLabel ? 1 : 0);
			writer.Write(MissingValueType);
			writer.Write(PrintFormat != null ? PrintFormat.GetInteger() : 0);
			writer.Write(WriteFormat != null ? WriteFormat.GetInteger() : 0);

			if (Name != null)
			{
				writer.Write(Name.PadRight(8, ' ').Substring(0, 8).ToCharArray());
			}
			else
			{
				writer.Write(new byte[8]);
			}
			
		    if (HasVariableLabel)
		    {
			    var labelBytes = Common.StringToByteArray(Label);
			    LabelLength = labelBytes.Length;
			    writer.Write(LabelLength);
				writer.Write(labelBytes);

				var padding = Common.RoundUp(LabelLength, 4) - LabelLength;
			    var paddingBytes = new byte[padding].Select(b => (byte) 0x20).ToArray();
				writer.Write(paddingBytes);
			}

			if (MissingValueType != 0)
			{
				for(int i = 0; i < MissingValues.Count && i < _missingValueCount; i++)
				{
					writer.Write(MissingValues[i]);
				}
			}
		    
	    }

        [Obsolete]
	    public static VariableRecord ParseNextRecord(BinaryReader reader)
        {
            var record = new VariableRecord();
            record.FillRecord(reader);
	        return record;
        }

        public void FillRecord(BinaryReader reader)
        {
            Type = reader.ReadInt32();
            HasVariableLabel = (reader.ReadInt32() == 1);
            MissingValueType = reader.ReadInt32();
            PrintFormat = new OutputFormat(reader.ReadInt32());
            WriteFormat = new OutputFormat(reader.ReadInt32());
            Name = Common.ByteArrayToString(reader.ReadBytes(8));
            if (HasVariableLabel)
            {
                LabelLength = reader.ReadInt32();

                //Rounding up to nearest multiple of 32 bits.
                //This is the original rounding version. But this leads to a wrong result with record.LabelLength=0
                //This is the strange situation where HasVariableLabel is true, but in fact does not have a label.
                //(((record.LabelLength - 1) / 4) + 1) * 4;
                //New round up version from stackoverflow
                int labelBytes = Common.RoundUp(LabelLength, 4);
                Label = Common.ByteArrayToString(reader.ReadBytes(labelBytes));
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
        }
    }
}
