using System;
using System.IO;

namespace SpssLib.FileParser.Records
{
    internal class EbcdicHeaderRecord : IRecord
    {
        public NotSupportedException Exception => new NotSupportedException("EBCDIC records not supported.");

        public RecordType RecordType => RecordType.EbcdicHeaderRecord;

        public void WriteRecord(BinaryWriter writer)
        {
            throw Exception;
        }

        public void FillRecord(DualBinaryReader reader)
        {
            throw Exception;
        }

        public void RegisterMetadata(MetaData metaData)
        {
            throw Exception;
        }
    }
}