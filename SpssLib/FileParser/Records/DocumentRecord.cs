using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace SpssLib.FileParser.Records
{
    public class DocumentRecord : IRecord
    {
        public RecordType RecordType { get { return RecordType.DocumentRecord; }}
        public Int32 LineCount { get; private set; }
        public IList<string> LineCollection { get; private set; }

        internal DocumentRecord()
        {
        }

        public DocumentRecord(IList<string> lines)
        {
            LineCollection = lines;
            LineCount = lines.Count();
        }

        [Obsolete]
        public static DocumentRecord ParseNextRecord(BinaryReader reader)
        {
            var record = new DocumentRecord();
            record.FillRecord(reader);
            return record;
        }

        public void FillRecord(BinaryReader reader)
        {
            LineCount = reader.ReadInt32();
            LineCollection = new List<string>();
            for (int i = 0; i < LineCount; i++)
            {
                LineCollection.Add(new String(reader.ReadChars(80)));
            }
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
