
using System.IO;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Represents the standard speed data block in a TZX file
    /// </summary>
    public class TzxArchiveInfoDataBlock : Tzx3ByteDataBlockBase
    {
        /// <summary>
        /// Length of the whole block (without these two bytes)
        /// </summary>
        public ushort Length { get; set; }

        /// <summary>
        /// Number of text strings
        /// </summary>
        public byte StringCount { get; set; }

        /// <summary>
        /// List of text strings
        /// </summary>
        public TzxText[] TextStrings { get; set; }

        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x32;

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public override void ReadFrom(BinaryReader reader)
        {
            Length = reader.ReadUInt16();
            StringCount = reader.ReadByte();
            TextStrings = new TzxText[StringCount];
            for (var i = 0; i < StringCount; i++)
            {
                var text = new TzxText();
                text.ReadFrom(reader);
                TextStrings[i] = text;
            }
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public override void WriteTo(BinaryWriter writer)
        {
            writer.Write(Length);
            writer.Write(StringCount);
            foreach (var text in TextStrings)
            {
                text.WriteTo(writer);
            }
        }
    }

    /// <summary>
    /// This block was created to support the Commodore 64 standard 
    /// ROM and similar tape blocks.
    /// </summary>
    public class TzxC64RomTypeDataBlock : TzxDeprecatedDataBlockBase
    {
        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x16;

        /// <summary>
        /// Reads through the block infromation, and does not store it
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public override void ReadThrough(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            reader.ReadBytes(length - 4);
        }
    }

    /// <summary>
    /// This block is made to support another type of encoding that is 
    /// commonly used by the C64.
    /// </summary>
    public class TzxC64TurboTapeDataBlock : TzxDeprecatedDataBlockBase
    {
        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x17;

        /// <summary>
        /// Reads through the block infromation, and does not store it
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public override void ReadThrough(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            reader.ReadBytes(length - 4);
        }
    }

    /// <summary>
    /// This block is an analogue of the CALL Subroutine statement.
    /// </summary>
    /// <remarks>
    /// It basically executes a sequence of blocks that are somewhere 
    /// else and then goes back to the next block. Because more than 
    /// one call can be normally used you can include a list of sequences 
    /// to be called. The 'nesting' of call blocks is also not allowed 
    /// for the simplicity reasons. You can, of course, use the CALL 
    /// blocks in the LOOP sequences and vice versa.
    /// </remarks>
    public class TzxCallSequenceDataBlock : TzxDataBlockBase
    {
        /// <summary>
        /// Number of group name
        /// </summary>
        public byte NumberOfCalls { get; set; }

        /// <summary>
        /// Group name bytes
        /// </summary>
        public ushort[] BlockOffsets { get; set; }

        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x26;

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public override void ReadFrom(BinaryReader reader)
        {
            NumberOfCalls = reader.ReadByte();
            BlockOffsets = ReadWords(reader, NumberOfCalls);
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public override void WriteTo(BinaryWriter writer)
        {
            writer.Write(NumberOfCalls);
            WriteWords(writer, BlockOffsets);
        }
    }

    /// <summary>
    /// Represents the standard speed data block in a TZX file
    /// </summary>
    public class TzxCswRecordingDataBlock : TzxDataBlockBase
    {
        /// <summary>
        /// Block length (without these four bytes)
        /// </summary>
        public uint BlockLength { get; set; }

        /// <summary>
        /// Pause after this block
        /// </summary>
        public ushort PauseAfter { get; set; }

        /// <summary>
        /// Sampling rate
        /// </summary>
        public byte[] SamplingRate { get; set; }

        /// <summary>
        /// Compression type
        /// </summary>
        /// <remarks>
        /// 0x01=RLE, 0x02=Z-RLE
        /// </remarks>
        public byte CompressionType { get; set; }

        /// <summary>
        /// Number of stored pulses (after decompression, for validation purposes)
        /// </summary>
        public uint PulseCount { get; set; }

        /// <summary>
        /// CSW data, encoded according to the CSW file format specification
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x18;

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public override void ReadFrom(BinaryReader reader)
        {
            BlockLength = reader.ReadUInt32();
            PauseAfter = reader.ReadUInt16();
            SamplingRate = reader.ReadBytes(3);
            CompressionType = reader.ReadByte();
            PulseCount = reader.ReadUInt32();
            var length = (int)BlockLength - 4 /* PauseAfter*/ - 3 /* SamplingRate */
                - 1 /* CompressionType */ - 4 /* PulseCount */;
            Data = reader.ReadBytes(length);
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public override void WriteTo(BinaryWriter writer)
        {
            writer.Write(BlockLength);
            writer.Write(PauseAfter);
            writer.Write(SamplingRate);
            writer.Write(CompressionType);
            writer.Write(PulseCount);
            writer.Write(Data);
        }

        /// <summary>
        /// Override this method to check the content of the block
        /// </summary>
        public override bool IsValid => BlockLength == 4 + 3 + 1 + 4 + Data.Length;
    }

    /// <summary>
    /// Represents the standard speed data block in a TZX file
    /// </summary>
    public class TzxCustomInfoDataBlock : Tzx3ByteDataBlockBase
    {
        /// <summary>
        /// Identification string (in ASCII)
        /// </summary>
        public byte[] Id { get; set; }

        /// <summary>
        /// String representation of the ID
        /// </summary>
        public string IdText => ToAsciiString(Id);

        /// <summary>
        /// Length of the custom info
        /// </summary>
        public uint Length { get; set; }

        /// <summary>
        /// Custom information
        /// </summary>
        public byte[] CustomInfo { get; set; }

        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x35;

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public override void ReadFrom(BinaryReader reader)
        {
            Id = reader.ReadBytes(10);
            Length = reader.ReadUInt32();
            CustomInfo = reader.ReadBytes((int)Length);
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public override void WriteTo(BinaryWriter writer)
        {
            writer.Write(Id);
            writer.Write(Length);
            writer.Write(CustomInfo);
        }
    }

    /// <summary>
    /// Represents the standard speed data block in a TZX file
    /// </summary>
    public class TzxDirectRecordingDataBlock : Tzx3ByteDataBlockBase
    {
        /// <summary>
        /// Number of T-states per sample (bit of data)
        /// </summary>
        public ushort TactsPerSample { get; set; }

        /// <summary>
        /// Pause after this block
        /// </summary>
        public ushort PauseAfter { get; set; }

        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x15;

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public override void ReadFrom(BinaryReader reader)
        {
            TactsPerSample = reader.ReadUInt16();
            PauseAfter = reader.ReadUInt16();
            LastByteUsedBits = reader.ReadByte();
            DataLength = reader.ReadBytes(3);
            Data = reader.ReadBytes(GetLength());
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public override void WriteTo(BinaryWriter writer)
        {
            writer.Write(TactsPerSample);
            writer.Write(PauseAfter);
            writer.Write(LastByteUsedBits);
            writer.Write(DataLength);
            writer.Write(Data);
        }
    }

    /// <summary>
    /// This is a special block that would normally be generated only by emulators.
    /// </summary>
    public class TzxEmulationInfoDataBlock : TzxDeprecatedDataBlockBase
    {
        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x34;

        /// <summary>
        /// Reads through the block infromation, and does not store it
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public override void ReadThrough(BinaryReader reader)
        {
            reader.ReadBytes(8);
        }
    }

    /// <summary>
    /// Represents a generalized data block in a TZX file
    /// </summary>
    public class TzxGeneralizedDataBlock : TzxDataBlockBase
    {
        /// <summary>
        /// Block length (without these four bytes)
        /// </summary>
        public uint BlockLength { get; set; }

        /// <summary>
        /// Pause after this block 
        /// </summary>
        public ushort PauseAfter { get; set; }

        /// <summary>
        /// Total number of symbols in pilot/sync block (can be 0)
        /// </summary>
        public uint Totp { get; set; }

        /// <summary>
        /// Maximum number of pulses per pilot/sync symbol
        /// </summary>
        public byte Npp { get; set; }

        /// <summary>
        /// Number of pilot/sync symbols in the alphabet table (0=256)
        /// </summary>
        public byte Asp { get; set; }

        /// <summary>
        /// Total number of symbols in data stream (can be 0)
        /// </summary>
        public uint Totd { get; set; }

        /// <summary>
        /// Maximum number of pulses per data symbol
        /// </summary>
        public byte Npd { get; set; }

        /// <summary>
        /// Number of data symbols in the alphabet table (0=256)
        /// </summary>
        public byte Asd { get; set; }

        /// <summary>
        /// Pilot and sync symbols definition table
        /// </summary>
        /// <remarks>
        /// This field is present only if Totp > 0
        /// </remarks>
        public TzxSymDef[] PilotSymDef { get; set; }

        /// <summary>
        /// Pilot and sync data stream
        /// </summary>
        /// <remarks>
        /// This field is present only if Totd > 0
        /// </remarks>
        public TzxPrle[] PilotStream { get; set; }

        /// <summary>
        /// Data symbols definition table
        /// </summary>
        /// <remarks>
        /// This field is present only if Totp > 0
        /// </remarks>
        public TzxSymDef[] DataSymDef { get; set; }

        /// <summary>
        /// Data stream
        /// </summary>
        /// <remarks>
        /// This field is present only if Totd > 0
        /// </remarks>
        public TzxPrle[] DataStream { get; set; }

        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x19;

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public override void ReadFrom(BinaryReader reader)
        {
            BlockLength = reader.ReadUInt32();
            PauseAfter = reader.ReadUInt16();
            Totp = reader.ReadUInt32();
            Npp = reader.ReadByte();
            Asp = reader.ReadByte();
            Totd = reader.ReadUInt32();
            Npd = reader.ReadByte();
            Asd = reader.ReadByte();

            PilotSymDef = new TzxSymDef[Asp];
            for (var i = 0; i < Asp; i++)
            {
                var symDef = new TzxSymDef(Npp);
                symDef.ReadFrom(reader);
                PilotSymDef[i] = symDef;
            }

            PilotStream = new TzxPrle[Totp];
            for (var i = 0; i < Totp; i++)
            {
                PilotStream[i].Symbol = reader.ReadByte();
                PilotStream[i].Repetitions = reader.ReadUInt16();
            }

            DataSymDef = new TzxSymDef[Asd];
            for (var i = 0; i < Asd; i++)
            {
                var symDef = new TzxSymDef(Npd);
                symDef.ReadFrom(reader);
                DataSymDef[i] = symDef;
            }

            DataStream = new TzxPrle[Totd];
            for (var i = 0; i < Totd; i++)
            {
                DataStream[i].Symbol = reader.ReadByte();
                DataStream[i].Repetitions = reader.ReadUInt16();
            }
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public override void WriteTo(BinaryWriter writer)
        {
            writer.Write(BlockLength);
            writer.Write(PauseAfter);
            writer.Write(Totp);
            writer.Write(Npp);
            writer.Write(Asp);
            writer.Write(Totd);
            writer.Write(Npd);
            writer.Write(Asd);
            for (var i = 0; i < Asp; i++)
            {
                PilotSymDef[i].WriteTo(writer);
            }
            for (var i = 0; i < Totp; i++)
            {
                writer.Write(PilotStream[i].Symbol);
                writer.Write(PilotStream[i].Repetitions);
            }

            for (var i = 0; i < Asd; i++)
            {
                DataSymDef[i].WriteTo(writer);
            }

            for (var i = 0; i < Totd; i++)
            {
                writer.Write(DataStream[i].Symbol);
                writer.Write(DataStream[i].Repetitions);
            }
        }
    }

    /// <summary>
    /// This block is generated when you merge two ZX Tape files together.
    /// </summary>
    /// <remarks>
    /// It is here so that you can easily copy the files together and use 
    /// them. Of course, this means that resulting file would be 10 bytes 
    /// longer than if this block was not used. All you have to do if 
    /// you encounter this block ID is to skip next 9 bytes.
    /// </remarks>
    public class TzxGlueDataBlock : TzxDataBlockBase
    {
        /// <summary>
        /// Value: { "XTape!", 0x1A, MajorVersion, MinorVersion }
        /// </summary>
        /// <remarks>
        /// Just skip these 9 bytes and you will end up on the next ID.
        /// </remarks>
        public byte[] Glue { get; set; }

        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x5A;

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public override void ReadFrom(BinaryReader reader)
        {
            Glue = reader.ReadBytes(9);
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public override void WriteTo(BinaryWriter writer)
        {
            writer.Write(Glue);
        }
    }

    /// <summary>
    /// This indicates the end of a group. This block has no body.
    /// </summary>
    public class TzxGroupEndDataBlock : TzxBodylessDataBlockBase
    {
        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x22;
    }

    /// <summary>
    /// This block marks the start of a group of blocks which are 
    /// to be treated as one single (composite) block.
    /// </summary>
    public class TzxGroupStartDataBlock : TzxDataBlockBase
    {
        /// <summary>
        /// Number of group name
        /// </summary>
        public byte Length { get; set; }

        /// <summary>
        /// Group name bytes
        /// </summary>
        public byte[] Chars { get; set; }

        /// <summary>
        /// Gets the group name
        /// </summary>
        public string GroupName => ToAsciiString(Chars);

        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x21;

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public override void ReadFrom(BinaryReader reader)
        {
            Length = reader.ReadByte();
            Chars = reader.ReadBytes(Length);
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public override void WriteTo(BinaryWriter writer)
        {
            writer.Write(Length);
            writer.Write(Chars);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class TzxHardwareInfoDataBlock : TzxDataBlockBase
    {
        /// <summary>
        /// Number of machines and hardware types for which info is supplied
        /// </summary>
        public byte HwCount { get; set; }

        /// <summary>
        /// List of machines and hardware
        /// </summary>
        public TzxHwInfo[] HwInfo { get; set; }

        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x33;

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public override void ReadFrom(BinaryReader reader)
        {
            HwCount = reader.ReadByte();
            HwInfo = new TzxHwInfo[HwCount];
            for (var i = 0; i < HwCount; i++)
            {
                var hw = new TzxHwInfo();
                hw.ReadFrom(reader);
                HwInfo[i] = hw;
            }
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public override void WriteTo(BinaryWriter writer)
        {
            writer.Write(HwCount);
            foreach (var hw in HwInfo)
            {
                hw.WriteTo(writer);
            }
        }
    }

    /// <summary>
    /// This block will enable you to jump from one block to another within the file.
    /// </summary>
    /// <remarks>
    /// Jump 0 = 'Loop Forever' - this should never happen
    /// Jump 1 = 'Go to the next block' - it is like NOP in assembler
    /// Jump 2 = 'Skip one block'
    /// Jump -1 = 'Go to the previous block'
    /// </remarks>
    public class TzxJumpDataBlock : TzxDataBlockBase
    {
        /// <summary>
        /// Relative jump value
        /// </summary>
        /// <remarks>
        /// </remarks>
        public short Jump { get; set; }

        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x23;

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public override void ReadFrom(BinaryReader reader)
        {
            Jump = reader.ReadInt16();
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public override void WriteTo(BinaryWriter writer)
        {
            writer.Write(Jump);
        }
    }

    /// <summary>
    /// It means that the utility should jump back to the start 
    /// of the loop if it hasn't been run for the specified number 
    /// of times.
    /// </summary>
    public class TzxLoopEndDataBlock : TzxBodylessDataBlockBase
    {
        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x25;
    }

    /// <summary>
    /// If you have a sequence of identical blocks, or of identical 
    /// groups of blocks, you can use this block to tell how many 
    /// times they should be repeated.
    /// </summary>
    public class TzxLoopStartDataBlock : TzxDataBlockBase
    {
        /// <summary>
        /// Number of repetitions (greater than 1)
        /// </summary>
        public ushort Loops { get; set; }

        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x24;

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public override void ReadFrom(BinaryReader reader)
        {
            Loops = reader.ReadUInt16();
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public override void WriteTo(BinaryWriter writer)
        {
            writer.Write(Loops);
        }
    }

    /// <summary>
    /// This will enable the emulators to display a message for a given time.
    /// </summary>
    /// <remarks>
    /// This should not stop the tape and it should not make silence. If the 
    /// time is 0 then the emulator should wait for the user to press a key.
    /// </remarks>
    public class TzxMessageDataBlock : TzxDataBlockBase
    {
        /// <summary>
        /// Time (in seconds) for which the message should be displayed
        /// </summary>
        public byte Time { get; set; }

        /// <summary>
        /// Length of the description
        /// </summary>
        public byte MessageLength { get; set; }

        /// <summary>
        /// The description bytes
        /// </summary>
        public byte[] Message;

        /// <summary>
        /// The string form of description
        /// </summary>
        public string MessageText => ToAsciiString(Message);

        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x31;

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public override void ReadFrom(BinaryReader reader)
        {
            Time = reader.ReadByte();
            MessageLength = reader.ReadByte();
            Message = reader.ReadBytes(MessageLength);
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public override void WriteTo(BinaryWriter writer)
        {
            writer.Write(Time);
            writer.Write(MessageLength);
            writer.Write(Message);
        }
    }

    /// <summary>
    /// Represents the standard speed data block in a TZX file
    /// </summary>
    public class TzxPulseSequenceDataBlock : TzxDataBlockBase
    {
        /// <summary>
        /// Pause after this block
        /// </summary>
        public byte PulseCount { get; set; }

        /// <summary>
        /// Lenght of block data
        /// </summary>
        public ushort[] PulseLengths { get; set; }

        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x13;

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public override void ReadFrom(BinaryReader reader)
        {
            PulseCount = reader.ReadByte();
            PulseLengths = ReadWords(reader, PulseCount);
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public override void WriteTo(BinaryWriter writer)
        {
            writer.Write(PulseCount);
            WriteWords(writer, PulseLengths);
        }

        /// <summary>
        /// Override this method to check the content of the block
        /// </summary>
        public override bool IsValid => PulseCount == PulseLengths.Length;
    }

    /// <summary>
    /// Represents the standard speed data block in a TZX file
    /// </summary>
    public class TzxPureDataBlock : Tzx3ByteDataBlockBase
    {
        /// <summary>
        /// Length of the zero bit
        /// </summary>
        public ushort ZeroBitPulseLength { get; set; }

        /// <summary>
        /// Length of the one bit
        /// </summary>
        public ushort OneBitPulseLength { get; set; }

        /// <summary>
        /// Pause after this block
        /// </summary>
        public ushort PauseAfter { get; set; }

        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x14;

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public override void ReadFrom(BinaryReader reader)
        {
            ZeroBitPulseLength = reader.ReadUInt16();
            OneBitPulseLength = reader.ReadUInt16();
            LastByteUsedBits = reader.ReadByte();
            PauseAfter = reader.ReadUInt16();
            DataLength = reader.ReadBytes(3);
            Data = reader.ReadBytes(GetLength());
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public override void WriteTo(BinaryWriter writer)
        {
            writer.Write(ZeroBitPulseLength);
            writer.Write(OneBitPulseLength);
            writer.Write(LastByteUsedBits);
            writer.Write(PauseAfter);
            writer.Write(DataLength);
            writer.Write(Data);
        }
    }

    /// <summary>
    /// Represents the standard speed data block in a TZX file
    /// </summary>
    public class TzxPureToneDataBlock : TzxDataBlockBase
    {
        /// <summary>
        /// Pause after this block
        /// </summary>
        public ushort PulseLength { get; private set; }

        /// <summary>
        /// Lenght of block data
        /// </summary>
        public ushort PulseCount { get; private set; }

        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x12;

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public override void ReadFrom(BinaryReader reader)
        {
            PulseLength = reader.ReadUInt16();
            PulseCount = reader.ReadUInt16();
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public override void WriteTo(BinaryWriter writer)
        {
            writer.Write(PulseLength);
            writer.Write(PulseCount);
        }
    }

    /// <summary>
    /// This block indicates the end of the Called Sequence.
    /// The next block played will be the block after the last 
    /// CALL block
    /// </summary>
    public class TzxReturnFromSequenceDataBlock : TzxBodylessDataBlockBase
    {
        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x27;
    }

    /// <summary>
    /// Pause (silence) or 'Stop the Tape' block
    /// </summary>
    public class TzxSelectDataBlock : TzxDataBlockBase
    {
        /// <summary>
        /// Length of the whole block (without these two bytes)
        /// </summary>
        public ushort Length { get; set; }

        /// <summary>
        /// Number of selections
        /// </summary>
        public byte SelectionCount { get; set; }

        /// <summary>
        /// List of selections
        /// </summary>
        public TzxSelect[] Selections { get; set; }

        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x28;

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public override void ReadFrom(BinaryReader reader)
        {
            Length = reader.ReadUInt16();
            SelectionCount = reader.ReadByte();
            Selections = new TzxSelect[SelectionCount];
            foreach (var selection in Selections)
            {
                selection.ReadFrom(reader);
            }
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public override void WriteTo(BinaryWriter writer)
        {
            writer.Write(Length);
            writer.Write(SelectionCount);
            foreach (var selection in Selections)
            {
                selection.WriteTo(writer);
            }
        }        
    }

    /// <summary>
    /// This block sets the current signal level to the specified value (high or low).
    /// </summary>
    public class TzxSetSignalLevelDataBlock : TzxDataBlockBase
    {
        /// <summary>
        /// Length of the block without these four bytes
        /// </summary>
        public uint Lenght { get; } = 1;

        /// <summary>
        /// Signal level (0=low, 1=high)
        /// </summary>
        public byte SignalLevel { get; set; }

        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x2B;

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public override void ReadFrom(BinaryReader reader)
        {
            reader.ReadUInt32();
            SignalLevel = reader.ReadByte();
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public override void WriteTo(BinaryWriter writer)
        {
            writer.Write(Lenght);
            writer.Write(SignalLevel);
        }
    }

    /// <summary>
    /// Pause (silence) or 'Stop the Tape' block
    /// </summary>
    public class TzxSilenceDataBlock : TzxDataBlockBase
    {
        /// <summary>
        /// Duration of silence
        /// </summary>
        /// <remarks>
        /// This will make a silence (low amplitude level (0)) for a given time 
        /// in milliseconds. If the value is 0 then the emulator or utility should 
        /// (in effect) STOP THE TAPE, i.e. should not continue loading until 
        /// the user or emulator requests it.
        /// </remarks>
        public ushort Duration { get; set; }

        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x20;

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public override void ReadFrom(BinaryReader reader)
        {
            Duration = reader.ReadUInt16();
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public override void WriteTo(BinaryWriter writer)
        {
            writer.Write(Duration);
        }
    }

    /// <summary>
    /// This block was created to support the Commodore 64 standard 
    /// ROM and similar tape blocks.
    /// </summary>
    public class TzxSnapshotBlock : TzxDeprecatedDataBlockBase
    {
        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x40;

        /// <summary>
        /// Reads through the block infromation, and does not store it
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public override void ReadThrough(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            length = length & 0x00FFFFFF;
            reader.ReadBytes(length);
        }
    }

    /// <summary>
    /// Represents the standard speed data block in a TZX file
    /// </summary>
    public class TzxStandardSpeedDataBlock : TzxDataBlockBase, ISupportsTapeBlockPlayback, ITapeData
    {
        private TapeDataBlockPlayer _player;

        /// <summary>
        /// Pause after this block (default: 1000ms)
        /// </summary>
        public ushort PauseAfter { get; set; } = 1000;

        /// <summary>
        /// Lenght of block data
        /// </summary>
        public ushort DataLength { get; set; }

        /// <summary>
        /// Block Data
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x10;

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public override void ReadFrom(BinaryReader reader)
        {
            PauseAfter = reader.ReadUInt16();
            DataLength = reader.ReadUInt16();
            Data = reader.ReadBytes(DataLength);
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public override void WriteTo(BinaryWriter writer)
        {
            writer.Write((byte)BlockId);
            writer.Write(PauseAfter);
            writer.Write(DataLength);
            writer.Write(Data, 0, DataLength);
        }

        /// <summary>
        /// The index of the currently playing byte
        /// </summary>
        /// <remarks>This proprty is made public for test purposes</remarks>
        public int ByteIndex => _player.ByteIndex;

        /// <summary>
        /// The mask of the currently playing bit in the current byte
        /// </summary>
        public byte BitMask => _player.BitMask;

        /// <summary>
        /// The current playing phase
        /// </summary>
        public PlayPhase PlayPhase => _player.PlayPhase;

        /// <summary>
        /// The tact count of the CPU when playing starts
        /// </summary>
        public long StartCycle=> _player.StartCycle;

        /// <summary>
        /// Last tact queried
        /// </summary>
        public long LastTact => _player.LastCycle;

        /// <summary>
        /// Initializes the player
        /// </summary>
        public void InitPlay(long startCycle)
        {
            _player = new TapeDataBlockPlayer(Data, PauseAfter);
            _player.InitPlay(startCycle);
        }

        /// <summary>
        /// Gets the EAR bit value for the specified tact
        /// </summary>
        /// <param name="currentTact">Tacts to retrieve the EAR bit</param>
        /// <returns>
        /// The EAR bit value to play back
        /// </returns>
        public bool GetEarBit(long currentCycle) => _player.GetEarBit(currentCycle);
    }

    /// <summary>
    /// When this block is encountered, the tape will stop ONLY if 
    /// the machine is an 48K Spectrum.
    /// </summary>
    /// <remarks>
    /// This block is to be used for multiloading games that load one 
    /// level at a time in 48K mode, but load the entire tape at once 
    /// if in 128K mode. This block has no body of its own, but follows 
    /// the extension rule.
    /// </remarks>
    public class TzxStopTheTape48DataBlock : TzxDataBlockBase
    {
        /// <summary>
        /// Length of the block without these four bytes (0)
        /// </summary>
        public uint Lenght { get; } = 0;

        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x2A;

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public override void ReadFrom(BinaryReader reader)
        {
            reader.ReadUInt32();
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public override void WriteTo(BinaryWriter writer)
        {
            writer.Write(Lenght);
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
    public class TzxTextDescriptionDataBlock : TzxDataBlockBase
    {
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
        public string DescriptionText => ToAsciiString(Description);

        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x30;

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public override void ReadFrom(BinaryReader reader)
        {
            DescriptionLength = reader.ReadByte();
            Description = reader.ReadBytes(DescriptionLength);
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public override void WriteTo(BinaryWriter writer)
        {
            writer.Write(DescriptionLength);
            writer.Write(Description);
        }
    }

    /// <summary>
    /// Represents the standard speed data block in a TZX file
    /// </summary>
    public class TzxTurboSpeedDataBlock : Tzx3ByteDataBlockBase
    {
        /// <summary>
        /// Length of pilot pulse
        /// </summary>
        public ushort PilotPulseLength { get; set; }

        /// <summary>
        /// Length of the first sync pulse
        /// </summary>
        public ushort Sync1PulseLength { get; set; }

        /// <summary>
        /// Length of the second sync pulse
        /// </summary>
        public ushort Sync2PulseLength { get; set; }

        /// <summary>
        /// Length of the zero bit
        /// </summary>
        public ushort ZeroBitPulseLength { get; set; }

        /// <summary>
        /// Length of the one bit
        /// </summary>
        public ushort OneBitPulseLength { get; set; }

        /// <summary>
        /// Length of the pilot tone
        /// </summary>
        public ushort PilotToneLength { get; set; }

        /// <summary>
        /// Pause after this block
        /// </summary>
        public ushort PauseAfter { get; set; }

        public TzxTurboSpeedDataBlock()
        {
            PilotPulseLength = 2168;
            Sync1PulseLength = 667;
            Sync2PulseLength = 735;
            ZeroBitPulseLength = 855;
            OneBitPulseLength = 1710;
            PilotToneLength = 8063;
            LastByteUsedBits = 8;
        }

        /// <summary>
        /// The ID of the block
        /// </summary>
        public override int BlockId => 0x11;

        /// <summary>
        /// Reads the content of the block from the specified binary stream.
        /// </summary>
        /// <param name="reader">Stream to read the block from</param>
        public override void ReadFrom(BinaryReader reader)
        {
            PilotPulseLength = reader.ReadUInt16();
            Sync1PulseLength = reader.ReadUInt16();
            Sync2PulseLength = reader.ReadUInt16();
            ZeroBitPulseLength = reader.ReadUInt16();
            OneBitPulseLength = reader.ReadUInt16();
            PilotToneLength = reader.ReadUInt16();
            LastByteUsedBits = reader.ReadByte();
            PauseAfter = reader.ReadUInt16();
            DataLength = reader.ReadBytes(3);
            Data = reader.ReadBytes(GetLength());
        }

        /// <summary>
        /// Writes the content of the block to the specified binary stream.
        /// </summary>
        /// <param name="writer">Stream to write the block to</param>
        public override void WriteTo(BinaryWriter writer)
        {
            writer.Write(PilotPulseLength);
            writer.Write(Sync1PulseLength);
            writer.Write(Sync2PulseLength);
            writer.Write(ZeroBitPulseLength);
            writer.Write(OneBitPulseLength);
            writer.Write(PilotToneLength);
            writer.Write(LastByteUsedBits);
            writer.Write(PauseAfter);
            writer.Write(DataLength);
            writer.Write(Data);
        }
    }
}
