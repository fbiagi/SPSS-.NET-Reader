using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SpssLib.FileParser.Records
{
    public class CharacterEncodingRecord : BaseInfoRecord
    {
        public override int SubType => InfoRecordType.CharacterEncoding;

        public string Name { get; private set; }
        
        public Encoding Encoding { get; private set; }

        internal CharacterEncodingRecord()
        {}

		internal CharacterEncodingRecord(Encoding encoding)
		{
            ItemSize = 1;
            // Supposedly has to be the IANA name
			Name = encoding.WebName;
		    ItemCount = Name.Length;

		    Encoding = encoding;
		}

        public override void RegisterMetadata(MetaData metaData)
        {
            metaData.CharEncodingRecord = this;
            metaData.DataEncoding = Encoding;
        }

        protected override void WriteInfo(BinaryWriter writer)
		{
            var bytes = Encoding.ASCII.GetBytes(Name);
			writer.Write(bytes);
		}

        protected override void FillInfo(BinaryReader reader)
        {
            CheckInfoHeader(1); // items must be of size 1 (byte)

            // TODO test if ReadString will work
            var nameBytes = reader.ReadBytes(ItemCount);
            Name = Encoding.ASCII.GetString(nameBytes);
            Encoding = GetEncoding(Name);
        }
        
        /// <summary>
        /// Gets the encoding, by trying to guess it from the string.
        /// </summary>
        /// <param name="strEncoding">Encoding name as written on the record</param>
        /// <returns>The guessed Encoding</returns>
        /// <remarks>
        /// This method tryies to guess the encoding base on what's written on the record.
        /// I will first try to look the encoding with the same name (case insencitive),
        /// this should catch Windows-1252, utf-8, etc.
        /// PSPP writes CP1252 that's not recognized, so in case of not finding the encoding,
        /// it will take all the numbers on the string and try to look it up by code page.
        /// </remarks>
        private Encoding GetEncoding(string strEncoding)
        {
            // Try to get the encoding by EncodingInfo name
            var encInfo = Encoding.GetEncodings();
            var info = encInfo.SingleOrDefault(ei => ei.Name.Equals(strEncoding, StringComparison.InvariantCultureIgnoreCase));
            if (info != null)
                return info.GetEncoding();

            // Try to get encoding by name or alias
            try
            {
                var enc = Encoding.GetEncoding(strEncoding);
                return enc;
            }
            catch (ArgumentException)
            {}
            
            // Try to get encoding parsing codepage
            int cp;
            if (int.TryParse(Regex.Match(strEncoding, @"\d+").Value, out cp))
            {
                info = encInfo.SingleOrDefault(ei => ei.CodePage == cp);
                if (info != null)
                    return info.GetEncoding();
            }
            
            if (strEncoding.Equals("windows-31j", StringComparison.InvariantCultureIgnoreCase))
            {
                // 932 - Japanese (Shift-JIS)
                return Encoding.GetEncoding(932); 
            }

            throw new SpssFileFormatException("Encoding not recognized: " + strEncoding);
        }
	}
}