using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Specialized;

namespace SpssLib.FileParser.Records
{
    public class DocumentRecord
    {
        public Int32 LineCount { get; private set; }
        public StringCollection LineCollection { get; private set; }

        private DocumentRecord()
        {
        }

        public static DocumentRecord ParseNextRecord(BinaryReader reader)
        {
            var record = new DocumentRecord();

            record.LineCount = reader.ReadInt32();
            record.LineCollection = new StringCollection();
            for (int i = 0; i < record.LineCount; i++)
            {
                record.LineCollection.Add(new String(reader.ReadChars(80)));
            }

            return record;
        }
    }
}
