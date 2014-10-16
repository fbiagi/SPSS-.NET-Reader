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
		/// Gets the value of a variable for this record. This method takes into account the varaible
		/// missing values.
		/// </summary>
		/// <param name="variable"></param>
		/// <returns></returns>
		public object GetValue(Variable variable)
		{
			var value = this[variable];
			return variable.GetValue(value);
		}
    }
}
