namespace SharpCompress.Compressors.LZMA.Utilites
{
    internal interface IPasswordProvider
    {
        string CryptoGetTextPassword();
    }
}