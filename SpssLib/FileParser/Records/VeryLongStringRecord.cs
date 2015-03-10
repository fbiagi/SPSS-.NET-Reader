using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SpssLib.FileParser.Records
{
    public class VeryLongStringRecord : VariableDataInfoRecord<int>
    {
        public override int SubType { get { return InfoRecordType.VeryLongString; } }

        public VeryLongStringRecord(IDictionary<string, int> dictionary, Encoding encoding)
            : base(dictionary, encoding)
        {}

        protected override int DecodeValue(string stringValue)
        {
            int lenght;
            if(!int.TryParse(stringValue, out lenght))
                throw new SpssFileFormatException("Couldn't read the size of the VeryLongString as interger. Value read was '"+
                    (stringValue.Length > 80 ? stringValue.Substring(0, 80)+"..." : stringValue) + "'");

            return lenght;
        }

        protected override string EncodeValue(int value)
        {
            var strValue = value.ToString(CultureInfo.InvariantCulture);
            // The value fields must have exactly 5 bytes
            if (strValue.Length > 5) 
                throw new SpssFileFormatException("A string length of "+value+" is not supported");

            // Add the 0 to the left to reach the 5 bytes and add the trailing null
            return strValue.PadLeft(5, '0')+'\0';
        }

        public override void RegisterMetadata(MetaData metaData)
        {
            metaData.VeryLongStrings = this;
            Metadata = metaData;
        }
    }
}