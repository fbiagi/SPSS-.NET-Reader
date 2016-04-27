namespace SpssLib.FileParser
{
	public interface IRecordWriter
	{
		/// <summary>
		/// Writes a sysmiss value on the stream.
		/// This is to be used for null values mostly
		/// </summary>
		void WriteSysMiss();

		/// <summary>
		/// Writes a numeric value to the file
        /// When implemented, it should also check if the last block is complete.
		/// </summary>
		/// <param name="d">The numeric value to be written</param>
		void WriteNumber(double d);

        /// <summary>
        /// This method should be called when starting to write a string variable.
        /// When implemented, it should check if the last block is complete.
        /// </summary>
        void StartString();

	    /// <summary>
	    /// Writes bytes that correspond to chars in a string from a buffer.
	    /// The caller method should keep track of the segments itself and the max length 
	    /// in bytes that should be written according to the variable info
	    /// </summary>
	    /// <param name="bytes">The byte array to copy from</param>
	    /// <param name="start">The starting position from where to copy</param>
	    /// <param name="length">Ammount of bytes to write from the buffer</param>
	    void WriteCharBytes(byte[] bytes, int start = 0, int length = Constants.BLOCK_BYTE_SIZE);

        /// <summary>
        /// Writes the end of a variable.
        /// When implemented, it should fill the last block with padding spaces and fill the rest
        /// of the length of the variables with padding spaces blocks (if needed).
        /// </summary>
        /// <param name="writtenBytes"></param>
        /// <param name="length"></param>
        void EndStringVariable(int writtenBytes, int length);

	    /// <summary>
		/// Writes an end of file, if needed
		/// </summary>
		void EndFile();

	    
	    
	}
}