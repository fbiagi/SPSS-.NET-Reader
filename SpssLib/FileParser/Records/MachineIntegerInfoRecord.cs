using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpssLib.FileParser.Records
{
    public class MachineIntegerInfoRecord
    {
        private InfoRecord record;

        public int VersionMajor
        {
            get
            {
                return BitConverter.ToInt32(record.Items[0], 0);
            }
        }
        public int VersionMinor
        {
            get
            {
                return BitConverter.ToInt32(record.Items[1], 0);
            }
        }
        public int VersionRevision
        {
            get
            {
                return BitConverter.ToInt32(record.Items[2], 0);
            }
        }
        public int MachineCode
        {
            get
            {
                return BitConverter.ToInt32(record.Items[3], 0);
            }
        }
        public int FloatingPointRepresentation   // TODO: make enum
        {
            get
            {
                return BitConverter.ToInt32(record.Items[4], 0);
            }
        }
        public int CompressionCode
        {
            get
            {
                return BitConverter.ToInt32(record.Items[5], 0);
            }
        }
        public int Endianness
        {
            get
            {
                return BitConverter.ToInt32(record.Items[6], 0);
            }
        }
        public int CharacterCode
        {
            get
            {
                return BitConverter.ToInt32(record.Items[7], 0);
            }
        }
        
        internal MachineIntegerInfoRecord(InfoRecord record)
        {
            if (record.SubType != 3 || record.ItemSize != 4 || record.ItemCount != 8)
                throw new UnexpectedFileFormatException();
            this.record = record;
        }
    }
}
