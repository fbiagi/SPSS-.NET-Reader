
namespace SpssLib.FileParser.Records
{
    public enum RecordType
    {
        HeaderRecord = 0x324C4624, // ASCII file header, in ascii chars: $FL2 
        EbcdicHeaderRecord = 0x5BC6D3F2, // EBCDIC file header would have this value
        VariableRecord = 2,
        ValueLabelRecord = 3,
        ValueLabelVariablesRecord = 4,
        DocumentRecord = 6,
        InfoRecord = 7,
        End = 999
    }
}
