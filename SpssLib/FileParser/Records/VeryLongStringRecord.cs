using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SpssLib.FileParser.Records
{
    public class VeryLongStringRecord : VariableDataInfoRecord<int>
    {
        public override int SubType { get { return InfoRecordType.VeryLongString; } }

        protected override int DecodeValue(string stringValue)
        {
            // TODO exception if fails
            int lenght;
            int.TryParse(stringValue, out lenght);
            return lenght;
        }

        protected override string EncodeValue(int value)
        {
            throw new NotImplementedException();
        }

        public override void RegisterMetadata(MetaData metaData)
        {
            metaData.VeryLongStrings = this;
            Metadata = metaData;
        }
    }
}