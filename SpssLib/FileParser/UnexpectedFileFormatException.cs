using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpssLib.FileParser
{
    [Serializable]
    public class UnexpectedFileFormatException: Exception
    {
        public UnexpectedFileFormatException() : base()
        {
            
        }

        public UnexpectedFileFormatException(string message) : base(message)
        {
            
        }
    }

}
