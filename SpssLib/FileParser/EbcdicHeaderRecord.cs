using System;
using System.IO;
using SpssLib.FileParser.Records;

namespace SpssLib.FileParser
{
    internal class EbcdicHeaderRecord : IRecord
    {
        public NotSupportedException Exception { get { return new NotSupportedException("EBCDIC???? Who uses that? Honestly!!"); } }

        public RecordType RecordType { get { return RecordType.EbcdicHeaderRecord;} }

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