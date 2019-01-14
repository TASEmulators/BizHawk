namespace SharpCompress.Compressors.Rar.UnpackV1.Decode
{
    internal class AudioVariables
    {
        internal AudioVariables()
        {
            Dif = new int[11];
        }

        internal int[] Dif { get; }
        internal int ByteCount { get; set; }
        internal int D1 { get; set; }

        internal int D2 { get; set; }
        internal int D3 { get; set; }
        internal int D4 { get; set; }

        internal int K1 { get; set; }
        internal int K2 { get; set; }
        internal int K3 { get; set; }
        internal int K4 { get; set; }
        internal int K5 { get; set; }
        internal int LastChar { get; set; }
        internal int LastDelta { get; set; }
    }
}