namespace SharpCompress.Compressors.Rar.UnpackV1.Decode
{
    internal enum CodeType
    {
        CODE_HUFFMAN,
        CODE_LZ,
        CODE_LZ2,
        CODE_REPEATLZ,
        CODE_CACHELZ,
        CODE_STARTFILE,
        CODE_ENDFILE,
        CODE_VM,
        CODE_VMDATA
    }
}