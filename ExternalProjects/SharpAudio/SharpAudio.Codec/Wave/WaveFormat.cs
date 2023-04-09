using System;
using System.IO;

namespace SharpAudio.Codec.Wave
{
    enum WaveFormatType : UInt16
    {
        Pcm = 0x01,
        DviAdpcm = 0x11,
    }

    struct RiffHeader
    {
        public string ChunkId;
        public UInt32 ChunkSize;
        public string Format;

        public static RiffHeader Parse(BinaryReader reader)
        {
            var header = new RiffHeader();
            header.ChunkId = reader.ReadFourCc();
            header.ChunkSize = reader.ReadUInt32();
            header.Format = reader.ReadFourCc();

            if (header.ChunkId != "RIFF" ||
                header.Format != "WAVE")
            {
                throw new InvalidDataException("Invalid or missing .wav file header!");
            }

            return header;
        }
    }

    struct WaveFormat
    {
        public string SubChunkID;
        public UInt32 SubChunkSize;
        public WaveFormatType AudioFormat;
        public UInt16 NumChannels;
        public UInt32 SampleRate;
        public UInt32 ByteRate;
        public UInt16 BlockAlign;
        public UInt16 BitsPerSample;
        public UInt16 ExtraBytesSize; // Only used in certain compressed formats
        public byte[] ExtraBytes; // Only used in certain compressed formats

        public static WaveFormat Parse(BinaryReader reader)
        {
            var format = new WaveFormat();
            format.SubChunkID = reader.ReadFourCc();
            if (format.SubChunkID != "fmt ")
            {
                throw new InvalidDataException("Invalid or missing .wav file format chunk!");
            }
            format.SubChunkSize = reader.ReadUInt32();
            format.AudioFormat = (WaveFormatType) reader.ReadUInt16();
            format.NumChannels = reader.ReadUInt16();
            format.SampleRate = reader.ReadUInt32();
            format.ByteRate = reader.ReadUInt32();
            format.BlockAlign = reader.ReadUInt16();
            format.BitsPerSample = reader.ReadUInt16();

            if (format.SubChunkSize == 18)
            {
                reader.ReadInt16();
            }

            switch (format.AudioFormat)
            {
                case WaveFormatType.Pcm:
                    format.ExtraBytesSize = 0;
                    format.ExtraBytes = new byte[0];
                    break;
                case WaveFormatType.DviAdpcm:
                    if (format.NumChannels != 1)
                    {
                        throw new NotSupportedException("Only single channel DVI ADPCM compressed .wavs are supported.");
                    }
                    format.ExtraBytesSize = reader.ReadUInt16();
                    if (format.ExtraBytesSize != 2)
                    {
                        throw new InvalidDataException("Invalid .wav DVI ADPCM format!");
                    }
                    format.ExtraBytes = reader.ReadBytes(format.ExtraBytesSize);
                    break;
                default:
                    throw new NotSupportedException("Invalid or unknown .wav compression format!");
            }
            return format;
        }
    };

    struct WaveFact
    {
        public string SubChunkID;
        public UInt32 SubChunkSize;
        // Technically this chunk could contain arbitrary data. But in practice
        // it only ever contains a single UInt32 representing the number of
        // samples.
        public UInt32 NumSamples;

        public static WaveFact Parse(BinaryReader reader)
        {
            var fact = new WaveFact();
            fact.SubChunkID = reader.ReadFourCc();
            if (fact.SubChunkID != "fact")
            {
                throw new InvalidDataException("Invalid or missing .wav file fact chunk!");
            }
            fact.SubChunkSize = reader.ReadUInt32();
            if (fact.SubChunkSize != 4)
            {
                throw new NotSupportedException("Invalid or unknown .wav compression format!");
            }
            fact.NumSamples = reader.ReadUInt32();
            return fact;
        }
    }

    struct WaveData
    {
        public string SubChunkID; // should contain the word data
        public UInt32 SubChunkSize; // Stores the size of the data block

        public static WaveData Parse(BinaryReader reader)
        {
            var data = new WaveData();
            data.SubChunkID = reader.ReadFourCc();
            if (data.SubChunkID != "data")
            {
                throw new InvalidDataException("Invalid or missing .wav file data chunk!");
            }
            data.SubChunkSize = reader.ReadUInt32();
            return data;
        }
    };
}
