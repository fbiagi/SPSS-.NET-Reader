using System.Text;
using SpssLib.SpssDataset;

namespace SpssLib.DataReader
{
    /// <summary>
    /// Contains general file metadata, as found in the file header
    /// </summary>
	public class SpssOptions
	{
        /// <summary>
        /// The file label or title
        /// </summary>
		public string Label { get; set; }
        /// <summary>
        /// Whether the data on the files is compressed or not. Only numerical whole values can be compressed
        /// </summary>
		public bool Compressed { get; set; }
        /// <summary>
        /// A bias used for the compression of numerical values. By default set to 100.<para/>
        /// Only integers between (1 - bias) and (251 - bias) will be compressed into only one byte.
        /// If the number has decimals or is not in that range, it will be written as a 8-byte double
        /// </summary>
		public long Bias { get; set; }
        /// <summary>
        /// The variable used to weight cases
        /// </summary>
		public Variable WeightVariable { get; set; }
        /// <summary>
        /// Number of cases in file, or -1 if unknown.
        /// This lib does not determine the amount of cases when writting. if you know how many records you'll
        /// write, you should set this fild with that number, otherwise it will remain -1
        /// </summary>
        public int Cases { get; set; }
        /// <summary>
        /// The encoding used to read/write the header of the file
        /// </summary>
        public Encoding HeaderEncoding { get; set; }
        /// <summary>
        /// The encoding used to read/write the cases
        /// </summary>
        public Encoding DataEncoding { get; set; }
		
        /// <summary>
        /// Creates a Spss options instance with defaults.
        /// Compressed, bias=100 &amp; encodings=UTF8
        /// </summary>
		public SpssOptions()
		{
			// Default values
			Compressed = true;
			Bias = 100;
			Cases = -1;
		    HeaderEncoding = Encoding.UTF8;
            DataEncoding = Encoding.UTF8;
		}

        // TODO Read-only informational prod_name, layout_code, nominal_case_size, creation_date, creation_time

    }
}