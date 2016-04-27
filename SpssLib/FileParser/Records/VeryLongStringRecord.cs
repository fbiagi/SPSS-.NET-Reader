using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SpssLib.FileParser.Records
{
    public class VeryLongStringRecord : VariableDataInfoRecord<int>
    {
        protected override bool UsesTerminator => true;

        public override int SubType => InfoRecordType.VeryLongString;

        public VeryLongStringRecord(IDictionary<string, int> dictionary, Encoding encoding)
            : base(dictionary, encoding)
        {}

        protected override int DecodeValue(string stringValue)
        {
            int lenght;
            if(!int.TryParse(stringValue, out lenght))
                throw new SpssFileFormatException("Couldn't read the size of the VeryLongString as interger. Value read was '"+
                    (stringValue.Length > 80 ? stringValue.Substring(0, 77)+"..." : stringValue) + "'");

            return lenght;
        }

        protected override string EncodeValue(int value)
        {
            var strValue = value.ToString(CultureInfo.InvariantCulture);
            return strValue + '\0';
        }

        public override void RegisterMetadata(MetaData metaData)
        {
            metaData.VeryLongStrings = this;
            Metadata = metaData;
        }
    }
}