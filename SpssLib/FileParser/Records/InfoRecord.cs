using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.ObjectModel;

namespace SpssLib.FileParser.Records
{
    public class InfoRecord
    {
        public Int32 SubType { get; private set; }
        public Int32 ItemSize { get; private set; }
        public Int32 ItemCount { get; private set; }
        public Collection<byte[]> Items { get; private set; }

        private InfoRecord()
        {
        }

        public static InfoRecord ParseNextRecord(BinaryReader reader)
        {
            var record = new InfoRecord();

            record.SubType = reader.ReadInt32();
            record.ItemSize = reader.ReadInt32();
            record.ItemCount = reader.ReadInt32();

            record.Items = new Collection<byte[]>();

            for (int i = 0; i < record.ItemCount; i++)
            {
                record.Items.Add(reader.ReadBytes(record.ItemSize));
            }

            return record;
        }

    }


}
