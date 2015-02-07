using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SpssLib.FileParser.Records
{
    public class LongVariableNamesRecord : BaseInfoRecord
    {
        private IDictionary<string, string> _dictionary;
        public override int SubType { get { return InfoRecordType.LongVariableNames; }}

        /// <summary>
        /// Holds the encoded byte sequence for the long names dictionary string
        /// </summary>
        private byte[] Data { get; set; }

        private static readonly byte EqualsChar = Encoding.ASCII.GetBytes("=")[0];
        private static readonly byte TabChar = Encoding.ASCII.GetBytes("\t")[0];

        public LongVariableNamesRecord()
        {}

        // TODO: add encoding
		public LongVariableNamesRecord(Dictionary<string, string> variableLongNames, Encoding encoding)
		{
		    Encoding = encoding;
		    ItemSize = 1;
            Dictionary = variableLongNames;
            BuildDataArray();
		    ItemCount = Data.Length;
		}

        public override void RegisterMetadata(MetaData metaData)
        {
            metaData.LongVariableNames = this;
            Metadata = metaData;
        }
        
        public IDictionary<string, string> Dictionary
        {
            get
            {
                if (_dictionary == null)
                {
                    if (Data == null || Data.Length == 0)
                    {
                        throw new SpssFileFormatException("No long variable names data loaded");
                    }

                    _dictionary = new Dictionary<string, string>();

                    var startIndex = 0;
                    do
                    {
                        var separatorIndex = Array.IndexOf(Data, EqualsChar, startIndex);
                        if (separatorIndex == -1)
                        {
                            throw new SpssFileFormatException("Long variable format in wrong status");
                        }
                        var endIndex = Array.IndexOf(Data, TabChar, separatorIndex);
                        string shortName = Encoding.GetString(Data, startIndex, separatorIndex - startIndex);
                        string longName = Encoding.GetString(Data, separatorIndex + 1, (endIndex != -1 ? endIndex : Data.Length) - separatorIndex - 1);

                        _dictionary.Add(shortName, longName);

                        startIndex = endIndex + 1;
                    } while (startIndex > 0 && startIndex+1 < Data.Length);
                }

                return _dictionary;
            }
            private set { _dictionary = value; }
        }

        protected override void WriteInfo(BinaryWriter writer)
        {
            writer.Write(Data);
        }

        private void BuildDataArray()
        {
            var buffer = new byte[_dictionary.Count*(8 + 2 + 64)];
            int byteIndex = 0;
            foreach (var variable in _dictionary)
            {
                byteIndex += Encoding.GetBytes(variable.Key, 0, variable.Key.Length, buffer, byteIndex);
                buffer[byteIndex++] = EqualsChar;
                byteIndex += Encoding.GetUpToMaxLenght(variable.Value, 64, buffer, byteIndex);
                buffer[byteIndex++] = TabChar;
            }

            // Trim the excess of the array (and the trailing tab char)
            Array.Resize(ref buffer, byteIndex - 1);
            Data = buffer;
        }

        protected override void FillInfo(BinaryReader reader)
        {
            CheckInfoHeader(1);
            Data = reader.ReadBytes(ItemCount);
            
        }
    }
}
