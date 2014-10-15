using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SpssLib.FileParser.Records
{
    public class LongVariableNamesRecord : IBaseRecord
    {
        private InfoRecord record;

		public int RecordType { get { return 7; } }

		public LongVariableNamesRecord(Dictionary<string, string> variableLongNames)
		{
			LongNameDictionary = variableLongNames;
		}

		public void WriteRecord(BinaryWriter writer)
		{
			writer.Write(RecordType);
			writer.Write(13); // Long Variable Names Record subtype
			writer.Write(1); //  Long Variable Names info record block size is allways 1
			
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

	    internal LongVariableNamesRecord(InfoRecord record)
        {
            if (record.SubType != 13 || record.ItemSize != 1)
                throw new UnexpectedFileFormatException();
            this.record = record;

            this.LongNameDictionary = new Dictionary<string, string>();

            // Not very efficient, but for now the best way I can come up with
            //   without sacrificing the Inforecords-design.
            var originalBytes = (from item in this.record.Items select item[0]).ToArray();
            var dictionaryString = Encoding.ASCII.GetString(originalBytes);

            // split on tabs:
            var entries = dictionaryString.Split('\t');

            foreach (var entry in entries)
            {
                var values = entry.Split('=');
                this.LongNameDictionary.Add(values[0], values[1]);
            }
        }

	    public Dictionary<string, string> LongNameDictionary
        {
            get;
            private set;
        }
    }
}
