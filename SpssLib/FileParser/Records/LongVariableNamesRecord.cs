using System.Collections.Generic;
using System.Text;

namespace SpssLib.FileParser.Records
{
    public class LongVariableNamesRecord : VariableDataInfoRecord<string>
    {
        public override int SubType { get { return InfoRecordType.LongVariableNames; }}

        public LongVariableNamesRecord(IDictionary<string, string> variableLongNames, Encoding encoding)
		{
		    Encoding = encoding;
		    ItemSize = 1;
            Dictionary = variableLongNames;
            BuildDataArray();
		    ItemCount = Data.Length;
		}

        public override void RegisterMetadata(MetaData metaData)
        {
            metaData.LongVariableNames = this;
            Metadata = metaData;
        }

        protected override string DecodeValue(string stringValue)
        {
            return stringValue;
        }

        protected override string EncodeValue(string value)
        {
            return value;
        }
    }
}
