namespace SpssLib.SpssDataset
{
    public class RecordMissingCategoryValuesAsNull 
    {
        private readonly object[] _data;

        public RecordMissingCategoryValuesAsNull(object[] data)
        {
            _data = data;
        }

        private object this[int index]
        {
            get
            {
                return _data[index];
            }
        }

        public object this[Variable variable]
        {
            get
            {
                var value = this[variable.Index];
                if(value == null) return value;
                if(!(value is double)) return value;
                var castedValue = (double) value;
                if(variable.MissingValues.Contains(castedValue))
                {
                    return null;
                }
                return value;
            }
        }
        
    }
}