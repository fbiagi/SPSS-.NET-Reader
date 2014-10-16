using System;

namespace SpssLib.SpssDataset
{
    public class OutputFormat
    {
        public int DecimalPlaces { get; private set; }
        public int FieldWidth { get; private set; }
        public FormatType FormatType { get; private set; }

		public OutputFormat(FormatType formatType, int fieldWidth, int decimalPlaces = 0)
		{
			DecimalPlaces = decimalPlaces;
			FieldWidth = fieldWidth;
			FormatType = formatType;
		}

        internal OutputFormat(Int32 formatValue)
        {
            byte[] formatBytes = BitConverter.GetBytes(formatValue);
            DecimalPlaces = formatBytes[0];
            FieldWidth = formatBytes[1];
            FormatType = (FormatType)formatBytes[2];
        }

		public int GetInteger()
		{
			byte[] formatBytes = new byte[4];
			formatBytes[0] = (byte)DecimalPlaces;
			formatBytes[1] = (byte)FieldWidth;
			formatBytes[2] = (byte)FormatType;

			return BitConverter.ToInt32(formatBytes, 0);
		}
    }
}
