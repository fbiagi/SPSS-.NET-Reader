using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SpssLib.SpssDataset
{
    public class OutputFormat
    {
        public int DecimalPlaces { get; private set; }
        public int FieldWidth { get; private set; }
        public FormatType FormatType { get; private set; }

        public OutputFormat(Int32 formatValue)
        {
            byte[] formatBytes = BitConverter.GetBytes(formatValue);
            this.DecimalPlaces = (int)formatBytes[0];
            this.FieldWidth = (int)formatBytes[1];
            this.FormatType = (FormatType)formatBytes[2];
        }

    }
}
