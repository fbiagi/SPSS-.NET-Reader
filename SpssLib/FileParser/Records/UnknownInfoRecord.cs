using System;
using System.IO;

namespace SpssLib.FileParser.Records
{
    public class UnknownInfoRecord : BaseInfoRecord
    {
        private readonly int _subType;

        public byte[] Data { get; private set; }

        public UnknownInfoRecord()
        {
            
        }

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