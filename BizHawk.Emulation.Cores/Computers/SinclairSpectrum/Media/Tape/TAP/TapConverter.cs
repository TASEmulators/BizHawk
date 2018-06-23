using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Reponsible for TAP format serializaton
    /// </summary>
    public class TapConverter : MediaConverter
    {
        /// <summary>
        /// The type of serializer
        /// </summary>
        private MediaConverterType _formatType = MediaConverterType.TAP;
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

        #region Construction

        private DatacorderDevice _datacorder;

        public TapConverter(DatacorderDevice _tapeDevice)
        {
            _datacorder = _tapeDevice;
        }

        #endregion

        #region TAP Format Constants

        /// <summary>
        /// Pilot pulse length
        /// </summary>
        public const int PILOT_PL = 2168;

        /// <summary>
        /// Pilot pulses in the ROM header block
        /// </summary>
        public const int HEADER_PILOT_COUNT = 8063;

        /// <summary>
        /// Pilot pulses in the ROM data block
        /// </summary>
        public const int DATA_PILOT_COUNT = 3223;

        /// <summary>
        /// Sync 1 pulse length
        /// </summary>
        public const int SYNC_1_PL = 667;

        /// <summary>
        /// Sync 2 pulse lenth
        /// </summary>
        public const int SYNC_2_PL = 735;

        /// <summary>
        /// Bit 0 pulse length
        /// </summary>
        public const int BIT_0_PL = 855;

        /// <summary>
        /// Bit 1 pulse length
        /// </summary>
        public const int BIT_1_PL = 1710;

        /// <summary>
        /// End sync pulse length
        /// </summary>
        public const int TERM_SYNC = 947;

        /// <summary>
        /// 1 millisecond pause
        /// </summary>
        public const int PAUSE_MS = 3500;

        /// <summary>
        /// Used bit count in last byte
        /// </summary>
        public const int BIT_COUNT_IN_LAST = 8;

        #endregion

        /// <summary>
        /// DeSerialization method
        /// </summary>
        /// <param name="data"></param>
        public override void Read(byte[] data)
        {
            /*
                The .TAP files contain blocks of tape-saved data. All blocks start with two bytes specifying how many bytes will follow (not counting the two length bytes). Then raw tape data follows, including the flag and checksum bytes. The checksum is the bitwise XOR of all bytes including the flag byte. For example, when you execute the line SAVE "ROM" CODE 0,2 this will result:

                |------ Spectrum-generated data -------|       |---------|

               13 00 00 03 52 4f 4d 7x20 02 00 00 00 00 80 f1 04 00 ff f3 af a3

               ^^^^^...... first block is 19 bytes (17 bytes+flag+checksum)
                     ^^... flag byte (A reg, 00 for headers, ff for data blocks)
                        ^^ first byte of header, indicating a code block

               file name ..^^^^^^^^^^^^^
               header info ..............^^^^^^^^^^^^^^^^^
               checksum of header .........................^^
               length of second block ........................^^^^^
               flag byte ............................................^^
               first two bytes of rom .................................^^^^^
               checksum (checkbittoggle would be a better name!).............^^
            */

            // clear existing tape blocks
            _datacorder.DataBlocks.Clear();

            // convert bytearray to memory stream
            MemoryStream stream = new MemoryStream(data);

            // the first 2 bytes of the TAP file designate the length of the first data block
            // this (I think) should always be 17 bytes (as this is the tape header)            
            byte[] blockLengthData = new byte[2];

            // we are now going to stream through the entire file processing a block at a time
            while (stream.Position < stream.Length)
            {
                // read and calculate the length of the block
                stream.Read(blockLengthData, 0, 2);
                int blockSize = BitConverter.ToUInt16(blockLengthData, 0);
                if (blockSize == 0)
                {
                    // block size is 0 - this is probably invalid (but I guess could be EoF in some situations)
                    break;
                }

                // copy the entire block into a new bytearray
                byte[] blockdata = new byte[blockSize];
                stream.Read(blockdata, 0, blockSize);

                // create and populate a new tapedatablock object
                TapeDataBlock tdb = new TapeDataBlock();

                // ascertain the block description
                string description = string.Empty;
                byte crc = 0;
                byte crcValue = 0;
                byte crcFile = 0;
                byte[] programData = new byte[10];

                // calculate block checksum value
                for (int i = 0; i < blockSize; i++)
                {
                    crc ^= blockdata[i];
                    if (i < blockSize - 1)
                    {
                        crcValue = crc;
                    }
                    else
                    {
                        crcFile = blockdata[i];
                    }
                }

                // process the type byte
                /*  (The type is 0,1,2 or 3 for a Program, Number array, Character array or Code file. 
                    A SCREEN$ file is regarded as a Code file with start address 16384 and length 6912 decimal. 
                    If the file is a Program file, parameter 1 holds the autostart line number (or a number >=32768 if no LINE parameter was given) 
                    and parameter 2 holds the start of the variable area relative to the start of the program. If it's a Code file, parameter 1 holds 
                    the start of the code block when saved, and parameter 2 holds 32768. For data files finally, the byte at position 14 decimal holds the variable name.)
                */

                tdb.MetaData = new Dictionary<BlockDescriptorTitle, string>();

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
                    tdb.AddMetaData(BlockDescriptorTitle.Data_Bytes, (blockSize - 2).ToString() + " Bytes");
                }
                else
                {
                    // some other type (turbo data etc..)
                    description = string.Format("#{0} block, {1} bytes", blockdata[0].ToString("X2"), blockSize);
                    //description += string.Format(", crc {0}", ((crc != 0) ? string.Format("bad (#{0:X2}!=#{1:X2})", crcFile, crcValue) : "ok"));
                    tdb.AddMetaData(BlockDescriptorTitle.Undefined, description);
                }
                /*
                if (blockdata[0] == 0x00 && blockSize == 19 && (blockdata[1] == 0x00) || blockdata[1] == 3)
                {
                    // This is the PROGRAM header
                    // take the 10 filename bytes (that start at offset 2)
                    programData = blockdata.Skip(2).Take(10).ToArray();

                    // get the filename as a string (with padding removed)
                    string fileName = Encoding.ASCII.GetString(programData).Trim();

                    // get the type
                    string type = "";
                    if (blockdata[0] == 0x00)
                    {
                        type = "Program";
                    }
                    else
                    {
                        type = "Bytes";
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
                else if (blockdata[0] == 0xFF)
                {
                    // this is a data block
                    description = "Data Block " + (blockSize - 2) + "bytes";
                }
                else
                {
                    // other type
                    description = string.Format("#{0} block, {1} bytes", blockdata[0].ToString("X2"), blockSize - 2);
                    description += string.Format(", crc {0}", ((crc != 0) ? string.Format("bad (#{0:X2}!=#{1:X2})", crcFile, crcValue) : "ok"));
                }
                */

                tdb.BlockDescription = BlockType.Standard_Speed_Data_Block;

                // calculate the data periods for this block
                int pilotLength = 0;

                // work out pilot length
                if (blockdata[0] < 4)
                {
                    pilotLength = 8064;
                }
                else
                {
                    pilotLength = 3220;
                }

                // create a list to hold the data periods
                List<int> dataPeriods = new List<int>();

                // generate pilot pulses
                for (int i = 0; i < pilotLength; i++)
                {
                    dataPeriods.Add(PILOT_PL);
                }

                // add syncro pulses
                dataPeriods.Add(SYNC_1_PL);
                dataPeriods.Add(SYNC_2_PL);

                int pos = 0;

                // add bit0 and bit1 periods
                for (int i = 0; i < blockSize - 1; i++, pos++)
                {
                    for (byte b = 0x80; b != 0; b >>= 1)
                    {
                        if ((blockdata[i] & b) != 0)
                            dataPeriods.Add(BIT_1_PL);
                        else
                            dataPeriods.Add(BIT_0_PL);
                        if ((blockdata[i] & b) != 0)
                            dataPeriods.Add(BIT_1_PL);
                        else
                            dataPeriods.Add(BIT_0_PL);
                    }
                }

                // add the last byte
                for (byte c = 0x80; c != (byte)(0x80 >> BIT_COUNT_IN_LAST); c >>= 1)
                {
                    if ((blockdata[pos] & c) != 0)
                        dataPeriods.Add(BIT_1_PL);
                    else
                        dataPeriods.Add(BIT_0_PL);
                    if ((blockdata[pos] & c) != 0)
                        dataPeriods.Add(BIT_1_PL);
                    else
                        dataPeriods.Add(BIT_0_PL);
                }

                // add block pause
                int actualPause = PAUSE_MS * 1000;
                //dataPeriods.Add(actualPause);

                // default pause for tap files
                tdb.PauseInMS = 1000;

                // add to the tapedatablock object
                tdb.DataPeriods = dataPeriods;

                // add the raw data
                tdb.BlockData = blockdata;

                // generate separate PAUS block
                TapeDataBlock tdbPause = new TapeDataBlock();
                tdbPause.DataPeriods = new List<int>();
                tdbPause.BlockDescription = BlockType.PAUSE_BLOCK;
                tdbPause.PauseInMS = 0;
                var pauseInTStates = TranslatePause(tdb.PauseInMS);
                //if (pauseInTStates > 0)
                    //tdbPause.DataPeriods.Add(pauseInTStates);
                tdb.PauseInMS = 0;

                // add block to the tape
                _datacorder.DataBlocks.Add(tdb);

                // PAUS block if neccessary
                if (pauseInTStates > 0)
                {
                    tdbPause.AddMetaData(BlockDescriptorTitle.Block_ID, pauseInTStates.ToString() + " cycles");

                    int by1000 = pauseInTStates / 70000;
                    int rem1000 = pauseInTStates % 70000;

                    if (by1000 > 1)
                    {
                        tdbPause.DataPeriods.Add(35000);
                        tdbPause.DataPeriods.Add(pauseInTStates - 35000);
                    }
                    else
                    {
                        tdbPause.DataPeriods.Add(pauseInTStates);
                        tdbPause.DataPeriods.Add(0);
                    }

                    _datacorder.DataBlocks.Add(tdbPause);
                }  
            }
        }
    }
}
