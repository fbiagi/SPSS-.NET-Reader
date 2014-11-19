using System;

namespace SpssLib.SpssDataset
{
    public class Record
    {
        private object[] data;

        internal Record(object[] data)
        {
            this.data = data;
        }

        public object this[int index]
        {
            get
            {
                return data[index];
            }
        }

        public object this[Variable variable]
        {
            get
            {
                return this[variable.Index];
            }
        }

		/// <summary>
		/// Gets the proper value of the variable for this record. This method will check the missing values
		/// in case there are, and will return null in case the value is one of them.
		/// Also, if the format fo this variable is a date, it will be tranformed into a <see cref="DateTime"/>.
		/// </summary>
		/// <param name="variable">The variable to get the value from</param>
		/// <returns>The value for the variable on this record as object</returns>
		public object GetValue(Variable variable)
		{
			var value = this[variable];
			return variable.GetValue(value);
		}
    }
}
