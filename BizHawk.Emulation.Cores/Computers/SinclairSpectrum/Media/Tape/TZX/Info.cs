
using System.IO;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// This blocks contains information about the hardware that the programs on this tape use.
    /// </summary>
    public class TzxHwInfo : ITapeDataSerialization
    {
        /// <summary>
        /// Hardware type
        /// </summary>
        public byte HwType { get; set; }

        /// <summary>
        /// Hardwer Id
        /// </summary>
        public byte HwId { get; set; }

        /// <summary>
        /// Information about the tape
        /// </summary>
        /// <remarks>
        /// 00 - The tape RUNS on this machine or with this hardware,
        ///      but may or may not use the hardware or special features of the machine.
        /// 01 - The tape USES the hardware or special features of the machine,
        ///      such as extra memory or a sound chip.
        /// 02 - The tape RUNS but it DOESN'T use the hardware
        ///      or special features of the machine.
        /// 03 - The tape DOESN'T RUN on this machine or with this hardware.
        /// </remarks>
        public byte TapeInfo;

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public void ReadFrom(BinaryReader reader)
        {
            HwType = reader.ReadByte();
            HwId = reader.ReadByte();
            TapeInfo = reader.ReadByte();
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(HwType);
            writer.Write(HwId);
            writer.Write(TapeInfo);
        }
    }

    /// <summary>
    /// Symbol repetitions
    /// </summary>
    public struct TzxPrle
    {
        /// <summary>
        /// Symbol represented
        /// </summary>
        public byte Symbol;

        /// <summary>
        /// Number of repetitions
        /// </summary>
        public ushort Repetitions;
    }

    /// <summary>
    /// This block represents an extremely wide range of data encoding techniques.
    /// </summary>
    /// <remarks>
    /// The basic idea is that each loading component (pilot tone, sync pulses, data) 
    /// is associated to a specific sequence of pulses, where each sequence (wave) can 
    /// contain a different number of pulses from the others. In this way we can have 
    /// a situation where bit 0 is represented with 4 pulses and bit 1 with 8 pulses.
    /// </remarks>
    public class TzxSymDef : ITapeDataSerialization
    {
        /// <summary>
        /// Bit 0 - Bit 1: Starting symbol polarity
        /// </summary>
        /// <remarks>
        /// 00: opposite to the current level (make an edge, as usual) - default
        /// 01: same as the current level(no edge - prolongs the previous pulse)
        /// 10: force low level
        /// 11: force high level
        /// </remarks>
        public byte SymbolFlags;

        /// <summary>
        /// The array of pulse lengths
        /// </summary>
        public ushort[] PulseLengths;

        public TzxSymDef(byte maxPulses)
        {
            PulseLengths = new ushort[maxPulses];
        }

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public void ReadFrom(BinaryReader reader)
        {
            SymbolFlags = reader.ReadByte();
            PulseLengths = TzxDataBlockBase.ReadWords(reader, PulseLengths.Length);
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(SymbolFlags);
            TzxDataBlockBase.WriteWords(writer, PulseLengths);
        }
    }

    /// <summary>
    /// This is meant to identify parts of the tape, so you know where level 1 starts, 
    /// where to rewind to when the game ends, etc.
    /// </summary>
    /// <remarks>
    /// This description is not guaranteed to be shown while the tape is playing, 
    /// but can be read while browsing the tape or changing the tape pointer.
    /// </remarks>
    public class TzxText : ITapeDataSerialization
    {
        /// <summary>
        /// Text identification byte.
        /// </summary>
        /// <remarks>
        /// 00 - Full title
        /// 01 - Software house/publisher
        /// 02 - Author(s)
        /// 03 - Year of publication
        /// 04 - Language
        /// 05 - Game/utility type
        /// 06 - Price
        /// 07 - Protection scheme/loader
        /// 08 - Origin
        /// FF - Comment(s)
        /// </remarks>
        public byte Type { get; set; }

        /// <summary>
        /// Length of the description
        /// </summary>
        public byte Length { get; set; }

        /// <summary>
        /// The description bytes
        /// </summary>
        public byte[] TextBytes;

        /// <summary>
        /// The string form of description
        /// </summary>
        public string Text => TzxDataBlockBase.ToAsciiString(TextBytes);

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public void ReadFrom(BinaryReader reader)
        {
            Type = reader.ReadByte();
            Length = reader.ReadByte();
            TextBytes = reader.ReadBytes(Length);
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(Type);
            writer.Write(Length);
            writer.Write(TextBytes);
        }
    }

    /// <summary>
    /// This block represents select structure
    /// </summary>
    public class TzxSelect : ITapeDataSerialization
    {
        /// <summary>
        /// Bit 0 - Bit 1: Starting symbol polarity
        /// </summary>
        /// <remarks>
        /// 00: opposite to the current level (make an edge, as usual) - default
        /// 01: same as the current level(no edge - prolongs the previous pulse)
        /// 10: force low level
        /// 11: force high level
        /// </remarks>
        public ushort BlockOffset;

        /// <summary>
        /// Length of the description
        /// </summary>
        public byte DescriptionLength { get; set; }

        /// <summary>
        /// The description bytes
        /// </summary>
        public byte[] Description;

        /// <summary>
        /// The string form of description
        /// </summary>
        public string DescriptionText => TzxDataBlockBase.ToAsciiString(Description);

        public TzxSelect(byte length)
        {
            DescriptionLength = length;
        }

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public void ReadFrom(BinaryReader reader)
        {
            BlockOffset = reader.ReadUInt16();
            DescriptionLength = reader.ReadByte();
            Description = reader.ReadBytes(DescriptionLength);
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(BlockOffset);
            writer.Write(DescriptionLength);
            writer.Write(Description);
        }
    }
}
