using System.IO;
using SharpCompress.Common.Rar.Headers;

namespace SharpCompress.Compressors.Rar
{
    internal interface IRarUnpack
    {
        void DoUnpack(FileHeader fileHeader, Stream readStream, Stream writeStream);
        void DoUnpack();

        // eg u/i pause/resume button
        bool Suspended { get; set; }

        long DestSize { get; }
        int Char { get; }
        int PpmEscChar { get; set; }
    }
}
