using System;
using System.IO;

namespace SpssLib.FileParser.Records
{
    public abstract class BaseInfoRecord : EncodeEnabledRecord, IRecord
    {
        public RecordType RecordType => RecordType.InfoRecord;
        public abstract int SubType { get; }

        protected int ItemSize;
        protected int ItemCount;

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

    public class UnknownInfoRecord : BaseInfoRecord
    {
        private readonly int _subType;

        public byte[] Data { get; private set; }

        internal UnknownInfoRecord(int subType)
        {
            _subType = subType;
        }

        internal UnknownInfoRecord(int subType, int itemSize, int itemCount)
        {
            _subType = subType;
            ItemSize = itemSize;
            ItemCount = itemCount;
        }

        public override int SubType => _subType;

        protected override void WriteInfo(BinaryWriter writer)
        {
            // TODO: check data lenght
            writer.Write(Data);
        }

        protected override void FillInfo(BinaryReader reader)
        {
            Data = reader.ReadBytes(ItemCount * ItemSize);
        }

        public byte[] this[int i]
        {
            get
            {
                if (ItemSize * i > Data.Length)
                {
                    throw new IndexOutOfRangeException();
                }
                byte[] result = new byte[ItemSize];
                Buffer.BlockCopy(Data, i * ItemSize, result, 0, ItemSize);
                return result;
            }
        }
    }
}