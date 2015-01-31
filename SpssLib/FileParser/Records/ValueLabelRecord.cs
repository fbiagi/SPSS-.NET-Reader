using System;
using System.Collections.Generic;
using System.IO;
using System.Collections.ObjectModel;
using System.Linq;

namespace SpssLib.FileParser.Records
{
    // RecordType == 3
    public class ValueLabelRecord : IRecord
    {
		public RecordType RecordType { get { return RecordType.ValueLabelRecord; } }
        public Int32 LabelCount { get; private set; }
        public IDictionary<byte[], string> Labels { get; private set; }
        public Int32 VarCount { get; private set; }
        public ICollection<Int32> Variables { get; private set; }


	    internal ValueLabelRecord(ValueLabel valueLabel)
	    {
		    LabelCount = valueLabel.Labels.Count;
			Labels = valueLabel.Labels.ToDictionary(p => BitConverter.GetBytes(p.Key), p => p.Value);
		    VarCount = valueLabel.VariableIndex.Count;
		    Variables = valueLabel.VariableIndex;
	    }

	    internal ValueLabelRecord(){}

		public void WriteRecord(BinaryWriter writer)
		{
			writer.Write((int)RecordType);
			writer.Write(LabelCount);
			foreach (var label in Labels)
			{
				writer.Write(label.Key);
				writer.Write((byte)label.Value.Length);
				writer.Write(label.Value.ToCharArray());
				
				// Label + 1 (the label length byte) must be a multiple of 8
				// if not, we'll need to add padding
				var mod = (label.Value.Length + 1)%8;
				if (mod > 0)
				{
					writer.Write(new byte[8-mod]);
				}
			}

			writer.Write(4); // Record type int32 = 4 for the valu labels variables
			writer.Write(Variables.Count);
			foreach (var dictionaryIndex in Variables)
			{
				writer.Write(dictionaryIndex);
			}
		}

        [Obsolete]
	    public static ValueLabelRecord ParseNextRecord(BinaryReader reader)
        {
            var record = new ValueLabelRecord();
            record.FillRecord(reader);
	        return record;
        }

        public void FillRecord(BinaryReader reader)
        {
            LabelCount = reader.ReadInt32();
            Labels = new Dictionary<byte[], string>();

            for (int i = 0; i < LabelCount; i++)
            {
                byte[] value = reader.ReadBytes(8);
                int labelLength = reader.ReadByte();

                // TODO replace with the read of labelLengh and the padding bytes as in the write method
                int labelBytes = (((((labelLength)/8) + 1)*8) - 1);
                byte[] chars = reader.ReadBytes(labelBytes);

                string label = Common.ByteArrayToString(chars);

                Labels.Add(
                    value,
                    label);
            }

            // Parse the adjecent ValueLabelVariablesRecord as well:
            // TODO improve conversion to enum, as in SavFileParser
            RecordType type = (RecordType) reader.ReadInt32();

            if (type != RecordType.ValueLabelVariablesRecord)
                throw new UnexpectedFileFormatException();

            VarCount = reader.ReadInt32();
            Variables = new Collection<int>();

            for (int i = 0; i < VarCount; i++)
            {
                Variables.Add(reader.ReadInt32());
            }
        }
    }
}
