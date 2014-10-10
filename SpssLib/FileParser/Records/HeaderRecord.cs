using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using SpssLib.DataReader;

namespace SpssLib.FileParser.Records
{
    public class HeaderRecord : IBaseRecord
    {
	    public int RecordType
	    {
			get { return 0x324C4624; } // in chars: $FL2 
	    }

        // char[60] prod_name
        public string ProductName { get; private set; }
        public Int32 LayoutCode { get; private set; }
        public Int32 NominalCaseSize { get; set; }
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

		internal HeaderRecord(SpssOptions options)
		{
			var assemblyName = GetType().Assembly.GetName();
			this.ProductName = ("@(#) SPSS DATA FILE " + assemblyName.Name + " " + assemblyName.Version);
			this.LayoutCode = 2;
			this.Compressed = options.Compressed;
			this.Bias = options.Bias;
			this.CasesCount = options.Cases;
			this.CreationDate = DateTime.Now.ToString("dd MMM yy", CultureInfo.InvariantCulture.DateTimeFormat);
			this.CreationTime = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture.DateTimeFormat);
			this.FileLabel = options.Label ?? string.Empty;
			this.Padding = string.Empty;
		}

	    public void WriteRecord(BinaryWriter writer)
	    {
		    writer.Write(RecordType);
			writer.Write(ProductName.PadRight(60, ' ').Substring(0, 60).ToCharArray());
			writer.Write(LayoutCode);
			writer.Write(NominalCaseSize);
			writer.Write((Int32)(Compressed ? 1 : 0 ));
			writer.Write(WeightIndex);
			writer.Write(CasesCount);
			writer.Write(Bias);
			writer.Write(CreationDate.Substring(0, 9).ToCharArray());
			writer.Write(CreationTime.Substring(0, 8).ToCharArray());
			writer.Write(FileLabel.PadRight(64, ' ').Substring(0, 64).ToCharArray());
			writer.Write(new byte[3]);
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

	internal interface IBaseRecord
	{
		Int32 RecordType { get; }
		void WriteRecord(BinaryWriter writer);
	}
}
