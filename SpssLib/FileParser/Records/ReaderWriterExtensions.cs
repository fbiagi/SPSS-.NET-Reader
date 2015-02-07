using System;
using System.IO;

namespace SpssLib.FileParser.Records
{
    internal static class ReaderWriterExtensions
    {
        public static RecordType ReadRecordType(this BinaryReader reader)
        {
            int recordTypeNum = reader.ReadInt32();
            if (!Enum.IsDefined(typeof (RecordType), recordTypeNum))
            {
                throw new SpssFileFormatException("Record type not recognized: "+recordTypeNum);
            }

            return (RecordType)Enum.ToObject(typeof(RecordType), recordTypeNum);
        }

        public static void Write(this BinaryWriter writer, RecordType type)
        {
            writer.Write((int) type);
        }
    }
}