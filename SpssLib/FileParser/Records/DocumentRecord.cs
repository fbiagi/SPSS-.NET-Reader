using System;
using System.Collections.Generic;
using System.IO;

namespace SpssLib.FileParser.Records
{
    public class DocumentRecord : IRecord
    {
        public RecordType RecordType => RecordType.DocumentRecord;
        public int LineCount { get; private set; }
        public IList<string> LineCollection { get; private set; }

        internal DocumentRecord()
        {
        }

        public DocumentRecord(IList<string> lines)
        {
            LineCollection = lines;
            LineCount = lines.Count;
        }

        public void FillRecord(BinaryReader reader)
        {
            LineCount = reader.ReadInt32();
            LineCollection = new List<string>();
            for (int i = 0; i < LineCount; i++)
            {
                LineCollection.Add(new string(reader.ReadChars(80)));
            }
        }

        public void RegisterMetadata(MetaData metaData)
        {
            metaData.DocumentRecord = this;
        }

        public void WriteRecord(BinaryWriter writer)
        {
            writer.Write((int)RecordType);
            writer.Write(LineCount);
            foreach (var line in LineCollection)
            {
                writer.Write(line);     // TODO proper encoding
            }
        }
    }
}
