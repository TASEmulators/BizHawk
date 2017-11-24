
using System;
using System.IO;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// This class describes a TZX Block
    /// </summary>
    public abstract class TzxDataBlockBase : ITapeDataSerialization
    {
        /// <summary>
        /// The ID of the block
        /// </summary>
        public abstract int BlockId { get; }

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public abstract void ReadFrom(BinaryReader reader);

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public abstract void WriteTo(BinaryWriter writer);

        /// <summary>
        /// Override this method to check the content of the block
        /// </summary>
        public virtual bool IsValid => true;

        /// <summary>
        /// Reads the specified number of words from the reader.
        /// </summary>
        /// <param name="reader">Reader to obtain the input from</param>
        /// <param name="count">Number of words to get</param>
        /// <returns>Word array read from the input</returns>
        public static ushort[] ReadWords(BinaryReader reader, int count)
        {
            var result = new ushort[count];
            var bytes = reader.ReadBytes(2 * count);
            for (var i = 0; i < count; i++)
            {
                result[i] = (ushort)(bytes[i * 2] + bytes[i * 2 + 1] << 8);
            }
            return result;
        }

        /// <summary>
        /// Writes the specified array of words to the writer
        /// </summary>
        /// <param name="writer">Output</param>
        /// <param name="words">Word array</param>
        public static void WriteWords(BinaryWriter writer, ushort[] words)
        {
            foreach (var word in words)
            {
                writer.Write(word);
            }
        }

        /// <summary>
        /// Converts the provided bytes to an ASCII string
        /// </summary>
        /// <param name="bytes">Bytes to convert</param>
        /// <param name="offset">First byte offset</param>
        /// <param name="count">Number of bytes</param>
        /// <returns>ASCII string representation</returns>
        public static string ToAsciiString(byte[] bytes, int offset = 0, int count = -1)
        {
            if (count < 0) count = bytes.Length - offset;
            var sb = new StringBuilder();
            for (var i = offset; i < count; i++)
            {
                sb.Append(Convert.ToChar(bytes[i]));
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Base class for all TZX block type with data length of 3 bytes
    /// </summary>
    public abstract class Tzx3ByteDataBlockBase : TzxDataBlockBase
    {
        /// <summary>
        /// Used bits in the last byte (other bits should be 0)
        /// </summary>
        /// <remarks>
        /// (e.g. if this is 6, then the bits used(x) in the last byte are: 
        /// xxxxxx00, where MSb is the leftmost bit, LSb is the rightmost bit)
        /// </remarks>
        public byte LastByteUsedBits { get; set; }

        /// <summary>
        /// Lenght of block data
        /// </summary>
        public byte[] DataLength { get; set; }

        /// <summary>
        /// Block Data
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Override this method to check the content of the block
        /// </summary>
        public override bool IsValid => GetLength() == Data.Length;

        /// <summary>
        /// Calculates data length
        /// </summary>
        protected int GetLength()
        {
            return DataLength[0] + DataLength[1] << 8 + DataLength[2] << 16;
        }
    }

    /// <summary>
    /// This class represents a TZX data block with empty body
    /// </summary>
    public abstract class TzxBodylessDataBlockBase : TzxDataBlockBase
    {
        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public override void ReadFrom(BinaryReader reader)
        {
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public override void WriteTo(BinaryWriter writer)
        {
        }
    }

    /// <summary>
    /// This class represents a deprecated block
    /// </summary>
    public abstract class TzxDeprecatedDataBlockBase : TzxDataBlockBase
    {
        /// <summary>
        /// Reads through the block infromation, and does not store it
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public abstract void ReadThrough(BinaryReader reader);

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public override void ReadFrom(BinaryReader reader)
        {
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public override void WriteTo(BinaryWriter writer)
        {
            throw new InvalidOperationException("Deprecated TZX data blocks cannot be written.");
        }
    }
}
