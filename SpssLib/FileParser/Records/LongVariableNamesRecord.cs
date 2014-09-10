using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace SpssLib.FileParser.Records
{
    public class LongVariableNamesRecord
    {
        private InfoRecord record;

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
