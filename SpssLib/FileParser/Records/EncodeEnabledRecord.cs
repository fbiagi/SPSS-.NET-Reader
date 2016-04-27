using System.Text;

namespace SpssLib.FileParser.Records
{
    public abstract class EncodeEnabledRecord
    {
        private Encoding _headerEncoding;
        protected MetaData Metadata;

        /// <summary>
        /// Gets the encoding used for all the header records
        /// </summary>
        /// <returns>The encoding to use</returns>
        /// <remarks>
        /// This method tries to load the encoding from the metadata if is not already loaded.
        /// While writing the encoding must be properly set on the constructor, and when reading 
        /// it must be set after reading the <see cref="MachineIntegerInfoRecord"/>. Because of that,
        /// this method should not be invoked before that record is read.
        /// </remarks>
        internal Encoding Encoding
        {
            get
            {
                if (_headerEncoding == null)
                {
                    if (Metadata?.HeaderEncoding == null)
                    {
                        throw new SpssFileFormatException("Can not determine encoding.");
                    }
                    _headerEncoding = Metadata.HeaderEncoding;
                }
                return _headerEncoding;
            }
            set { _headerEncoding = value; }
        }
    }
}