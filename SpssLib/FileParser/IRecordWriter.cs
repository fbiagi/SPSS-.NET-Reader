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
		/// </summary>
		/// <param name="d">The numeric value to be written</param>
		void WriteNumber(double d);

		void WriteString(string s, int width);

		/// <summary>
		/// Writes an end of file, if needed
		/// </summary>
		void EndFile();
	}
}