using System;

namespace SpssLib.FileParser
{
    [Serializable]
    public class SpssFileFormatException: Exception
    {
        public SpssFileFormatException() : base()
        {
            
        }

        public SpssFileFormatException(string message) : base(message)
        {
            
        }
    }
}