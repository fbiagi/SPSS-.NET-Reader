using System.Collections.Generic;
using System.Text;

namespace SpssLib.FileParser.Records
{
    public class LongVariableNamesRecord : VariableDataInfoRecord<string>
    {
        public override int SubType => InfoRecordType.LongVariableNames;

        public LongVariableNamesRecord(IDictionary<string, string> variableLongNames, Encoding encoding) 
            : base(variableLongNames, encoding)
        {}

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
