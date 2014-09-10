using System;
using System.Linq;
using System.Data;
using System.IO;
using SpssLib.FileParser;

namespace SpssLib.DataReader
{
    public class SpssDataReader: IDataReader
    {
        FileParser.SavFileParser parser;
        byte[][] currentRecord;

        public SpssDataReader(FileParser.SavFileParser parser)
        {
            this.parser = parser;
        }

        public SpssDataReader(Stream spssFileStream)
        {
            this.parser = new FileParser.SavFileParser(spssFileStream);
        }

        public MetaData FileMetaData
        {
            get
            {
                if (!parser.MetaDataParsed)
                    parser.ParseMetaData();
                return this.parser.MetaData;
            }
        }

        public void Close()
        {
            this.IsClosed = true;
            this.parser.Dispose();
        }

        public int Depth
        {
            get { return 0; }
        }

        public DataTable GetSchemaTable()
        {
            // throw new NotImplementedException();
            return null;
        }

        public bool IsClosed
        {
            get;
            private set;
        }

        public bool NextResult()
        {
            return false;
        }

        public bool Read()
        {
            this.currentRecord = parser.ReadNextDataRecord();
            return (currentRecord != null);
        }

        public int RecordsAffected
        {
            get { return 0; }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.parser != null)
                {
                    this.parser.Dispose();
                    this.parser = null;
                }
            }
        }

        public int FieldCount
        {
            get { return parser.Variables.Count; }
        }

        public bool GetBoolean(int i)
        {
            throw new NotSupportedException();
        }

        public byte GetByte(int i)
        {
            throw new NotSupportedException();
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotSupportedException();
        }

        public char GetChar(int i)
        {
            throw new NotSupportedException();
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotSupportedException();
        }

        public IDataReader GetData(int i)
        {
            throw new NotSupportedException();
        }

        public string GetDataTypeName(int i)
        {
            return this.parser.Variables[i].Type.ToString();
        }

        public DateTime GetDateTime(int i)
        {
            throw new NotSupportedException();
        }

        public decimal GetDecimal(int i)
        {
            throw new NotSupportedException();
        }

        public double GetDouble(int i)
        {
            var value = BitConverter.ToDouble(currentRecord[i], 0);
            if (value == this.FileMetaData.InfoRecords.MachineFloatingPointInfoRecord.SystemMissingValue)
                throw new InvalidOperationException("Value is sysmis");
            return value;
        }

        public Type GetFieldType(int i)
        {
            if (this.parser.Variables[i].Type == SpssDataset.DataType.Numeric)
            {
                return typeof(double);
            }
            else
            {
                return typeof(string);
            }
        }

        public float GetFloat(int i)
        {
            throw new NotSupportedException();
        }

        public Guid GetGuid(int i)
        {
            throw new NotSupportedException();
        }

        public short GetInt16(int i)
        {
            throw new NotSupportedException();
        }

        public int GetInt32(int i)
        {
            throw new NotSupportedException();
        }

        public long GetInt64(int i)
        {
            throw new NotSupportedException();
        }

        public string GetName(int i)
        {
            return this.parser.Variables[i].Name;
        }

        public int GetOrdinal(string name)
        {
            var variable =
                (from v in this.parser.Variables
                 where v.Name == name
                 select v)
                .FirstOrDefault();

            if (variable == null) throw new ArgumentException("Fieldname unknown", "name");

            return variable.Index;
        }

        public string GetString(int i)
        {
            throw new NotSupportedException();
        }

        public object GetValue(int i)
        {
            if(GetFieldType(i) == typeof(double))
            {
                var value =  this.GetDouble(i);
                if (value == this.FileMetaData.InfoRecords.MachineFloatingPointInfoRecord.SystemMissingValue)
                    return DBNull.Value;
                else
                    return value;
            }
            else
            {
                return this.GetString(i);
            }
        }

        public int GetValues(object[] values)
        {
            var record = parser.RecordToObjects(this.currentRecord).ToArray();
            record.CopyTo(values, 0);
            return record.Length;
        }

        public bool IsDBNull(int i)
        {
            return (GetValue(i) is DBNull);
        }

        public object this[string name]
        {
            get
            {
                return GetValue(GetOrdinal(name));
            }
        }

        public object this[int i]
        {
            get { return GetValue(i); }
        }
    }
}
