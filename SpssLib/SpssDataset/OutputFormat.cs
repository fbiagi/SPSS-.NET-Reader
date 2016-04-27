using System;

namespace SpssLib.SpssDataset
{
    /// <summary>
    /// Specifies a write/print format
    /// </summary>
    public class OutputFormat
    {
        /// <summary>
        /// Number of decimal places
        /// </summary>
        public int DecimalPlaces { get; }
        
        /// <summary>
        /// The display width of the filed
        /// </summary>
        public int FieldWidth { get; }
        /// <summary>
        /// The format type
        /// </summary>
        public FormatType FormatType { get; }

        /// <summary>
        /// Creates a write/print format specification
        /// </summary>
        /// <param name="formatType"></param>
        /// <param name="fieldWidth"></param>
        /// <param name="decimalPlaces"></param>
		public OutputFormat(FormatType formatType, int fieldWidth, int decimalPlaces = 0)
		{
			DecimalPlaces = decimalPlaces;
			FieldWidth = fieldWidth;
			FormatType = formatType;
		}

        internal OutputFormat(int formatValue)
        {
            byte[] formatBytes = BitConverter.GetBytes(formatValue);
            DecimalPlaces = formatBytes[0];
            FieldWidth = formatBytes[1];
            FormatType = (FormatType)formatBytes[2];
        }

		internal int GetInteger()
		{
			byte[] formatBytes = new byte[4];
			formatBytes[0] = (byte)DecimalPlaces;
			formatBytes[1] = (byte)FieldWidth;
			formatBytes[2] = (byte)FormatType;

			return BitConverter.ToInt32(formatBytes, 0);
		}
    }
}
