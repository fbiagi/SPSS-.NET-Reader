using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpssLib.FileParser.Records
{
    public class MachineFloatingPointInfoRecord
    {
        private InfoRecord record;
        internal MachineFloatingPointInfoRecord(InfoRecord record)
        {
            if (record.SubType != 4 || record.ItemSize != 8 || record.ItemCount != 3)
                throw new UnexpectedFileFormatException();
            this.record = record;
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
    }
}
