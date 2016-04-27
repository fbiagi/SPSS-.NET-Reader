using System;
using System.IO;

namespace SpssLib.FileParser.Records
{
    internal class EbcdicHeaderRecord : IRecord
    {
        public NotSupportedException Exception => new NotSupportedException("EBCDIC???? Who uses that? Honestly!!");

        public RecordType RecordType => RecordType.EbcdicHeaderRecord;

        public void WriteRecord(BinaryWriter writer)
        {
            throw Exception;
        }

        public void FillRecord(BinaryReader reader)
        {
            throw Exception;
        }

        public void RegisterMetadata(MetaData metaData)
        {
            throw Exception;
        }
    }
}