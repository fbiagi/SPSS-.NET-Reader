using System;
using System.Collections.Generic;
using System.IO;
using System.Collections.ObjectModel;
using System.Text;

namespace SpssLib.FileParser.Records
{
    // RecordType == 3
    public class ValueLabelRecord
    {
        public Int32 LabelCount { get; private set; }
        public Dictionary<byte[], string> Labels { get; private set; }
        public Int32 VarCount { get; private set; }
        public Collection<Int32> Variables { get; private set; }

        private ValueLabelRecord()
        {
        }

        public static ValueLabelRecord ParseNextRecord(BinaryReader reader)
        {
            var record = new ValueLabelRecord();

            record.LabelCount = reader.ReadInt32();
            record.Labels = new Dictionary<byte[], string>();

            for (int i = 0; i < record.LabelCount; i++)
            {
                byte[] value = reader.ReadBytes(8);
                int labelLength = (int)reader.ReadByte();

                //Rounding up to nearest multiple of 32 bits.
                int labelBytes = (((((labelLength) / 8) + 1) * 8) - 1);
                byte[] chars = reader.ReadBytes(labelBytes);

                string label = Common.ByteArrayToString(chars); 
               
                record.Labels.Add(
                    value,
                    label);
            }

            // Parse the adjecent ValueLabelVariablesRecord as well:
            RecordType type = (RecordType)reader.ReadInt32();

            if (type != RecordType.ValueLabelVariablesRecord)
                throw new UnexpectedFileFormatException();

            record.VarCount = reader.ReadInt32();
            record.Variables = new Collection<int>();

            for (int i = 0; i < record.VarCount; i++)
            {
                record.Variables.Add(reader.ReadInt32());
            }

            return record;
        }
    }
}
