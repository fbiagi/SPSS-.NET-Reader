using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using SpssLib.SpssDataset;
using System.Collections.ObjectModel;

namespace SpssLib.FileParser.Records
{
    public class VariableRecord : IBaseRecord
    {
		public int RecordType { get { return 2; } }
        public Int32 Type { get; private set; }
        public bool HasVariableLabel { get; private set; }
        public Int32 MissingValueCount { get; private set; }
        public OutputFormat PrintFormat { get; private set; }
        public OutputFormat WriteFormat { get; private set; }
        public string Name { get; private set; }
        public Int32 LabelLength { get; private set; }
        public string Label { get; private set; }
        public ICollection<double> MissingValues { get; private set; }

        private VariableRecord()
        {
        }

		internal VariableRecord(Variable variable)
		{
			// if type is numeric, write 0, if not write the string lenght for short string fields
			Type = variable.Type == 0 ? 0 : variable.PrintFormat.FieldWidth;
			// TODO for long strings (with long string records) this can be more that 255
			// Set the max string lenght for the type
			if (Type > 255)
			{
				Type = 255;
			}
			HasVariableLabel = !string.IsNullOrEmpty(variable.Label);

			MissingValues = variable.MissingValues;
			// Enforce 3 renge values as max
			if (MissingValues.Count > 3)
			{
				MissingValues = MissingValues.Take(3).ToList();
			}
			// TODO change this with actual info about the type of mising values (-2 Range, -3 Range + discrete value)
			MissingValueCount = variable.MissingValues.Count > 3 ? 3 : variable.MissingValues.Count;
			PrintFormat = variable.PrintFormat;
			WriteFormat = variable.WriteFormat;

			Name = variable.ShortName;
			Label = variable.Label;

		}

	    public void WriteRecord(BinaryWriter writer)
	    {
		    writer.Write(RecordType);
		    writer.Write(Type);
		    writer.Write(HasVariableLabel ? 1 : 0);
			writer.Write(MissingValueCount);
			writer.Write(PrintFormat.GetInteger());
			writer.Write(WriteFormat.GetInteger());
			writer.Write(Name.PadRight(8, ' ').Substring(0, 8).ToCharArray());

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

		    foreach (var missingValue in MissingValues)
		    {
			    writer.Write(missingValue);
		    }
	    }
	  
	    public static VariableRecord ParseNextRecord(BinaryReader reader)
        {
            var record = new VariableRecord();
            record.Type = reader.ReadInt32();
            record.HasVariableLabel = (reader.ReadInt32() == 1);
            record.MissingValueCount = reader.ReadInt32();
            record.PrintFormat = new OutputFormat(reader.ReadInt32());
            record.WriteFormat = new OutputFormat(reader.ReadInt32());
            record.Name =Common.ByteArrayToString(reader.ReadBytes(8));
            if(record.HasVariableLabel)
            {
                record.LabelLength = reader.ReadInt32();
                
                //Rounding up to nearest multiple of 32 bits.
                //This is the original rounding version. But this leads to a wrong result with record.LabelLength=0
                //This is the strange situation where HasVariableLabel is true, but in fact does not have a label.
                //(((record.LabelLength - 1) / 4) + 1) * 4;
                //New round up version from stackoverflow
                int labelBytes = Common.RoundUp(record.LabelLength, 4);
                record.Label = Common.ByteArrayToString(reader.ReadBytes(labelBytes));
            }

            var missingValues = new List<double>(Math.Abs(record.MissingValueCount));
            for (int i = 0; i < Math.Abs(record.MissingValueCount); i++)
		    {
                missingValues.Add(reader.ReadDouble());
		    }
            record.MissingValues = new Collection<double>(missingValues);

            return record;
        }
    }
}
