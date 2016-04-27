using System.Text;

namespace SpssLib
{
    static class Constants
    {
        public const int BLOCK_BYTE_SIZE = 8;

        /// <summary>
        /// The encoding to use for reading and writing the file with stream readers/writers.
        /// This is not the actual data or header encoding
        /// </summary>
        public static readonly Encoding BaseEncoding = Encoding.ASCII;
    }
}
