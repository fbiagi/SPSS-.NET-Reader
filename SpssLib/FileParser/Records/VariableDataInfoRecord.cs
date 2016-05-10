using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SpssLib.FileParser.Records
{
    public abstract class VariableDataInfoRecord<T> : BaseInfoRecord
    {
        protected virtual bool UsesTerminator => false;

        private IDictionary<string, T> _dictionary;
        // ReSharper disable StaticFieldInGenericType // Doesn't need to be shared
        private static readonly byte EqualsChar = Encoding.ASCII.GetBytes("=")[0];
        private static readonly byte TabChar = Encoding.ASCII.GetBytes("\t")[0];
        // ReSharper restore StaticFieldInGenericType

        /// <summary>
        /// Holds the encoded byte sequence for the long names dictionary string
        /// </summary>
        protected byte[] Data { get; set; }
        
        public VariableDataInfoRecord(IDictionary<string, T> dictionary, Encoding encoding)
		{
		    Encoding = encoding;
		    ItemSize = 1;
            Dictionary = dictionary;
            BuildDataArray();
		    ItemCount = Data.Length;
		}

        protected abstract T DecodeValue(string stringValue);
        protected abstract string EncodeValue(T value);

        public IDictionary<string, T> Dictionary
        {
            get
            {
                if (_dictionary == null)
                {
                    if (Data == null || Data.Length == 0)
                    {   
                        throw new SpssFileFormatException("Info record dictionary has no data");
                    }

                    _dictionary = new Dictionary<string, T>();

                    var startIndex = 0;
                    do
                    {
                        var separatorIndex = Array.IndexOf(Data, EqualsChar, startIndex);
                        if (separatorIndex == -1)
                        {   
                            throw new SpssFileFormatException("Info record dictionary has no '=' char");
                        }
                        var endIndex = Array.IndexOf( Data, TabChar, separatorIndex);
                        string shortName = Encoding.GetString(Data, startIndex, separatorIndex - startIndex);
                        string stringValue = Encoding.GetString(Data, separatorIndex + 1, (endIndex != -1 ? endIndex : Data.Length) - separatorIndex - 1);

                        _dictionary.Add(shortName, DecodeValue(stringValue));

                        startIndex = endIndex + 1;
                    } while (startIndex > 0 && startIndex+1 < Data.Length);
                }

                return _dictionary;
            }
            protected set { _dictionary = value; }
        }

        protected override void WriteInfo(BinaryWriter writer)
        {
            writer.Write(Data);
        }

        protected void BuildDataArray()
        {
            var buffer = new byte[_dictionary.Count*(8 + 2 + 64)];
            int byteIndex = 0;
            foreach (var variable in _dictionary)
            {
                byteIndex += Encoding.GetBytes(variable.Key, 0, variable.Key.Length, buffer, byteIndex);
                buffer[byteIndex++] = EqualsChar;
                byteIndex += Encoding.GetUpToMaxLenght(EncodeValue(variable.Value), 64, buffer, byteIndex);
                buffer[byteIndex++] = TabChar;
            }

            if (!UsesTerminator)
            {
                // Do not account for the triling tab
                byteIndex--;
            }

            // Trim the excess of the array
            Array.Resize(ref buffer, byteIndex);
            Data = buffer;
        }
        
        protected override void FillInfo(BinaryReader reader)
        {
            CheckInfoHeader(1);
            Data = reader.ReadBytes(ItemCount);
        }
    }
}