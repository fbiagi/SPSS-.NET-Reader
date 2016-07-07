using System.IO;

namespace SpssLib.FileParser.Records
{
    public abstract class BaseInfoRecord : EncodeEnabledRecord, IRecord
    {
        public RecordType RecordType => RecordType.InfoRecord;
        public abstract int SubType { get; }

        protected int ItemSize;
        protected int ItemCount;

        public BaseInfoRecord()
        {
            
        }

        public void WriteRecord(BinaryWriter writer)
        {
            writer.Write(RecordType);
            writer.Write(SubType);
            writer.Write(ItemSize);
            writer.Write(ItemCount);

            WriteInfo(writer);
        }

        public void FillRecord(BinaryReader reader)
        {
            ItemSize = reader.ReadInt32();
            ItemCount = reader.ReadInt32();

            FillInfo(reader);
        }

        public virtual void RegisterMetadata(MetaData metaData)
        {
            metaData.InfoRecords.Add(this);
            Metadata = metaData;
        }
        
        protected void CheckInfoHeader(int itemSize = -1, int itemCount = -1)
        {
            if (itemSize >= 0 && itemSize != ItemSize)
            {
                throw new SpssFileFormatException($"Wrong info record subtype {ItemSize}. Expected {itemSize}.");
            }

            if (itemCount >= 0 && itemCount != ItemCount)
            {
                throw new SpssFileFormatException($"Wrong info record subtype {ItemCount}. Expected {itemCount}.");
            }
        }

        protected abstract void WriteInfo(BinaryWriter writer);
        protected abstract void FillInfo(BinaryReader reader);
    }
}