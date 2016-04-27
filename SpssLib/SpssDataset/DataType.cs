
namespace SpssLib.SpssDataset
{
    /// <summary>
    /// The SPSS data type
    /// </summary>
    public enum DataType
    {
        /// <summary>
        /// Numeric data. Value should be a double (or null for SYSMISS)
        /// </summary>
        Numeric = 0,
        /// <summary>
        /// Text data. Value should be a string
        /// </summary>
        Text = 1
    }
}
