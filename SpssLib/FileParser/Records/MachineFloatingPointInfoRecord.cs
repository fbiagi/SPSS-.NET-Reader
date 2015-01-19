using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace SpssLib.FileParser.Records
{
    public class MachineFloatingPointInfoRecord : IBaseRecord
    {
        private InfoRecord record;
        internal MachineFloatingPointInfoRecord(InfoRecord record)
        {
            if (record.SubType != 4 || record.ItemSize != 8 || record.ItemCount != 3)
                throw new UnexpectedFileFormatException();
            this.record = record;
        }

        /// <summary>
        /// Constructor for creating an appropiate floating point record for this machine
        /// </summary>
        internal MachineFloatingPointInfoRecord()
        {
            record = new InfoRecord(4, 8, 3, new Collection<byte[]>
                {
                    BitConverter.GetBytes(double.MinValue),
                    BitConverter.GetBytes(double.MaxValue),
                    BitConverter.GetBytes((double)0xffeffffffffffffe) // Second largest negative double. Is there a better way to calculate this?
                });

        }


        public double SystemMissingValue
        {
            get
            {
                return BitConverter.ToDouble(record.Items[0], 0);
            }
        }
        public double MissingHighestValue
        {
            get
            {
                return BitConverter.ToDouble(record.Items[1], 0);
            }
        }
        public double MissingLowestValue
        {
            get
            {
                return BitConverter.ToDouble(record.Items[2], 0);
            }
        }

        public int RecordType
        {
            get { return 7; }
        }

        public void WriteRecord(BinaryWriter writer)
        {
            writer.Write(RecordType);
            writer.Write(record.SubType);
            writer.Write(record.ItemSize);
            writer.Write(record.ItemCount);

            foreach (var item in record.Items)
            {
                writer.Write(item);
            }
        }
    }
}
