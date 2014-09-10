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
    }
}
