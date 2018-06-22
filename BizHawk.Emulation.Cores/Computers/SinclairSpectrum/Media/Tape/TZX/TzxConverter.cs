using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Reponsible for TZX format serializaton
    /// </summary>
    public class TzxConverter : MediaConverter
    {
        /// <summary>
        /// The type of serializer
        /// </summary>
        private MediaConverterType _formatType = MediaConverterType.TZX;
        public override MediaConverterType FormatType
        {
            get
            {
                return _formatType;
            }
        }

        /// <summary>
        /// Signs whether this class can be used to read the data format
        /// </summary>
        public override bool IsReader { get { return true; } }

        /// <summary>
        /// Signs whether this class can be used to write the data format
        /// </summary>
        public override bool IsWriter { get { return false; } }

        /// <summary>
        /// Working list of generated tape data blocks
        /// </summary>
        private List<TapeDataBlock> _blocks = new List<TapeDataBlock>();

        /// <summary>
        /// Position counter
        /// </summary>
        private int _position = 0;

        /// <summary>
        /// Object to keep track of loops - this assumes there is only one loop at a time
        /// </summary>
        private List<KeyValuePair<int, int>> _loopCounter = new List<KeyValuePair<int, int>>();

        #region Construction

        private DatacorderDevice _datacorder;

        public TzxConverter(DatacorderDevice _tapeDevice)
        {
            _datacorder = _tapeDevice;
        }

        #endregion

        /// <summary>
        /// Returns TRUE if tzx header is detected
        /// </summary>
        /// <param name="data"></param>
        public override bool CheckType(byte[] data)
        {
            /*
            // TZX Header
            length: 10 bytes
            Offset  Value       Type        Description
            0x00    "ZXTape!"   ASCII[7]    TZX signature
            0x07    0x1A        BYTE        End of text file marker
            0x08    1           BYTE        TZX major revision number
            0x09    20          BYTE        TZX minor revision number
            */

            // check whether this is a valid tzx format file by looking at the identifier in the header
            // (first 7 bytes of the file)
            string ident = Encoding.ASCII.GetString(data, 0, 7);
            // and 'end of text' marker
            byte eotm = data[7];

            // version info
            int majorVer = data[8];
            int minorVer = data[9];

            if (ident != "ZXTape!" || eotm != 0x1A)
            {
                // this is not a valid TZX format file
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// DeSerialization method
        /// </summary>
        /// <param name="data"></param>
        public override void Read(byte[] data)
        {
            // clear existing tape blocks
            _datacorder.DataBlocks.Clear();

/*
            // TZX Header
            length: 10 bytes
            Offset  Value       Type        Description
            0x00    "ZXTape!"   ASCII[7]    TZX signature
            0x07    0x1A        BYTE        End of text file marker
            0x08    1           BYTE        TZX major revision number
            0x09    20          BYTE        TZX minor revision number
*/

            // check whether this is a valid tzx format file by looking at the identifier in the header
            // (first 7 bytes of the file)
            string ident = Encoding.ASCII.GetString(data, 0, 7);
            // and 'end of text' marker
            byte eotm = data[7];

            // version info
            int majorVer = data[8];
            int minorVer = data[9];

            if (ident != "ZXTape!" || eotm != 0x1A)
            {
                // this is not a valid TZX format file
                throw new Exception(this.GetType().ToString() +
                    "This is not a valid TZX format file");
            }

            // iterate through each block
            _position = 10;
            while (_position < data.Length)
            {
                // block ID is the first byte in a new block                
                int ID = data[_position++];

                // process the data
                ProcessBlock(data, ID);
            }

        }

        /// <summary>
        /// Processes a TZX block
        /// </summary>
        /// <param name="data"></param>
        /// <param name="id"></param>
        private void ProcessBlock(byte[] data, int id)
        {
            // process based on detected block ID
            switch (id)
            {
                // ID 10 - Standard Speed Data Block
                case 0x10:
                    ProcessBlockID10(data);
                    break;
                // ID 11 - Turbo Speed Data Block
                case 0x11:
                    ProcessBlockID11(data);
                    break;
                // ID 12 - Pure Tone
                case 0x12:
                    ProcessBlockID12(data);
                    break;
                // ID 13 - Pulse sequence
                case 0x13:
                    ProcessBlockID13(data);
                    break;
                // ID 14 - Pure Data Block
                case 0x14:
                    ProcessBlockID14(data);
                    break;
                // ID 15 - Direct Recording
                case 0x15:
                    ProcessBlockID15(data);
                    break;
                // ID 18 - CSW Recording
                case 0x18:
                    ProcessBlockID18(data);
                    break;
                // ID 19 - Generalized Data Block
                case 0x19:
                    ProcessBlockID19(data);
                    break;
                // ID 20 - Pause (silence) or 'Stop the Tape' command
                case 0x20:
                    ProcessBlockID20(data);
                    break;
                // ID 21 - Group start
                case 0x21:
                    ProcessBlockID21(data);
                    break;
                // ID 22 - Group end
                case 0x22:
                    ProcessBlockID22(data);
                    break;
                // ID 23 - Jump to block
                case 0x23:
                    ProcessBlockID23(data);
                    break;
                // ID 24 - Loop start
                case 0x24:
                    ProcessBlockID24(data);
                    break;
                // ID 25 - Loop end
                case 0x25:
                    ProcessBlockID25(data);
                    break;
                // ID 26 - Call sequence
                case 0x26:
                    ProcessBlockID26(data);
                    break;
                // ID 27 - Return from sequence
                case 0x27:
                    ProcessBlockID27(data);
                    break;
                // ID 28 - Select block
                case 0x28:
                    ProcessBlockID28(data);
                    break;
                // ID 2A - Stop the tape if in 48K mode
                case 0x2A:
                    ProcessBlockID2A(data);
                    break;
                // ID 2B - Set signal level
                case 0x2B:
                    ProcessBlockID2B(data);
                    break;
                // ID 30 - Text description
                case 0x30:
                    ProcessBlockID30(data);
                    break;
                // ID 31 - Message block
                case 0x31:
                    ProcessBlockID31(data);
                    break;
                // ID 32 - Archive info
                case 0x32:
                    ProcessBlockID32(data);
                    break;
                // ID 33 - Hardware type
                case 0x33:
                    ProcessBlockID33(data);
                    break;
                // ID 35 - Custom info block
                case 0x35:
                    ProcessBlockID35(data);
                    break;
                // ID 5A - "Glue" block
                case 0x5A:
                    ProcessBlockID5A(data);
                    break;

                #region Depreciated Blocks

                // ID 16 - C64 ROM Type Data Block
                case 0x16:
                    ProcessBlockID16(data);
                    break;
                // ID 17 - C64 Turbo Tape Data Block
                case 0x17:
                    ProcessBlockID17(data);
                    break;
                // ID 34 - Emulation info
                case 0x34:
                    ProcessBlockID34(data);
                    break;
                // ID 40 - Snapshot block
                case 0x40:
                    ProcessBlockID40(data);
                    break;

                #endregion

                default:
                    ProcessUnidentifiedBlock(data);
                    break;
            }
        }

        #region TZX Block Processors

        #region ID 10 - Standard Speed Data Block
/*              length: [02,03]+04
        Offset	    Value	Type	    Description
        0x00	    -	    WORD	    Pause after this block (ms.) {1000}
        0x02	    N	    WORD	    Length of data that follow
        0x04	    -	    BYTE[N]	    Data as in .TAP files                   

        This block must be replayed with the standard Spectrum ROM timing values - see the values in 
        curly brackets in block ID 11. The pilot tone consists in 8063 pulses if the first data byte 
        (flag byte) is < 128, 3223 otherwise. This block can be used for the ROM loading routines AND 
        for custom loading routines that use the same timings as ROM ones do. */
        private void ProcessBlockID10(byte[] data)
        {
            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x10;
            t.BlockDescription = BlockType.Standard_Speed_Data_Block;
            t.DataPeriods = new List<int>();

            int pauseLen = GetWordValue(data, _position);
            if (pauseLen == 0)
                pauseLen = 1000;

            t.PauseInMS = pauseLen;

            int blockLen = GetWordValue(data, _position + 2);

            _position += 4;

            byte[] tmp = new byte[blockLen];
            tmp = data.Skip(_position).Take(blockLen).ToArray();

            var t2 = DecodeDataBlock(t, tmp, DataBlockType.Standard, pauseLen);          

            // add the block
            _datacorder.DataBlocks.Add(t2);

            // advance the position to the next block
            _position += blockLen;

            // generate PAUSE block
            CreatePauseBlock(_datacorder.DataBlocks.Last());
        }
        #endregion

        #region ID 11 - Turbo Speed Data Block
/*              length: [0F,10,11]+12
        Offset	Value	Type	Description
        0x00	-	    WORD	Length of PILOT pulse {2168}
        0x02	-	    WORD	Length of SYNC first pulse {667}
        0x04	-	    WORD	Length of SYNC second pulse {735}
        0x06	-	    WORD	Length of ZERO bit pulse {855}
        0x08	-	    WORD	Length of ONE bit pulse {1710}
        0x0A	-	    WORD	Length of PILOT tone (number of pulses) {8063 header (flag<128), 3223 data (flag>=128)}
        0x0C	-	    BYTE	Used bits in the last byte (other bits should be 0) {8}
                                (e.g. if this is 6, then the bits used (x) in the last byte are: xxxxxx00, 
                                where MSb is the leftmost bit, LSb is the rightmost bit)
        0x0D	-	    WORD	Pause after this block (ms.) {1000}
        0x0F	N	    BYTE[3]	Length of data that follow
        0x12	-	    BYTE[N]	Data as in .TAP files                  

        This block is very similar to the normal TAP block but with some additional info on the timings and other important 
        differences. The same tape encoding is used as for the standard speed data block. If a block should use some non-standard 
        sync or pilot tones (i.e. all sorts of protection schemes) then use the next three blocks to describe it.*/
        private void ProcessBlockID11(byte[] data)
        {
            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x11;
            t.BlockDescription = BlockType.Turbo_Speed_Data_Block;
            t.DataPeriods = new List<int>();

            int pilotPL = GetWordValue(data, _position);
            int sync1P = GetWordValue(data, _position + 2);
            int sync2P = GetWordValue(data, _position + 4);
            int bit0P = GetWordValue(data, _position + 6);
            int bit1P = GetWordValue(data, _position + 8);
            int pilotTL = GetWordValue(data, _position + 10);
            int bitinbyte = data[_position + 12];
            int pause = GetWordValue(data, _position + 13);


            int blockLen = 0xFFFFFF & GetInt32(data, _position + 0x0F);

            byte[] bLenArr = data.Skip(_position + 0x0F).Take(3).ToArray();

            _position += 0x12;

            byte[] tmp = new byte[blockLen];
            tmp = data.Skip(_position).Take(blockLen).ToArray();

            var t2 = DecodeDataBlock(t, tmp, DataBlockType.Turbo, pause, pilotTL, pilotPL, sync1P, sync2P, bit0P, bit1P, bitinbyte);

            t.PauseInMS = pause;

            // add the block
            _datacorder.DataBlocks.Add(t2);

            // advance the position to the next block
            _position += blockLen;

            // generate PAUSE block
            CreatePauseBlock(_datacorder.DataBlocks.Last());
        }
        #endregion

        #region ID 12 - Pure Tone
/*              length: 04
        Offset	Value	Type	Description
        0x00	-	    WORD	Length of one pulse in T-states
        0x02	-	    WORD	Number of pulses                                

        This will produce a tone which is basically the same as the pilot tone in the ID 10, ID 11 blocks. You can define how 
        long the pulse is and how many pulses are in the tone. */
        private void ProcessBlockID12(byte[] data)
        {
            int blockLen = 4;

            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x12;
            t.BlockDescription = BlockType.Pure_Tone;
            t.DataPeriods = new List<int>();
            t.PauseInMS = 0;

            // get values
            int pulseLength = GetWordValue(data, _position);
            int pulseCount = GetWordValue(data, _position + 2);

            t.AddMetaData(BlockDescriptorTitle.Pulse_Length, pulseLength.ToString() + " T-States");
            t.AddMetaData(BlockDescriptorTitle.Pulse_Count, pulseCount.ToString());

            // build period information
            for (int p = 0; p < pulseCount; p++)
            {
                t.DataPeriods.Add(pulseLength);
            }

            // add the block
            _datacorder.DataBlocks.Add(t);

            // advance the position to the next block
            _position += blockLen;
        }
        #endregion

        #region ID 13 - Pulse sequence
/*              length: [00]*02+01
        Offset	Value	Type	Description
        0x00	N	    BYTE	Number of pulses
        0x01	-	    WORD[N]	Pulses' lengths                               

        This will produce N pulses, each having its own timing. Up to 255 pulses can be stored in this block; this is useful for non-standard 
        sync tones used by some protection schemes. */
        private void ProcessBlockID13(byte[] data)
        {
            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x13;
            t.BlockDescription = BlockType.Pulse_Sequence;
            t.DataPeriods = new List<int>();

            t.PauseInMS = 0;

            // get pulse count           
            int pulseCount = data[_position];
            t.AddMetaData(BlockDescriptorTitle.Pulse_Count, pulseCount.ToString());
            _position++;            

            // build period information
            for (int p = 0; p < pulseCount; p++, _position += 2)
            {
                // get pulse length
                int pulseLength = GetWordValue(data, _position);
                t.AddMetaData(BlockDescriptorTitle.Needs_Parsing, "Pulse " + p + " Length\t" + pulseLength.ToString() + " T-States");
                t.DataPeriods.Add(pulseLength);
            }

            // add the block
            _datacorder.DataBlocks.Add(t);
        }
        #endregion

        #region ID 14 - Pure Data Block
/*              length: [07,08,09]+0A
        Offset	Value	Type	Description
        0x00	-	    WORD	Length of ZERO bit pulse
        0x02	-	    WORD	Length of ONE bit pulse
        0x04	-	    BYTE	Used bits in last byte (other bits should be 0)
                                (e.g. if this is 6, then the bits used (x) in the last byte are: xxxxxx00, 
                                where MSb is the leftmost bit, LSb is the rightmost bit)
        0x05	-	    WORD	Pause after this block (ms.)
        0x07	N	    BYTE[3]	Length of data that follow
        0x0A	-	    BYTE[N]	Data as in .TAP files                             

        This is the same as in the turbo loading data block, except that it has no pilot or sync pulses. */
        private void ProcessBlockID14(byte[] data)
        {
            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x14;
            t.BlockDescription = BlockType.Pure_Data_Block;
            t.DataPeriods = new List<int>();

            int pilotPL = 0;
            int sync1P = 0;
            int sync2P = 0;
            int bit0P = GetWordValue(data, _position + 0);
            int bit1P = GetWordValue(data, _position + 2);
            int pilotTL = 0;
            int bitinbyte = data[_position + 4];
            int pause = GetWordValue(data, _position + 5);

            int blockLen = 0xFFFFFF & GetInt32(data, _position + 0x07);

            _position += 0x0A;

            byte[] tmp = new byte[blockLen];
            tmp = data.Skip(_position).Take(blockLen).ToArray();

            var t2 = DecodeDataBlock(t, tmp, DataBlockType.Pure, pause, pilotTL, pilotPL, sync1P, sync2P, bit0P, bit1P, bitinbyte);

            t.PauseInMS = pause;

            // add the block
            _datacorder.DataBlocks.Add(t2);

            // advance the position to the next block
            _position += blockLen;

            // generate PAUSE block
            CreatePauseBlock(_datacorder.DataBlocks.Last());
        }
        #endregion

        #region ID 15 - Direct Recording
/*              length: [05,06,07]+08
        Offset	Value	Type	Description
        0x00	-	    WORD	Number of T-states per sample (bit of data)
        0x02	-	    WORD	Pause after this block in milliseconds (ms.)
        0x04	-	    BYTE	Used bits (samples) in last byte of data (1-8)
                                (e.g. if this is 2, only first two samples of the last byte will be played)
        0x05	N	    BYTE[3]	Length of samples' data
        0x08	-	    BYTE[N]	Samples data. Each bit represents a state on the EAR port (i.e. one sample).
                                MSb is played first.                            

        This block is used for tapes which have some parts in a format such that the turbo loader block cannot be used. 
        This is not like a VOC file, since the information is much more compact. Each sample value is represented by one bit only 
        (0 for low, 1 for high) which means that the block will be at most 1/8 the size of the equivalent VOC.
        The preferred sampling frequencies are 22050 or 44100 Hz (158 or 79 T-states/sample). 
        Please, if you can, don't use other sampling frequencies.
        Please use this block only if you cannot use any other block. */
        private void ProcessBlockID15(byte[] data)
        {
            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x15;
            t.BlockDescription = BlockType.Direct_Recording;
            t.DataPeriods = new List<int>();

            // get values
            int samLen = GetInt32(data, _position + 5);
            int samSize = 0xFFFFFF & samLen;

            int tStatesPerSample = GetWordValue(data, _position);
            int pauseAfterBlock = GetWordValue(data, _position + 2);
            int usedBitsInLastByte = data[_position + 4];

            // skip to samples data
            _position += 8;

            int pulseLength = 0;
            int pulseCount = 0;

            // ascertain the pulse count
            for (int i = 0; i < samSize; i++)
            {
                for (int p = 0x80; p != 0; p >>= 1)
                {
                    if (((data[_position + i] ^ pulseLength) & p) != 0)
                    {
                        pulseCount++;
                        pulseLength ^= -1;
                    }
                }
            }

            // get the pulses
            t.DataPeriods = new List<int>(pulseCount + 2);
            int tStateCount = 0;
            pulseLength = 0;
            for (int i = 1; i < samSize; i++)
            {
                for (int p = 0x80; p != 0; p >>= 1)
                {
                    tStateCount += tStatesPerSample;
                    if (((data[_position] ^ pulseLength) & p) != 0)
                    {
                        t.DataPeriods.Add(tStateCount);
                        pulseLength ^= -1;
                        tStateCount = 0;
                    }
                }

                // incrememt position
                _position++;
            }

            // get the pulses in the last byte of data
            for (int p = 0x80; p != (byte)(0x80 >> usedBitsInLastByte); p >>= 1)
            {
                tStateCount += tStatesPerSample;
                if (((data[_position] ^ pulseLength) & p) != 0)
                {
                    t.DataPeriods.Add(tStateCount);
                    pulseLength ^= -1;
                    tStateCount = 0;
                }
            }

            // add final pulse
            t.DataPeriods.Add(tStateCount);

            // add end of block pause
            if (pauseAfterBlock > 0)
            {
                //t.DataPeriods.Add(3500 * pauseAfterBlock);
            }

            t.PauseInMS = pauseAfterBlock;
            
            // increment position
            _position++;

            // add the block
            _datacorder.DataBlocks.Add(t);

            // generate PAUSE block
            CreatePauseBlock(_datacorder.DataBlocks.Last());
        }
        #endregion

        #region ID 18 - CSW Recording
/*              length: [00,01,02,03]+04
        Offset	Value	Type	Description
        0x00	10+N	DWORD	Block length (without these four bytes)
        0x04	-	    WORD	Pause after this block (in ms).
        0x06	-	    BYTE[3]	Sampling rate
        0x09	-	    BYTE	Compression type
                                0x01: RLE
                                0x02: Z-RLE
        0x0A	-	    DWORD	Number of stored pulses (after decompression, for validation purposes)
        0x0E	-	    BYTE[N]	CSW data, encoded according to the CSW file format specification.                          

        This block contains a sequence of raw pulses encoded in CSW format v2 (Compressed Square Wave). */
        private void ProcessBlockID18(byte[] data)
        {
            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x18;
            t.BlockDescription = BlockType.CSW_Recording;
            t.DataPeriods = new List<int>();

            int blockLen = GetInt32(data, _position);
            _position += 4;

            t.PauseInMS = GetWordValue(data, _position);

            _position += 2;

            int sampleRate = data[_position++] << 16 | data[_position++] << 8 | data[_position++];
            byte compType = data[_position++];
            int pulses = GetInt32(data, _position);
            _position += 4;

            int dataLen = blockLen - 10;

            // build source array
            byte[] src = new byte[dataLen];
            // build destination array
            byte[] dest = new byte[pulses + 1];

            // process the CSW data
            CswConverter.ProcessCSWV2(src, ref dest, compType, pulses);

            // create the periods
            var rate = (69888 * 50) / sampleRate;

            for (int i = 0; i < dest.Length;)
            {
                int length = dest[i++] * rate;
                if (length == 0)
                {
                    length = GetInt32(dest, i) / rate;
                    i += 4;
                }

                t.DataPeriods.Add(length);
            }

            // add closing period
            t.DataPeriods.Add((69888 * 50) / 10);

            _position += dataLen;
            //_position += blockLen;

            // add the block
            _datacorder.DataBlocks.Add(t);

            // generate PAUSE block
            CreatePauseBlock(_datacorder.DataBlocks.Last());
        }
        #endregion

        #region ID 19 - Generalized Data Block
/*              length: [00,01,02,03]+04
        Offset	                    Value	Type	    Description
        0x00	                    -	    DWORD	    Block length (without these four bytes)
        0x04	                    -	    WORD	    Pause after this block (ms)
        0x06	                    TOTP	DWORD	    Total number of symbols in pilot/sync block (can be 0)
        0x0A	                    NPP	    BYTE	    Maximum number of pulses per pilot/sync symbol
        0x0B	                    ASP	    BYTE	    Number of pilot/sync symbols in the alphabet table (0=256)
        0x0C	                    TOTD	DWORD	    Total number of symbols in data stream (can be 0)
        0x10	                    NPD	    BYTE	    Maximum number of pulses per data symbol
        0x11	                    ASD	    BYTE	    Number of data symbols in the alphabet table (0=256)
        0x12	                    -	    SYMDEF[ASP]	Pilot and sync symbols definition table
                                                        This field is present only if TOTP>0
        0x12+
        (2*NPP+1)*ASP	            -	    PRLE[TOTP]	Pilot and sync data stream
                                                        This field is present only if TOTP>0
        0x12+
        (TOTP>0)*((2*NPP+1)*ASP)+
        TOTP*3	                    -	    SYMDEF[ASD]	Data symbols definition table
                                                        This field is present only if TOTD>0
        0x12+
        (TOTP>0)*((2*NPP+1)*ASP)+
        TOTP*3+
        (2*NPD+1)*ASD	            -	    BYTE[DS]	Data stream
                                                        This field is present only if TOTD>0                 

        This block has been specifically developed to represent an extremely wide range of data encoding techniques.
        The basic idea is that each loading component (pilot tone, sync pulses, data) is associated to a specific sequence 
        of pulses, where each sequence (wave) can contain a different number of pulses from the others. 
        In this way we can have a situation where bit 0 is represented with 4 pulses and bit 1 with 8 pulses.    

        ----
        SYMDEF structure format
        Offset	Value	Type	Description
        0x00	-	    BYTE	Symbol flags
                            b0-b1: starting symbol polarity
                                    00: opposite to the current level (make an edge, as usual) - default
                                    01: same as the current level (no edge - prolongs the previous pulse)
                                    10: force low level
                                    11: force high level
        0x01	-	    WORD[MAXP]	Array of pulse lengths.

        The alphabet is stored using a table where each symbol is a row of pulses. The number of columns (i.e. pulses) of the table is the 
        length of the longest sequence amongst all (MAXP=NPP or NPD, for pilot/sync or data blocks respectively); shorter waves are terminated by a 
        zero-length pulse in the sequence.
        Any number of data symbols is allowed, so we can have more than two distinct waves; for example, imagine a loader which writes two bits at a 
        time by encoding them with four distinct pulse lengths: this loader would have an alphabet of four symbols, each associated to a specific 
        sequence of pulses (wave).
        ----
        ----
        PRLE structure format
        Offset	Value	Type	Description
        0x00	-	    BYTE	Symbol to be represented
        0x01	-	    WORD	Number of repetitions

        Most commonly, pilot and sync are repetitions of the same pulse, thus they are represented using a very simple RLE encoding structure which stores 
        the symbol and the number of times it must be repeated.
        Each symbol in the data stream is represented by a string of NB bits of the block data, where NB = ceiling(Log2(ASD)). 
        Thus the length of the whole data stream in bits is NB*TOTD, or in bytes DS=ceil(NB*TOTD/8). 
        ----                                                    */
        private void ProcessBlockID19(byte[] data)
        {
            // not currently implemented properly

            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x19;
            t.BlockDescription = BlockType.Generalized_Data_Block;
            t.DataPeriods = new List<int>();

            int blockLen = GetInt32(data, _position);
            _position += 4;

            int pause = GetWordValue(data, _position);
            _position += 2;

            int totp = GetInt32(data, _position);
            _position += 4;

            int npp = data[_position++];

            int asp = data[_position++];

            int totd = GetInt32(data, _position);
            _position += 4;

            int npd = data[_position++];

            int asd = data[_position++];

            // add the block
            _datacorder.DataBlocks.Add(t);

            // advance the position to the next block
            _position += blockLen;
        }
        #endregion

        #region ID 20 - Pause (silence) or 'Stop the Tape' command
/*              length: 02
        Offset	Value	Type	Description
        0x00	-	    WORD	Pause duration (ms.)                 

        This will make a silence (low amplitude level (0)) for a given time in milliseconds. If the value is 0 then the 
        emulator or utility should (in effect) STOP THE TAPE, i.e. should not continue loading until the user or emulator requests it.     */
        private void ProcessBlockID20(byte[] data)
        {
            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x20;
            t.DataPeriods = new List<int>();
            t.BlockDescription = BlockType.Pause_or_Stop_the_Tape;

            int pauseDuration = GetWordValue(data, _position);
            if (pauseDuration != 0)
            {
                //t.BlockDescription = "Pause: " + pauseDuration + " ms";
            }
            else
            {
                //t.BlockDescription = "[STOP THE TAPE]";
            }

            t.PauseInMS = pauseDuration;

            if (pauseDuration == 0)
            {
                // issue stop the tape command
                t.Command = TapeCommand.STOP_THE_TAPE;
                // add 1ms period
                //t.DataPeriods.Add(3500);
                //pauseDuration = -1;
                
            }
            else
            {
                // this is actually just a pause
                //pauseDuration = 3500 * pauseDuration;
                //t.DataPeriods.Add(pauseDuration);
            }

            // add end of block pause
            //t.DataPeriods.Add(pauseDuration);

            // add to tape
            _datacorder.DataBlocks.Add(t);

            // advanced position to next block
            _position += 2;

            // generate PAUSE block
            CreatePauseBlock(_datacorder.DataBlocks.Last());

        }
        #endregion

        #region ID 21 - Group start
/*              length: [00]+01
        Offset	Value	Type	Description
        0x00	L	BYTE	Length of the group name string
        0x01	-	CHAR[L]	Group name in ASCII format (please keep it under 30 characters long)                

        This block marks the start of a group of blocks which are to be treated as one single (composite) block. 
        This is very handy for tapes that use lots of subblocks like Bleepload (which may well have over 160 custom loading blocks). 
        You can also give the group a name (example 'Bleepload Block 1').
        For each group start block, there must be a group end block. Nesting of groups is not allowed.           */
        private void ProcessBlockID21(byte[] data)
        {
            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x21;
            t.DataPeriods = new List<int>();
            t.BlockDescription = BlockType.Group_Start;

            int nameLength = data[_position];
            _position++;

            string name = Encoding.ASCII.GetString(data, _position, nameLength);
            //t.BlockDescription = "[GROUP: " + name + "]";
            t.Command = TapeCommand.BEGIN_GROUP;

            t.PauseInMS = 0;

            // add to tape
            _datacorder.DataBlocks.Add(t);

            // advance to next block 
            _position += nameLength;
        }
        #endregion

        #region ID 22 - Group end
/*              length: 00              
             
        This indicates the end of a group. This block has no body.           */
        private void ProcessBlockID22(byte[] data)
        {
            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x22;
            t.DataPeriods = new List<int>();
            t.BlockDescription = BlockType.Group_End;
            t.Command = TapeCommand.END_GROUP;

            t.PauseInMS = 0;

            // add to tape
            _datacorder.DataBlocks.Add(t);
        }
        #endregion

        #region ID 23 - Jump to block
/*              length: 02
        Offset	Value	Type	Description
        0x00	-	    WORD	Relative jump value              

        This block will enable you to jump from one block to another within the file. The value is a signed short word 
        (usually 'signed short' in C); Some examples:
            Jump 0 = 'Loop Forever' - this should never happen
            Jump 1 = 'Go to the next block' - it is like NOP in assembler ;)
            Jump 2 = 'Skip one block'
            Jump -1 = 'Go to the previous block'
        All blocks are included in the block count!.           */
        private void ProcessBlockID23(byte[] data)
        {
            // not implemented properly

            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x23;
            t.DataPeriods = new List<int>();
            t.BlockDescription = BlockType.Jump_to_Block;

            int relativeJumpValue = GetWordValue(data, _position);
            string result = string.Empty;

            switch(relativeJumpValue)
            {
                case 0:
                    result = "Loop Forever";
                    break;
                case 1:
                    result = "To Next Block";
                    break;
                case 2:
                    result = "Skip One Block";
                    break;
                case -1:
                    result = "Go to Previous Block";
                    break;
            }

            //t.BlockDescription = "[JUMP BLOCK - " + result +"]";

            t.PauseInMS = 0;

            // add to tape
            _datacorder.DataBlocks.Add(t);

            // advance to next block 
            _position += 2;
        }
        #endregion

        #region ID 24 - Loop start
/*              length: 02
        Offset	Value	Type	Description
        0x00	-	    WORD	Number of repetitions (greater than 1)           

        If you have a sequence of identical blocks, or of identical groups of blocks, you can use this block to tell how many times they should 
        be repeated. This block is the same as the FOR statement in BASIC.
        For simplicity reasons don't nest loop blocks!           */
        private void ProcessBlockID24(byte[] data)
        {
            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x24;
            t.DataPeriods = new List<int>();
            t.BlockDescription = BlockType.Loop_Start;

            // loop should start from the next block
            int loopStart = _datacorder.DataBlocks.Count() + 1;

            int numberOfRepetitions = GetWordValue(data, _position);

            // update loop counter
            _loopCounter.Add(
                new KeyValuePair<int, int>(
                    loopStart,
                    numberOfRepetitions));

            // update description
            //t.BlockDescription = "[LOOP START - " + numberOfRepetitions + " times]";

            t.PauseInMS = 0;

            // add to tape
            _datacorder.DataBlocks.Add(t);

            // advance to next block 
            _position += 2;
        }
        #endregion

        #region ID 25 - Loop end
/*              length: 00    

        This is the same as BASIC's NEXT statement. It means that the utility should jump back to the start of the loop if it hasn't 
        been run for the specified number of times.
        This block has no body.          */
        private void ProcessBlockID25(byte[] data)
        {
            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x25;
            t.DataPeriods = new List<int>();
            t.BlockDescription = BlockType.Loop_End;

            // get the most recent loop info
            var loop = _loopCounter.LastOrDefault();

            int loopStart = loop.Key;
            int numberOfRepetitions = loop.Value;

            if (numberOfRepetitions == 0)
            {
                return;
            }

            // get the number of blocks to loop
            int blockCnt = _datacorder.DataBlocks.Count() - loopStart;

            // loop through each group to repeat
            for (int b = 0; b < numberOfRepetitions; b++)
            {
                TapeDataBlock repeater = new TapeDataBlock();
                //repeater.BlockDescription = "[LOOP REPEAT - " + (b + 1) + "]";
                repeater.DataPeriods = new List<int>();

                // add the repeat block
                _datacorder.DataBlocks.Add(repeater);

                // now iterate through and add the blocks to be repeated
                for (int i = 0; i < blockCnt; i++)
                {
                    var block = _datacorder.DataBlocks[loopStart + i];
                    _datacorder.DataBlocks.Add(block);
                }
            }
        }
        #endregion

        #region ID 26 - Call sequence
/*              length: [00,01]*02+02
        Offset	Value	Type	Description
        0x00	N	    WORD	Number of calls to be made
        0x02	-	    WORD[N]	Array of call block numbers (relative-signed offsets)    

        This block is an analogue of the CALL Subroutine statement. It basically executes a sequence of blocks that are somewhere else and 
        then goes back to the next block. Because more than one call can be normally used you can include a list of sequences to be called. 
        The 'nesting' of call blocks is also not allowed for the simplicity reasons. You can, of course, use the CALL blocks in the LOOP 
        sequences and vice versa. The value is relative for the obvious reasons - so that you can add some blocks in the beginning of the 
        file without disturbing the call values. Please take a look at 'Jump To Block' for reference on the values.          */
        private void ProcessBlockID26(byte[] data)
        {
            // block processing not implemented for this - just gets added for informational purposes only
            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x26;
            t.DataPeriods = new List<int>();
            t.BlockDescription = BlockType.Call_Sequence;

            int blockSize = 2 + 2 * GetWordValue(data, _position);
            t.PauseInMS = 0;


            // add to tape
            _datacorder.DataBlocks.Add(t);

            // advance to next block 
            _position += blockSize;
        }
        #endregion

        #region ID 27 - Return from sequence
/*              length: 00  

        This block indicates the end of the Called Sequence. The next block played will be the block after the last CALL block (or the next Call, 
        if the Call block had multiple calls).
        Again, this block has no body.          */
        private void ProcessBlockID27(byte[] data)
        {
            // block processing not implemented for this - just gets added for informational purposes only
            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x27;
            t.DataPeriods = new List<int>();
            t.BlockDescription = BlockType.Return_From_Sequence;
            t.PauseInMS = 0;


            // add to tape
            _datacorder.DataBlocks.Add(t);
        }
        #endregion

        #region ID 28 - Select block
/*              length: [00,01]+02
        Offset	Value	Type	Description
        0x00	-	    WORD	Length of the whole block (without these two bytes)
        0x02	N	    BYTE	Number of selections
        0x03	-	    SELECT[N]	List of selections  

        ----
        SELECT structure format
        Offset	Value	Type	Description
        0x00	-	    WORD	Relative Offset
        0x02	L	    BYTE	Length of description text
        0x03	-	    CHAR[L]	Description text (please use single line and max. 30 chars)
        ----

        This block is useful when the tape consists of two or more separately-loadable parts. With this block, you are able to select 
        one of the parts and the utility/emulator will start loading from that block. For example you can use it when the game has a 
        separate Trainer or when it is a multiload. Of course, to make some use of it the emulator/utility has to show a menu with the 
        selections when it encounters such a block. All offsets are relative signed words.          */
        private void ProcessBlockID28(byte[] data)
        {
            // block processing not implemented for this - just gets added for informational purposes only
            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x28;
            t.DataPeriods = new List<int>();
            t.BlockDescription = BlockType.Select_Block;

            int blockSize = 2 + GetWordValue(data, _position);

            t.PauseInMS = 0;

            // add to tape
            _datacorder.DataBlocks.Add(t);

            // advance to next block 
            _position += blockSize;
        }
        #endregion

        #region ID 2A - Stop the tape if in 48K mode
/*              length: 04
                Offset	Value	Type	Description
                0x00	0	    DWORD	Length of the block without these four bytes (0)

                When this block is encountered, the tape will stop ONLY if the machine is an 48K Spectrum. This block is to be used for 
                multiloading games that load one level at a time in 48K mode, but load the entire tape at once if in 128K mode.
                This block has no body of its own, but follows the extension rule.          */
        private void ProcessBlockID2A(byte[] data)
        {
            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x2A;
            t.DataPeriods = new List<int>();
            t.BlockDescription = BlockType.Stop_the_Tape_48K;
            t.Command = TapeCommand.STOP_THE_TAPE_48K;

            int blockSize = 4 + GetWordValue(data, _position);

            t.PauseInMS = 0;

            // add to tape
            _datacorder.DataBlocks.Add(t);

            // advance to next block 
            _position += blockSize;
        }
        #endregion

        #region ID 2B - Set signal level
/*              length: 05
        Offset	Value	Type	Description
        0x00	1	    DWORD	Block length (without these four bytes)
        0x04	-	    BYTE	Signal level (0=low, 1=high)

        This block sets the current signal level to the specified value (high or low). It should be used whenever it is necessary to avoid any 
        ambiguities, e.g. with custom loaders which are level-sensitive.         */
        private void ProcessBlockID2B(byte[] data)
        {
            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x2B;
            t.DataPeriods = new List<int>();
            t.BlockDescription = BlockType.Set_Signal_Level;

            t.PauseInMS = 0;

            // add to tape
            _datacorder.DataBlocks.Add(t);

            // advance to next block 
            _position += 5;
        }
        #endregion

        #region ID 30 - Text description
/*              length: [00]+01
        Offset	Value	Type	Description
        0x00	N	    BYTE	Length of the text description
        0x01	-	    CHAR[N]	Text description in ASCII format

        This is meant to identify parts of the tape, so you know where level 1 starts, where to rewind to when the game ends, etc. 
        This description is not guaranteed to be shown while the tape is playing, but can be read while browsing the tape or changing 
        the tape pointer.
        The description can be up to 255 characters long but please keep it down to about 30 so the programs can show it in one line 
        (where this is appropriate).
        Please use 'Archive Info' block for title, authors, publisher, etc.        */
        private void ProcessBlockID30(byte[] data)
        {
            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x30;
            t.DataPeriods = new List<int>();
            t.BlockDescription = BlockType.Text_Description;

            int textLen = data[_position];
            _position++;

            string desc = Encoding.ASCII.GetString(data, _position, textLen);

            t.PauseInMS = 0;

            //t.BlockDescription = "[" + desc + "]";

            // add to tape
            _datacorder.DataBlocks.Add(t);

            // advance to next block 
            _position += textLen;
        }
        #endregion

        #region ID 31 - Message block
/*              length: [01]+02
        Offset	Value	Type	Description
        0x00	-	    BYTE	Time (in seconds) for which the message should be displayed
        0x01	N	    BYTE	Length of the text message
        0x02	-	    CHAR[N]	Message that should be displayed in ASCII format

        This will enable the emulators to display a message for a given time. This should not stop the tape and it should not make silence. 
        If the time is 0 then the emulator should wait for the user to press a key.
        The text message should:
            stick to a maximum of 30 chars per line;
            use single 0x0D (13 decimal) to separate lines;
            stick to a maximum of 8 lines.
        If you do not obey these rules, emulators may display your message in any way they like.        */
        private void ProcessBlockID31(byte[] data)
        {
            // currently not implemented properly in ZXHawk

            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x31;
            t.DataPeriods = new List<int>();
            t.BlockDescription = BlockType.Message_Block;
            
            _position++;

            int msgLen = data[_position];
            _position++;

            string desc = Encoding.ASCII.GetString(data, _position, msgLen);

            t.Command = TapeCommand.SHOW_MESSAGE;

            //t.BlockDescription = "[MESSAGE: " + desc + "]";

            t.PauseInMS = 0;

            // add to tape
            _datacorder.DataBlocks.Add(t);

            // advance to next block 
            _position += msgLen;
        }
        #endregion

        #region ID 32 - Archive info
/*              length: [00,01]+02
        Offset	Value	Type	Description
        0x00	-	    WORD	Length of the whole block (without these two bytes)
        0x02	N	    BYTE	Number of text strings
        0x03	-	    TEXT[N]	List of text strings

        ----
        TEXT structure format
        Offset	Value	Type	Description
        0x00	-	    BYTE	Text identification byte:
                                    00 - Full title
                                    01 - Software house/publisher
                                    02 - Author(s)
                                    03 - Year of publication
                                    04 - Language
                                    05 - Game/utility type
                                    06 - Price
                                    07 - Protection scheme/loader
                                    08 - Origin
                                    FF - Comment(s)
        0x01	L	    BYTE	Length of text string
        0x02	-	    CHAR[L]	Text string in ASCII format
        ----

        Use this block at the beginning of the tape to identify the title of the game, author, publisher, year of publication, price (including 
        the currency), type of software (arcade adventure, puzzle, word processor, ...), protection scheme it uses (Speedlock 1, Alkatraz, ...) 
        and its origin (Original, Budget re-release, ...), etc. This block is built in a way that allows easy future expansion. 
        The block consists of a series of text strings. Each text has its identification number (which tells us what the text means) and then 
        the ASCII text. To make it possible to skip this block, if needed, the length of the whole block is at the beginning of it.
        If all texts on the tape are in English language then you don't have to supply the 'Language' field
        The information about what hardware the tape uses is in the 'Hardware Type' block, so no need for it here.              */
        private void ProcessBlockID32(byte[] data)
        {
            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x32;
            t.DataPeriods = new List<int>();
            t.BlockDescription = BlockType.Archive_Info;

            int blockLen = GetWordValue(data, _position);
            _position += 2;
            int stringCount = data[_position++];

            // iterate through each string
            for (int s = 0; s < stringCount; s++)
            {
                // identify the type of text
                int type = data[_position++];

                // get text length
                int strLen = data[_position++];

                string title = "Info: ";

                switch (type)
                {
                    case 0x00:
                        title = "Full Title: ";
                        break;
                    case 0x01:
                        title = "Software House/Publisher: ";
                        break;
                    case 0x02:
                        title = "Author(s): ";
                        break;
                    case 0x03:
                        title = "Year of Publication: ";
                        break;
                    case 0x04:
                        title = "Language: ";
                        break;
                    case 0x05:
                        title = "Game/Utility Type: ";
                        break;
                    case 0x06:
                        title = "Price: ";
                        break;
                    case 0x07:
                        title = "Protection Scheme/Loader: ";
                        break;
                    case 0x08:
                        title = "Origin: ";
                        break;
                    case 0xFF:
                        title = "Comment(s): ";
                        break;
                    default:
                        break;
                }

                // add title to description
                //t.BlockDescription += title;

                // get string data
                string val = Encoding.ASCII.GetString(data, _position, strLen);
                //t.BlockDescription += val + " \n";

                t.PauseInMS = 0;

                // advance to next string block
                _position += strLen;
            }

            // add to tape
            _datacorder.DataBlocks.Add(t);
        }
        #endregion

        #region ID 33 - Hardware type
/*              length: [00]*03+01
        Offset	Value	Type	Description
        0x00	N	    BYTE	Number of machines and hardware types for which info is supplied
        0x01	-	    HWINFO[N]	List of machines and hardware

        ----
        HWINFO structure format
        Offset	Value	Type	Description
        0x00	-	    BYTE	Hardware type
        0x01	-	    BYTE	Hardware ID
        0x02	-	    BYTE	Hardware information:
                                    00 - The tape RUNS on this machine or with this hardware,
                                            but may or may not use the hardware or special features of the machine.
                                    01 - The tape USES the hardware or special features of the machine,
                                            such as extra memory or a sound chip.
                                    02 - The tape RUNS but it DOESN'T use the hardware
                                            or special features of the machine.
                                    03 - The tape DOESN'T RUN on this machine or with this hardware.
        ----

        This blocks contains information about the hardware that the programs on this tape use. Please include only machines and hardware for 
        which you are 100% sure that it either runs (or doesn't run) on or with, or you know it uses (or doesn't use) the hardware or special 
        features of that machine.
        If the tape runs only on the ZX81 (and TS1000, etc.) then it clearly won't work on any Spectrum or Spectrum variant, so there's no 
        need to list this information.
        If you are not sure or you haven't tested a tape on some particular machine/hardware combination then do not include it in the list.
        The list of hardware types and IDs is somewhat large, and may be found at the end of the format description.              */
        private void ProcessBlockID33(byte[] data)
        {
            // currently not implemented properly in ZXHawk

            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x33;
            t.DataPeriods = new List<int>();
            t.BlockDescription = BlockType.Hardware_Type;

            t.PauseInMS = 0;

            // first byte contains number of HWINFOs
            int infos = data[_position];

            _position += 1;

            // now starts the HW infos (each block 3 bytes)
            for (int i = 0; i < infos; i++)
            {
                _position += 3;
            }

            // add to tape
            _datacorder.DataBlocks.Add(t);
        }
        #endregion

        #region ID 35 - Custom info block
/*              length: [10,11,12,13]+14
        Offset	Value	Type	Description
        0x00	-	    CHAR[10]	Identification string (in ASCII)
        0x10	L	    DWORD	Length of the custom info
        0x14	-	    BYTE[L]	Custom info                                

        This block can be used to save any information you want. For example, it might contain some information written by a utility, 
        extra settings required by a particular emulator, or even poke data.               */
        private void ProcessBlockID35(byte[] data)
        {
            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x35;
            t.DataPeriods = new List<int>();
            t.BlockDescription = BlockType.Custom_Info_Block;

            t.PauseInMS = 0;

            string info = Encoding.ASCII.GetString(data, _position, 0x10);
            //t.BlockDescription = "[CUSTOM INFO: " + info + "]";
            _position += 0x10;

            int blockLen = BitConverter.ToInt32(data, _position);
            _position += 4;

            // add to tape
            _datacorder.DataBlocks.Add(t);

            // advance to next block 
            _position += blockLen;
        }
        #endregion

        #region ID 5A - "Glue" block
/*              length: 09
        Offset	Value	Type	Description
        0x00	-	    BYTE[9]	Value: { "XTape!",0x1A,MajR,MinR } 
                                Just skip these 9 bytes and you will end up on the next ID.                                

        This block is generated when you merge two ZX Tape files together. It is here so that you can easily copy the files together and use 
        them. Of course, this means that resulting file would be 10 bytes longer than if this block was not used. All you have to do 
        if you encounter this block ID is to skip next 9 bytes.
        If you can avoid using this block for this purpose, then do so; it is preferable to use a utility to join the two files and 
        ensure that they are both of the higher version number.               */
        private void ProcessBlockID5A(byte[] data)
        {
            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x5A;
            t.DataPeriods = new List<int>();
            t.BlockDescription = BlockType.Glue_Block;

            t.PauseInMS = 0;

            // add to tape
            _datacorder.DataBlocks.Add(t);

            // advance to next block 
            _position += 9;
        }
        #endregion

        #region UnDetected Blocks

        private void ProcessUnidentifiedBlock(byte[] data)
        {
            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = -2;
            t.DataPeriods = new List<int>();
            t.BlockDescription = BlockType.Unsupported;
            //t.BlockDescription = "[UNSUPPORTED - 0x" + data[_position - 1]  + "]";

            _position += GetInt32(data, _position) & 0xFFFFFF;

            // add to tape
            _datacorder.DataBlocks.Add(t);

            // advance to next block 
            _position += 4;
        }

        #endregion

        #region Depreciated Blocks

        // These mostly should be ignored by ZXHawk - here for completeness

        #region ID 16 - C64 ROM Type Data Block
        private void ProcessBlockID16(byte[] data)
        {
            // zxhawk will not implement this block. it will however handle it so subsequent blocks can be parsed
            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x16;
            t.DataPeriods = new List<int>();
            t.BlockDescription = BlockType.C64_ROM_Type_Data_Block;

            t.PauseInMS = 0;

            // add to tape
            _datacorder.DataBlocks.Add(t);

            // advance to next block 
            int blockLen = GetInt32(data, _position);
            _position += blockLen;
        }
        #endregion

        #region ID 17 - C64 Turbo Tape Data Block
        private void ProcessBlockID17(byte[] data)
        {
            // zxhawk will not implement this block. it will however handle it so subsequent blocks can be parsed
            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x17;
            t.DataPeriods = new List<int>();
            t.BlockDescription = BlockType.C64_Turbo_Tape_Data_Block;

            t.PauseInMS = 0;

            // add to tape
            _datacorder.DataBlocks.Add(t);

            // advance to next block 
            int blockLen = GetInt32(data, _position);
            _position += blockLen;
        }
        #endregion

        #region ID 34 - Emulation info
        private void ProcessBlockID34(byte[] data)
        {
            // currently not implemented properly in ZXHawk

            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x34;
            t.DataPeriods = new List<int>();
            t.BlockDescription = BlockType.Emulation_Info;

            // add to tape
            _datacorder.DataBlocks.Add(t);

            // advance to next block 
            _position += 8;
        }
        #endregion

        #region ID 40 - Snapshot block
        /*              length: [01,02,03]+04
                Offset	Value	Type	Description
                0x00	-	    BYTE	Snapshot type:
                                            00: .Z80 format
                                            01: .SNA format
                0x01	L	    BYTE[3]	Snapshot length
                0x04	-	    BYTE[L]	Snapshot itself                               

                This would enable one to snapshot the game at the start and still have all the tape blocks (level data, etc.) in the same file. 
                Only .Z80 and .SNA snapshots are supported for compatibility reasons!
                The emulator should take care of that the snapshot is not taken while the actual Tape loading is taking place (which doesn't do much sense). 
                And when an emulator encounters the snapshot block it should load it and then continue with the next block.               */
        private void ProcessBlockID40(byte[] data)
        {
            // currently not implemented properly in ZXHawk

            TapeDataBlock t = new TapeDataBlock();
            t.BlockID = 0x40;
            t.DataPeriods = new List<int>();
            t.BlockDescription = BlockType.Snapshot_Block;

            _position++;

            int blockLen = data[_position] | 
                data[_position + 1] << 8 | 
                data[_position + 2] << 16;
            _position += 3;

            // add to tape
            _datacorder.DataBlocks.Add(t);

            // advance to next block 
            _position += blockLen;
        }
        #endregion

        #endregion

        #endregion

        #region DataBlockDecoder

        /// <summary>
        /// Used to process either a standard or turbo data block
        /// </summary>
        /// <param name="block"></param>
        /// <param name="blockData"></param>
        /// <returns></returns>
        private TapeDataBlock DecodeDataBlock
            (
                TapeDataBlock block,
                byte[] blockdata,
                DataBlockType dataBlockType,
                int pauseAfterBlock,
                int pilotCount,

                int pilotToneLength = 2168,
                int sync1PulseLength = 667,
                int sync2PulseLength = 735,
                int bit0PulseLength = 855,
                int bit1PulseLength = 1710,     
                int bitsInLastByte = 8
            )
        {
            // first get the block description
            string description = string.Empty;

            // process the type byte
            /*  (The type is 0,1,2 or 3 for a Program, Number array, Character array or Code file. 
                A SCREEN$ file is regarded as a Code file with start address 16384 and length 6912 decimal. 
                If the file is a Program file, parameter 1 holds the autostart line number (or a number >=32768 if no LINE parameter was given) 
                and parameter 2 holds the start of the variable area relative to the start of the program. If it's a Code file, parameter 1 holds 
                the start of the code block when saved, and parameter 2 holds 32768. For data files finally, the byte at position 14 decimal holds the variable name.)
            */

            int blockSize = blockdata.Length;

            // dont get description info for Pure Data Blocks
            if (dataBlockType != DataBlockType.Pure)
            {
                if (blockdata[0] == 0x00 && blockSize == 19)
                {
                    string fileName = Encoding.ASCII.GetString(blockdata.Skip(2).Take(10).ToArray()).Trim();
                    string type = "Unknown Type";
                    StringBuilder sb = new StringBuilder();

                    var param1 = GetWordValue(blockdata, 12);
                    var param2 = GetWordValue(blockdata, 14);

                    // header block - examine first byte of header
                    if (blockdata[1] == 0)
                    {
                        type = "Program";
                        sb.Append(type + ": ");
                        sb.Append(fileName + " ");
                    }
                    else if (blockdata[1] == 1)
                    {
                        type = "NumArray";
                        sb.Append(type + ": ");
                        sb.Append(fileName + " ");
                    }
                    else if (blockdata[1] == 2)
                    {
                        type = "CharArray";
                        sb.Append(type + ": ");
                        sb.Append(fileName + " ");
                    }
                    else if (blockdata[1] == 3)
                    {
                        type = "Code";
                        sb.Append(type + ": ");
                        sb.Append(fileName + " ");
                    }
                }
                else if (blockdata[0] == 0xff)
                {
                    // data block
                    description = "Data Block " + (blockSize - 2) + "bytes";
                    block.AddMetaData(BlockDescriptorTitle.Data_Bytes, (blockSize - 2).ToString() + " Bytes");
                }
                else
                {
                    // some other type (turbo data etc..)
                    description = string.Format("#{0} block, {1} bytes", blockdata[0].ToString("X2"), blockSize);
                    //description += string.Format(", crc {0}", ((crc != 0) ? string.Format("bad (#{0:X2}!=#{1:X2})", crcFile, crcValue) : "ok"));
                    block.AddMetaData(BlockDescriptorTitle.Undefined, description);
                }
                /*
                if (blockdata[0] == 0x00 && blockSize == 19 && (blockdata[1] == 0x00) || (blockdata[1] == 3 && blockdata.Length > 3))
                {
                    if (dataBlockType != DataBlockType.Turbo)
                    {
                        // This is the program header
                        string fileName = Encoding.ASCII.GetString(blockdata.Skip(2).Take(10).ToArray()).Trim();

                        string type = "";
                        if (blockdata[0] == 0x00)
                        {
                            type = "Program";
                            block.AddMetaData(BlockDescriptorTitle.Program, fileName);
                        }
                        else
                        {
                            type = "Bytes";
                            block.AddMetaData(BlockDescriptorTitle.Bytes, fileName);
                        }

                        // now build the description string
                        StringBuilder sb = new StringBuilder();
                        sb.Append(type + ": ");
                        sb.Append(fileName + " ");
                        sb.Append(GetWordValue(blockdata, 14));
                        sb.Append(":");
                        sb.Append(GetWordValue(blockdata, 12));
                        description = sb.ToString();
                    }
                }
                else if (blockdata[0] == 0xFF)
                {
                    // this is a data block
                    description = "Data Block " + (blockSize - 2) + "bytes";
                    block.AddMetaData(BlockDescriptorTitle.Data_Bytes, (blockSize - 2).ToString() + " Bytes");
                }
                else
                {
                    // other type
                    description = string.Format("#{0} block, {1} bytes", blockdata[0].ToString("X2"), blockSize);
                    //description += string.Format(", crc {0}", ((crc != 0) ? string.Format("bad (#{0:X2}!=#{1:X2})", crcFile, crcValue) : "ok"));
                    block.AddMetaData(BlockDescriptorTitle.Undefined, description);
                }  
                */              
            }

            // update metadata
            switch (dataBlockType)
            {
                case DataBlockType.Standard:
                case DataBlockType.Turbo:

                    if (dataBlockType == DataBlockType.Standard)
                        block.BlockDescription = BlockType.Standard_Speed_Data_Block;
                    if (dataBlockType == DataBlockType.Turbo)
                        block.BlockDescription = BlockType.Turbo_Speed_Data_Block;

                    block.AddMetaData(BlockDescriptorTitle.Pilot_Pulse_Length, pilotToneLength.ToString() + " T-States");
                    block.AddMetaData(BlockDescriptorTitle.Pilot_Pulse_Count, pilotCount.ToString() + " Pulses");
                    block.AddMetaData(BlockDescriptorTitle.First_Sync_Length, sync1PulseLength.ToString() + " T-States");
                    block.AddMetaData(BlockDescriptorTitle.Second_Sync_Length, sync2PulseLength.ToString() + " T-States");
                    break;

                case DataBlockType.Pure:
                    block.BlockDescription = BlockType.Pure_Data_Block;
                    break;
            }

            block.AddMetaData(BlockDescriptorTitle.Zero_Bit_Length, bit0PulseLength.ToString() + " T-States");
            block.AddMetaData(BlockDescriptorTitle.One_Bit_Length, bit1PulseLength.ToString() + " T-States");
            block.AddMetaData(BlockDescriptorTitle.Data_Length, blockSize.ToString() + " Bytes");
            block.AddMetaData(BlockDescriptorTitle.Bits_In_Last_Byte, bitsInLastByte.ToString() + " Bits");
            block.AddMetaData(BlockDescriptorTitle.Pause_After_Data, pauseAfterBlock.ToString() + " ms");

            // calculate period information
            List <int> dataPeriods = new List<int>();

            // generate pilot pulses

            if (pilotCount > 0)
            {
                for (int i = 0; i < pilotCount; i++)
                {
                    dataPeriods.Add(pilotToneLength);
                }

                // add syncro pulses
                dataPeriods.Add(sync1PulseLength);
                dataPeriods.Add(sync2PulseLength);
            }

            int pos = 0;

            // add bit0 and bit1 periods
            for (int i = 0; i < blockSize - 1; i++, pos++)
            {
                for (byte b = 0x80; b != 0; b >>= 1)
                {
                    if ((blockdata[i] & b) != 0)
                        dataPeriods.Add(bit1PulseLength);
                    else
                        dataPeriods.Add(bit0PulseLength);
                    if ((blockdata[i] & b) != 0)
                        dataPeriods.Add(bit1PulseLength);
                    else
                        dataPeriods.Add(bit0PulseLength);
                }
            }

            // add the last byte
            for (byte c = 0x80; c != (byte)(0x80 >> bitsInLastByte); c >>= 1)
            {
                if ((blockdata[pos] & c) != 0)
                    dataPeriods.Add(bit1PulseLength);
                else
                    dataPeriods.Add(bit0PulseLength);
                if ((blockdata[pos] & c) != 0)
                    dataPeriods.Add(bit1PulseLength);
                else
                    dataPeriods.Add(bit0PulseLength);
            }

            // add block pause if pause is not 0
            if (pauseAfterBlock != 0)
            {
                block.PauseInMS = pauseAfterBlock;
                //int actualPause = pauseAfterBlock * 3500;
                //dataPeriods.Add(actualPause);
            }            

            // add to the tapedatablock object
            block.DataPeriods = dataPeriods;

            // add the raw data
            block.BlockData = blockdata;

            return block;
        }

        /// <summary>
        /// Used to process either a standard or turbo data block
        /// </summary>
        /// <param name="block"></param>
        /// <param name="blockData"></param>
        /// <returns></returns>
        private TapeDataBlock DecodeDataBlock
            (
                TapeDataBlock block,
                byte[] blockData,
                DataBlockType dataBlockType,
                int pauseAfterBlock,

                int pilotToneLength = 2168,
                int sync1PulseLength = 667,
                int sync2PulseLength = 735,
                int bit0PulseLength = 855,
                int bit1PulseLength = 1710,
                int bitsInLastByte = 8
            )
        {

            // pilot count needs to be ascertained from flag byte
            int pilotCount;
            if (blockData[0] < 128)
                pilotCount = 8063;
            else
                pilotCount = 3223;

            // now we can decode
            var nBlock = DecodeDataBlock
                (
                    block,
                    blockData,
                    dataBlockType,
                    pauseAfterBlock,
                    pilotCount,
                    pilotToneLength,
                    sync1PulseLength,
                    sync2PulseLength,
                    bit0PulseLength,
                    bit1PulseLength,
                    bitsInLastByte
                );


            return nBlock;
        }

        #endregion

        #region Pause Block Creator

        /// <summary>
        /// If neccessary a seperate PAUSE block will be created
        /// </summary>
        /// <param name="original"></param>
        private void CreatePauseBlock(TapeDataBlock original)
        {
            if (original.PauseInMS > 0)
            {
                TapeDataBlock pBlock = new TapeDataBlock();
                pBlock.DataPeriods = new List<int>();
                pBlock.BlockDescription = BlockType.PAUSE_BLOCK;
                pBlock.PauseInMS = 0;
                var pauseInTStates = TranslatePause(original.PauseInMS);

                pBlock.AddMetaData(BlockDescriptorTitle.Block_ID, pauseInTStates.ToString() + " cycles");

                int by1000 = pauseInTStates / 70000;
                int rem1000 = pauseInTStates % 70000;

                if (by1000 > 1)
                {
                    pBlock.DataPeriods.Add(35000);
                    pBlock.DataPeriods.Add(pauseInTStates - 35000);
                }
                else
                {
                    pBlock.DataPeriods.Add(pauseInTStates);
                    pBlock.DataPeriods.Add(0);
                }

                _datacorder.DataBlocks.Add(pBlock);
            }
        }

        #endregion
    }

    public enum DataBlockType
    {
        Standard,
        Turbo,
        Pure
    }
}
