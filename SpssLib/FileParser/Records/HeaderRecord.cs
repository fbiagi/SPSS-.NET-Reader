using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace SpssLib.FileParser.Records
{
    public class HeaderRecord
    {
        // char[60] prod_name
        public string ProductName { get; private set; }
        public Int32 LayoutCode { get; private set; }
        public Int32 NominalCaseSize { get; private set; }
        public bool Compressed { get; private set; }
        public Int32 WeightIndex { get; private set; }
        public Int32 CasesCount { get; private set; }
        public Double Bias { get; private set; }
        public string CreationDate { get; private set; }
        public string CreationTime { get; private set; }
        // char[64] file_label
        public string FileLabel { get; private set; }
        // char[3] padding
        public string Padding { get; private set; }

        private HeaderRecord()
        {
        }

        public static HeaderRecord ParseNextRecord(BinaryReader reader)
        {            
            var record = new HeaderRecord();

            record.ProductName = new String(reader.ReadChars(60));
            record.LayoutCode = reader.ReadInt32();
            record.NominalCaseSize = reader.ReadInt32();
            record.Compressed = (reader.ReadInt32() == 1);
            record.WeightIndex = reader.ReadInt32();
            record.CasesCount = reader.ReadInt32();
            record.Bias = reader.ReadDouble();
            record.CreationDate = new String(reader.ReadChars(9));
            record.CreationTime = new String(reader.ReadChars(8));
            record.FileLabel = new String(reader.ReadChars(64));
            record.Padding = new String(reader.ReadChars(3));

            return record;
        }
    }
}
