namespace SpssLib.FileParser.Records
{
    public static class InfoRecordType
    {
        public const int MachineInteger = 3;
        public const int MachineFloatingPoint = 4;
        public const int GroupedVariables = 5;              // not sure, not implemented
        public const int DateInfo = 6;                      // not sure, not implemented
        public const int MultipleResponseSets = 7;          // TODO implement may be?
        public const int VariableDisplayParameter = 11;     // TODO implement writing
        public const int LongVariableNames = 13;
        public const int VeryLongString = 14;         // TODO implement, VLS vars might look like multiple vars, find what should happend with variable indexes
        public const int ExtendedNumberOfCases = 16;        // TODO implement may be?
        public const int DataFileAttributes = 17;           // TODO implement may be?
        public const int VariableAttributes = 18;           // TODO implement may be?
        public const int MultipleResponseSetsV14 = 19;      // TODO implement may be?
        public const int CharacterEncoding = 20;            // TODO implement reading
        public const int LongStringValueLabels = 21;        // TODO implement
    }
}