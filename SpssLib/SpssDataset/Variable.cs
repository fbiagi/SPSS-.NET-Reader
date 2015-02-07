using System;
using System.Collections.Generic;

namespace SpssLib.SpssDataset
{
    public class Variable
    {
		private static DateTime _epoc = new DateTime(1582, 10, 14, 0, 0, 0, DateTimeKind.Unspecified);

        public MeasurementType MeasurementType { get; set; }
        public int Width { get; set; }
        public int TextWidth { get; set; }
        public Alignment Alignment { get; set; }
        // TODO ShortName should be created and handled allways from VariableRecord, this class shouldn't know about it
        [Obsolete("Should not be needed on this class. Only needed for internal SPSS format issues.")]
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
            MissingValues = new double[3];
            ValueLabels = new Dictionary<double, string>();
        }

		/// <summary>
		/// Gets the proper value of this variable. This method will check the missing values
		/// in case there are, and will return null in case the value is one of them.
		/// Also, if the format fo this variable is a date, it will be tranformed into a <see cref="DateTime"/>.
		/// </summary>
		/// <param name="value">A value that should be of this variable</param>
		/// <returns>The value as object</returns>
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

			var cleanValue = MissingValueType == 0 ? value : GetWithMissingValueAsNull(value);
		    return cleanValue != null && IsDate() ? AsDate(cleanValue) : cleanValue;
	    }

		public bool IsDate()
	    {
		    var format = WriteFormat.FormatType;
		    return format == FormatType.ADATE
		           || format == FormatType.DATE
		           || format == FormatType.DATETIME
		           || format == FormatType.EDATE
		           || format == FormatType.JDATE
		           || format == FormatType.SDATE;

	    }

		private DateTime AsDate(object value)
		{
			var dVal = (double)value;
			return _epoc.AddSeconds(dVal);
		}

		public static double GetValueFromDate(DateTime date)
		{
			var span = date.Subtract(_epoc);
			return span.TotalSeconds;
		}

	    private object GetWithMissingValueAsNull(object value)
	    {
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
		    }
		    else
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
