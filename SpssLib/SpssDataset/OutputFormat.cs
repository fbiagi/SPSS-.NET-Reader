using System;

namespace SpssLib.SpssDataset
{
    public class OutputFormat
    {
        public int DecimalPlaces { get; private set; }
        public int FieldWidth { get; private set; }
        public FormatType FormatType { get; private set; }
		
		public OutputFormat(int decimalPlaces, int fieldWidth ,FormatType formatType)
		{
			DecimalPlaces = decimalPlaces;
			FieldWidth = fieldWidth;
			FormatType = formatType;
		}

        public OutputFormat(Int32 formatValue)
        {
            byte[] formatBytes = BitConverter.GetBytes(formatValue);
            this.DecimalPlaces = (int)formatBytes[0];
            this.FieldWidth = (int)formatBytes[1];
            this.FormatType = (FormatType)formatBytes[2];
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
