using System.IO;

namespace SpssLib.FileParser.Records
{
    internal interface IRecordParser
    {
        RecordType Accepts { get; }
        IRecord ParseRecord(BinaryReader reader);
    }
}