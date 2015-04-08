using System;

namespace SpssLib.SpssDataset
{
	[Obsolete("Use Record.GetValue(object) for sanitized value")]
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
	            return variable.GetValue(value);
            }
        }
        
    }
}