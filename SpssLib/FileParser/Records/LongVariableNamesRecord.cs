using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SpssLib.FileParser.Records
{
    public class LongVariableNamesRecord : BaseInfoRecord
    {
        public override int SubType { get { return InfoRecordType.LongVariableNames; }}

        public LongVariableNamesRecord()
        {}

		public LongVariableNamesRecord(Dictionary<string, string> variableLongNames)
		{
			LongNameDictionary = variableLongNames;
		}

        protected override void WriteInfo(BinaryWriter writer)
		{
			StringBuilder sb = new StringBuilder();
			foreach (var variable in LongNameDictionary)
			{
				sb.Append(variable.Key)
					.Append('=')
					.Append(GetStringMaxLength(variable.Value, 64))
					.Append('\t');
			}
			var stringDictionary = sb.ToString();
			stringDictionary = stringDictionary.Substring(0, stringDictionary.Length - 1);
			writer.Write(stringDictionary.Length); // TODO check if it'll work with just the write(string) method that prepends the length
			writer.Write(stringDictionary.ToCharArray());
		}

	    private string GetStringMaxLength(string value, int i)
	    {
		    return value.Length > i ? value.Substring(0, i) : value;
	    }
        
        protected override void FillInfo(BinaryReader reader)
        {
            CheckInfoHeader(1);
            
            LongNameDictionary = new Dictionary<string, string>();

            var originalBytes = reader.ReadBytes(ItemCount); //(from item in this.record.Items select item[0]).ToArray();
            // TODO see what happens with encoding, we might have to use the one in MachineIntegerInfo record
            var dictionaryString = Encoding.ASCII.GetString(originalBytes);

            // split on tabs:
            var entries = dictionaryString.Split('\t');

            foreach (var entry in entries)
            {
                var values = entry.Split('=');
                LongNameDictionary.Add(values[0], values[1]);
            }
        }

	    public Dictionary<string, string> LongNameDictionary
        {
            get;
            private set;
        }
    }
}
