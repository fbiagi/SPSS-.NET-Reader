using SpssLib.SpssDataset;

namespace SpssLib.FileParser.Records
{
    public class VariableDisplayInfo
    {
        public MeasurementType MeasurementType { get; internal set; }
        public int Width { get; internal set; }
        public Alignment Alignment { get; internal set; }

        internal VariableDisplayInfo()
        {
        }
    }
}
