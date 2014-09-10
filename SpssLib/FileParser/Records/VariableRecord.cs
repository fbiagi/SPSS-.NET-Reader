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
    public class VariableRecord
    {
        public Int32 Type { get; private set; }
        public bool HasVariableLabel { get; private set; }
        public Int32 MissingValueCount { get; private set; }
        public OutputFormat PrintFormat { get; private set; }
        public OutputFormat WriteFormat { get; private set; }
        public string Name { get; private set; }
        public Int32 LabelLength { get; private set; }
        public string Label { get; private set; }
        public Collection<double> MissingValues { get; private set; }

        private VariableRecord()
        {
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
