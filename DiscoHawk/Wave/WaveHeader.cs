using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace WaveLibrary
{
    class WaveHeader
    {
        string ChunkID = "RIFF";
        int ChunkSize = 0;
        string Format = "WAVE"; //Specifiy WAVE, AVI  could also be used for a RIFF format

        public WaveHeader()
        {

        }
        public void SetChunkSize(int fmtSubChunkSize, int dataSubChunkSize)
        {
            ChunkSize = 4 + (8 + fmtSubChunkSize) + (8 + dataSubChunkSize);
        }

        public void WriteHeader(FileStream fs)
        {
            //ChunkID
            byte[] riff = Encoding.ASCII.GetBytes(ChunkID);
            fs.Write(riff, 0, riff.Length);

            //Chunk Size
            byte[] chunkSize = BitConverter.GetBytes(ChunkSize);
            fs.Write(chunkSize, 0, chunkSize.Length);

            //Data Type
            byte[] wave = Encoding.ASCII.GetBytes(Format);
            fs.Write(wave, 0, wave.Length);
        }

        public int Chunk_Size { get { return ChunkSize; }}
    }
}
