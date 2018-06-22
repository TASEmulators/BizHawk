using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// From https://archive.codeplex.com/?p=zxmak2
    /// </summary>
    public class WavHeader
    {
        // RIFF chunk (12 bytes)
        public Int32 chunkID;           // "RIFF"
        public Int32 fileSize;
        public Int32 riffType;          // "WAVE"

        // Format chunk (24 bytes)
        public Int32 fmtID;             // "fmt "
        public Int32 fmtSize;
        public Int16 fmtCode;
        public Int16 channels;
        public Int32 sampleRate;
        public Int32 fmtAvgBPS;
        public Int16 fmtBlockAlign;
        public Int16 bitDepth;
        public Int16 fmtExtraSize;

        // Data chunk
        public Int32 dataID;            // "data"
        public Int32 dataSize;          // The data size should be file size - 36 bytes.


        public void Deserialize(Stream stream)
        {
            StreamHelper.Read(stream, out chunkID);
            StreamHelper.Read(stream, out fileSize);
            StreamHelper.Read(stream, out riffType);
            if (chunkID != BitConverter.ToInt32(Encoding.ASCII.GetBytes("RIFF"), 0))
            {
                throw new FormatException("Invalid WAV file header");
            }
            if (riffType != BitConverter.ToInt32(Encoding.ASCII.GetBytes("WAVE"), 0))
            {
                throw new FormatException(string.Format(
                    "Not supported RIFF type: '{0}'",
                    Encoding.ASCII.GetString(BitConverter.GetBytes(riffType))));
            }
            Int32 chunkId;
            Int32 chunkSize;
            while (stream.Position < stream.Length)
            {
                StreamHelper.Read(stream, out chunkId);
                StreamHelper.Read(stream, out chunkSize);
                string strChunkId = Encoding.ASCII.GetString(
                    BitConverter.GetBytes(chunkId));
                if (strChunkId == "fmt ")
                {
                    read_fmt(stream, chunkId, chunkSize);
                }
                else if (strChunkId == "data")
                {
                    read_data(stream, chunkId, chunkSize);
                    break;
                }
                else
                {
                    stream.Seek(chunkSize, SeekOrigin.Current);
                }
            }
            if (fmtID != BitConverter.ToInt32(Encoding.ASCII.GetBytes("fmt "), 0))
            {
                throw new FormatException("WAV format chunk not found");
            }
            if (dataID != BitConverter.ToInt32(Encoding.ASCII.GetBytes("data"), 0))
            {
                throw new FormatException("WAV data chunk not found");
            }
        }

        private void read_data(Stream stream, int chunkId, int chunkSize)
        {
            dataID = chunkId;
            dataSize = chunkSize;
        }

        private void read_fmt(Stream stream, int chunkId, int chunkSize)
        {
            fmtID = chunkId;
            fmtSize = chunkSize;
            StreamHelper.Read(stream, out fmtCode);
            StreamHelper.Read(stream, out channels);
            StreamHelper.Read(stream, out sampleRate);
            StreamHelper.Read(stream, out fmtAvgBPS);
            StreamHelper.Read(stream, out fmtBlockAlign);
            StreamHelper.Read(stream, out bitDepth);
            if (fmtSize == 18)
            {
                // Read any extra values
                StreamHelper.Read(stream, out fmtExtraSize);
                stream.Seek(fmtExtraSize, SeekOrigin.Current);
            }
        }
    }
}
