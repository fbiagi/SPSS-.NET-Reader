using System;
using System.Globalization;
using System.IO;
using SpssLib.DataReader;

namespace SpssLib.FileParser.Records
{
    public class HeaderRecord : IRecord
    {
	    public RecordType RecordType
	    {
			get { return RecordType.HeaderRecord; } 
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

        internal HeaderRecord()
        {
        }

		internal HeaderRecord(SpssOptions options)
		{
			var assemblyName = GetType().Assembly.GetName();
			ProductName = ("@(#) SPSS DATA FILE " + assemblyName.Name + " " + assemblyName.Version);
			LayoutCode = 2;
			Compressed = options.Compressed;
			Bias = options.Bias;
			CasesCount = options.Cases;
			CreationDate = DateTime.Now.ToString("dd MMM yy", CultureInfo.InvariantCulture.DateTimeFormat);
			CreationTime = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture.DateTimeFormat);
			FileLabel = options.Label ?? string.Empty;
			Padding = string.Empty;
		}

	    public void WriteRecord(BinaryWriter writer)
	    {
		    writer.Write((int)RecordType);
			writer.Write(ProductName.PadRight(60, ' ').Substring(0, 60).ToCharArray());
			writer.Write(LayoutCode);
			writer.Write(NominalCaseSize);
			writer.Write(Compressed ? 1 : 0 );
			writer.Write(WeightIndex);
			writer.Write(CasesCount);
			writer.Write(Bias);
			writer.Write(CreationDate.Substring(0, 9).ToCharArray());
			writer.Write(CreationTime.Substring(0, 8).ToCharArray());
			writer.Write(FileLabel.PadRight(64, ' ').Substring(0, 64).ToCharArray());
			writer.Write(new byte[3]);
	    }

        [Obsolete("Method should be deleted")]
        public static HeaderRecord ParseNextRecord(BinaryReader reader)
        {            
            var record = new HeaderRecord();
            record.FillRecord(reader);
            return record;
        }

        public void FillRecord(BinaryReader reader)
        {
            ProductName = new String(reader.ReadChars(60));
            LayoutCode = reader.ReadInt32();
            NominalCaseSize = reader.ReadInt32();
            Compressed = (reader.ReadInt32() == 1);
            WeightIndex = reader.ReadInt32();
            CasesCount = reader.ReadInt32();
            Bias = reader.ReadDouble();
            CreationDate = new String(reader.ReadChars(9));
            CreationTime = new String(reader.ReadChars(8));
            FileLabel = new String(reader.ReadChars(64));
            Padding = new String(reader.ReadChars(3));
        }

        public void RegisterMetadata(MetaData metaData)
        {
            metaData.HeaderRecord = this;
        }
    }
}
