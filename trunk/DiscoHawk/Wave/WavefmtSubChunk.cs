using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace WaveLibrary
{
    class WavefmtSubChunk
    {
        string SubChunk1ID = "fmt ";
        int Subchunk1Size = 16; //For PCM
        int AudioFormat = 1; //For no compression
        public int NumChannels = 2; //1 For Mono, 2 For Stereo
        int SampleRate = 22050;
        int ByteRate;
        int BlockAlign;
        public int BitsPerSample = 16;

        public WavefmtSubChunk(int channels, int bitsPerSamples, int sampleRate)
        {
            BitsPerSample = bitsPerSamples;
            NumChannels = channels;
            SampleRate = sampleRate;
            ByteRate = SampleRate * NumChannels * (BitsPerSample / 8);
            BlockAlign = NumChannels * (BitsPerSample / 8);
        }

        public void Writefmt(FileStream fs)
        {
            //Chunk ID
            byte[] _subchunk1ID = Encoding.ASCII.GetBytes(SubChunk1ID);
            fs.Write(_subchunk1ID, 0, _subchunk1ID.Length);

            //Chunk Size
            byte[] _subchunk1Size = BitConverter.GetBytes(Subchunk1Size);
            fs.Write(_subchunk1Size, 0, _subchunk1Size.Length);

            //Audio Format (PCM)
            byte[] _audioFormat = BitConverter.GetBytes(AudioFormat);
            fs.Write(_audioFormat, 0, 2);

            //Number of Channels (1 or 2)
            byte[] _numChannels = BitConverter.GetBytes(NumChannels);
            fs.Write(_numChannels, 0, 2);

            //Sample Rate
            byte[] _sampleRate = BitConverter.GetBytes(SampleRate);
            fs.Write(_sampleRate, 0, _sampleRate.Length);

            //Byte Rate
            byte[] _byteRate = BitConverter.GetBytes(ByteRate);
            fs.Write(_byteRate, 0, _byteRate.Length);

            //Block Align
            byte[] _blockAlign = BitConverter.GetBytes(BlockAlign);
            fs.Write(_blockAlign, 0, 2);

            //Bits Per Sample
            byte[] _bitsPerSample = BitConverter.GetBytes(BitsPerSample);
            fs.Write(_bitsPerSample, 0, 2);
        }

        public int Size { get { return Subchunk1Size; } }
    }
}
