using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace WaveLibrary
{
    class WavedataSubChunk
    {
        string SubChunk2ID = "data";
        int SubChunk2Size;
        byte[] SoundData;

        public WavedataSubChunk(int NumSamples, int NumChannels, int BitsPerSample, byte[] SoundData)
        {
            SubChunk2Size = NumSamples * NumChannels * (BitsPerSample / 8);
            this.SoundData = SoundData;
        }

        public void WriteData(FileStream fs)
        {
            //Chunk ID
            byte[] _subChunk2ID = Encoding.ASCII.GetBytes(SubChunk2ID);
            fs.Write(_subChunk2ID, 0, _subChunk2ID.Length);

            //Chunk Size
            byte[] _subChunk2Size = BitConverter.GetBytes(SubChunk2Size);
            fs.Write(_subChunk2Size, 0, _subChunk2Size.Length);

            //Wave Sound Data
            fs.Write(SoundData, 0, SoundData.Length);
        }

        public int Size { get { return SubChunk2Size; } }
    }
}
