using System;
using System.IO;
using System.Collections.ObjectModel;

namespace SpssLib.FileParser.Records
{
    public class InfoRecord
    {
        public int SubType { get; private set; }
        public int ItemSize { get; private set; }
        public int ItemCount { get; private set; }
        public Collection<byte[]> Items { get; private set; }

        private InfoRecord()
        {
        }

        internal InfoRecord(int subtype, int itemSize, int itemCount, Collection<byte[]> items)
        {
            SubType = subtype;
            ItemSize = itemSize;
            ItemCount = itemCount;
            Items = items;
        }

        public static InfoRecord ParseNextRecord(BinaryReader reader)
        {
            var record = new InfoRecord
                         {
                             SubType = reader.ReadInt32(),
                             ItemSize = reader.ReadInt32(),
                             ItemCount = reader.ReadInt32(),
                             Items = new Collection<byte[]>()
                         };

            for (int i = 0; i < record.ItemCount; i++)
            {
                record.Items.Add(reader.ReadBytes(record.ItemSize));
            }

            return record;
        }

    }


}
