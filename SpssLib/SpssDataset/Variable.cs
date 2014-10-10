using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpssLib.FileParser;
using System.Collections.ObjectModel;

namespace SpssLib.SpssDataset
{
    public class Variable
    {
        public MeasurementType MeasurementType { get; set; }
        public int Width { get; set; }
        public int TextWidth { get; set; }
        public Alignment Alignment { get; set; }
        public string ShortName { get; set; }
        public string Name { get; set; }
        public string Label { get; set; }
        public OutputFormat PrintFormat { get; set; }
        public OutputFormat WriteFormat { get; set; }
        public DataType Type { get; set; }
		// TODO this is an oversimplification of missing values. Ranges can't be set like this
        public ICollection<double> MissingValues { get; set;} 
        public IDictionary<double, string> ValueLabels { get; set; }

        public int Index { get; internal set; }
        
        public Variable()
        {
            this.MissingValues = new Collection<double>();
            this.ValueLabels = new Dictionary<double, string>();
        }

    }
}
