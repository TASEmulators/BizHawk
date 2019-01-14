namespace SharpCompress.Compressors.Rar.UnpackV1.Decode
{
    internal class LowDistDecode : Decode
    {
        internal LowDistDecode()
            : base(new int[PackDef.LDC])
        {
        }
    }
}