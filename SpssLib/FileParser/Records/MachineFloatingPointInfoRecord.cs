using System;
using System.IO;

namespace SpssLib.FileParser.Records
{
    public class MachineFloatingPointInfoRecord : BaseInfoRecord
    {
        public override int SubType => InfoRecordType.MachineFloatingPoint;

        public double SystemMissingValue { get; private set; }
        public double MissingHighestValue { get; private set; }
        public double MissingLowestValue { get; private set; }

        /// <summary>
        /// Constructor for creating an appropiate floating point record for this machine
        /// </summary>
        internal MachineFloatingPointInfoRecord()
        {
            ItemSize = 8;
            ItemCount = 3;
            SystemMissingValue = double.MinValue;
            MissingHighestValue = double.MaxValue;
            MissingLowestValue = BitConverter.ToDouble(BitConverter.GetBytes(0xffeffffffffffffe),0); // Second largest negative double. Is there a better way to calculate this?
        }

        public override void RegisterMetadata(MetaData metaData)
        {
            metaData.FloatingPointInfo = this;
        }

        protected override void WriteInfo(BinaryWriter writer)
        {
            writer.Write(SystemMissingValue);
            writer.Write(MissingHighestValue);
            writer.Write(MissingLowestValue);
        }

        protected override void FillInfo(BinaryReader reader)
        {
            CheckInfoHeader(8, 3);

            SystemMissingValue = reader.ReadDouble();
            MissingHighestValue = reader.ReadDouble();
            MissingLowestValue = reader.ReadDouble();
        }
    }
}
