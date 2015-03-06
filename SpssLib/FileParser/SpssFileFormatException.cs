using System;

namespace SpssLib.FileParser
{
    [Serializable]
    public class SpssFileFormatException: Exception
    {
        public int DictionaryIndex { get; set; }

        public SpssFileFormatException()
        {}

        public SpssFileFormatException(string message)
            : base(message)
        {}

        public SpssFileFormatException(string message, int dictionaryIndex) 
            : base(message+". Dictionary index "+dictionaryIndex)
        {
            DictionaryIndex = dictionaryIndex;
        }
    }
}