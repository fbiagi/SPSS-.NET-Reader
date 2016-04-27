using System;
using System.Collections.Generic;
using System.IO;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace SpssLib.FileParser.Records
{
    // RecordType == 3
    public class ValueLabelRecord : EncodeEnabledRecord, IRecord
    {
        private IDictionary<byte[], KeyValuePair<byte, byte[]>> _labelsRaw;
        public RecordType RecordType => RecordType.ValueLabelRecord;
        public int LabelCount { get; private set; }
        public IDictionary<byte[], string> Labels
        {
            get { return _labelsRaw.ToDictionary(p => p.Key, p => DecodeLabel(p.Value)); }
            private set { _labelsRaw = value.ToDictionary(p => p.Key, p => EncodeLabel(p.Value)); }
        }

        public int VarCount { get; private set; }
        public ICollection<int> Variables { get; private set; }


	    internal ValueLabelRecord(ValueLabel valueLabel, Encoding headerEncoding)
	    {
            // TODO add this to base constructor
            Encoding = headerEncoding;
		    
            LabelCount = valueLabel.Labels.Count;
	        Labels = valueLabel.Labels;
		    VarCount = valueLabel.VariableIndex.Count;
		    Variables = valueLabel.VariableIndex;
	    }

	    internal ValueLabelRecord(){}

		public void WriteRecord(BinaryWriter writer)
		{
			writer.Write(RecordType);
			writer.Write(LabelCount);
		    foreach (var label in _labelsRaw)
		    {
		        // Write the value of the value label
		        writer.Write(label.Key);
                // Write the lenght of the label
				writer.Write(label.Value.Key);
				//Write the label bytes
                writer.Write(label.Value.Value);
			}

            // Writes the variables for this dictionary
			writer.Write(RecordType.ValueLabelVariablesRecord); 
			writer.Write(Variables.Count);
			foreach (var dictionaryIndex in Variables)
			{
				writer.Write(dictionaryIndex);
			}
		}

        public void FillRecord(BinaryReader reader)
        {
            LabelCount = reader.ReadInt32();
            _labelsRaw = new Dictionary<byte[], KeyValuePair<byte, byte[]>>();

            for (int i = 0; i < LabelCount; i++)
            {
                var value = reader.ReadBytes(8);
                int labelLength = reader.ReadByte();

                // labelLenght + labelByte must be multiples of 8
                int labelBytes = Common.RoundUp(labelLength+1, 8)-1;
                byte[] chars = reader.ReadBytes(labelBytes);

                _labelsRaw.Add(value, new KeyValuePair<byte, byte[]>((byte)labelLength, chars));
            }

            // Parse the adjecent ValueLabelVariablesRecord as well:
            // TODO improve conversion to enum, as in SavFileParser
            RecordType type = reader.ReadRecordType();

            if (type != RecordType.ValueLabelVariablesRecord)
            {
                throw new SpssFileFormatException();
            }

            VarCount = reader.ReadInt32();
            Variables = new Collection<int>();

            for (int i = 0; i < VarCount; i++)
            {
                Variables.Add(reader.ReadInt32());
            }
        }

        public void RegisterMetadata(MetaData metaData)
        {
            metaData.ValueLabelRecords.Add(this);
            Metadata = metaData;
        }

        private string DecodeLabel(KeyValuePair<byte, byte[]> p)
        {
            return Encoding.GetString(p.Value, 0, p.Key);
        }

        private KeyValuePair<byte, byte[]> EncodeLabel(string value)
        {
            int length;
            var bytes = Encoding.GetPaddedRounded(value, 8, out length, 120, roundUpDelta: 1);
            return new KeyValuePair<byte, byte[]>((byte)length, bytes);
        }
    }
}
