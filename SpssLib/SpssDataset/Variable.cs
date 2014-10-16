using System.Collections.Generic;

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
		
		public int MissingValueType { get; set; }
        public double[] MissingValues { get; private set;} 
        public IDictionary<double, string> ValueLabels { get; set; }

        public int Index { get; internal set; }
        
        public Variable()
        {
            this.MissingValues = new double[3];
            this.ValueLabels = new Dictionary<double, string>();
        }

	    public object GetValue(object value)
	    {
			// TODO use strategy pattern to evaluate value (replace MissingValues for strategy impl object)
			
			if (value == null)
			{
				return null;
			}
			
		    if (Type != DataType.Numeric)
		    {
			    var s = value.ToString().Trim();
				return s.Length == 0 ? null : s;
		    }
			
			if (MissingValueType == 0)
			{
				return value;
			}

			// ReSharper disable CompareOfFloatsByEqualityOperator
			// Comparisons are for exact value, as missing values have to be written in
			var dVal = (double) value;
			if (MissingValueType > 0)
			{
				for (int i = 0; i < MissingValueType && i < MissingValues.Length; i++)
				{

					if (dVal == MissingValues[i])

					{
						return null;
					}
				}
			} else 
			{
				if (dVal >= MissingValues[0] && dVal <= MissingValues[1])
				{
					return null;
				}
				if (MissingValueType == -3 && dVal == MissingValues[3])
				{
					return null;
				}
			}
			// ReSharper restore CompareOfFloatsByEqualityOperator		    
			return value;
	    }
    }
}
