using System.IO;

namespace SpssLib.FileParser.Records
{
    internal interface IRecord
    {
        RecordType RecordType { get; }
        void WriteRecord(BinaryWriter writer);  // TODO: split to internal interface
        void FillRecord(BinaryReader reader);
        void RegisterMetadata(MetaData metaData);
    }


}