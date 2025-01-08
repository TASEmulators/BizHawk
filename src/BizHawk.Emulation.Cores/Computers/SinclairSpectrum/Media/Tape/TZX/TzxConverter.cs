using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// Reponsible for TZX format serializaton
	/// </summary>
	public sealed class TzxConverter : MediaConverter
	{
		/// <summary>
		/// The type of serializer
		/// </summary>
		private readonly MediaConverterType _formatType = MediaConverterType.TZX;
		public override MediaConverterType FormatType => _formatType;

		/// <summary>
		/// Signs whether this class can be used to read the data format
		/// </summary>
		public override bool IsReader => true;

		/// <summary>
		/// Signs whether this class can be used to write the data format
		/// </summary>
		public override bool IsWriter => false;

		protected override string SelfTypeName
			=> nameof(TzxConverter);

		/// <summary>
		/// Working list of generated tape data blocks
		/// </summary>
		private readonly IList<TapeDataBlock> _blocks = new List<TapeDataBlock>();

		/// <summary>
		/// Position counter
		/// </summary>
		private int _position = 0;

		/// <summary>
		/// Object to keep track of loops - this assumes there is only one loop at a time
		/// </summary>
		private readonly List<KeyValuePair<int, int>> _loopCounter = new List<KeyValuePair<int, int>>();

		/// <summary>
		/// The virtual cassette deck
		/// </summary>
		private readonly DatacorderDevice _datacorder;

		public TzxConverter(DatacorderDevice _tapeDevice)
		{
			_datacorder = _tapeDevice;
		}

		/// <summary>
		/// Returns TRUE if tzx header is detected
		/// </summary>
		public override bool CheckType(byte[] tzxRaw)
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
			string ident = Encoding.ASCII.GetString(tzxRaw, 0, 7);
			// and 'end of text' marker
			byte eotm = tzxRaw[7];

			// version info
			int majorVer = tzxRaw[8];
			int minorVer = tzxRaw[9];

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
		/// The raw TZX data array
		/// </summary>
		private byte[] data;

		/// <summary>
		/// The current BizHawk TapeDataBlock object
		/// </summary>
		private TapeDataBlock t;

		/// <summary>
		/// Block ID of the current TZX block
		/// </summary>
		private int blockId;

		/// <summary>
		/// The length of the data in the current block (in bytes)
		/// </summary>
		private int blockLen;

		/// <summary>
		/// Length of the pilot tone in pulses (half-periods)
		/// </summary>
		private int pilotToneLength;

		/// <summary>
		/// Length of the syncro first pulse
		/// </summary>
		private int sync1PulseLength;

		/// <summary>
		/// Length of the syncro second pulse
		/// </summary>
		private int sync2PulseLength;

		/// <summary>
		/// Length of the zero bit pulse
		/// </summary>
		private int bit0PulseLength;

		/// <summary>
		/// Length of the one bit pulse length
		/// </summary>
		private int bit1PulseLength;

		/// <summary>
		/// Length of pilot tone (number of pulses or half-periods)
		/// </summary>
		private int pilotCount;

		/// <summary>
		/// Used bits in the last byte (MSb first)
		/// </summary>
		private int bitsInLastByte;

		/// <summary>
		/// Pause length (in ms) after this block
		/// </summary>
		private int pauseLen;

		/// <summary>
		/// Represents whether the current signal is HIGH (true) or LOW (false)
		/// </summary>
		private bool signal = false;

		/// <summary>
		/// Remaining bits in current byte to process
		/// </summary>
		private int bitCount;

		/*
		private int At(int tStates)
		{
			double res = 0.98385608 * (double)tStates;
			return (int)res;
		}
		*/			

		/// <summary>
		/// DeSerialization method
		/// </summary>
		public override void Read(byte[] tzxRaw)
		{
			data = tzxRaw;

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
				throw new Exception($"{nameof(TzxConverter)}: This is not a valid TZX format file");
			}

			// iterate through each block
			_position = 10;
			while (_position < data.Length)
			{
				// block ID is the first byte in a new block                
				blockId = data[_position++];

				// process the data
				// this will return leaving us at the start of relevant block data (if there is any)
				ProcessBlock();
			}

			// final pause (some games require this to finish loading properly - eg Kong2)
			pauseLen = 1000;
			t.PauseInTStates = TranslatePause(1000);
			DoPause();

			/* debugging stuff
			StringBuilder export = new StringBuilder();
			foreach (var b in _datacorder.DataBlocks)
			{
				export.Append(b.BlockDescription);
				for (int i = 0; i < b.DataPeriods.Count; i++)
				{
					export.Append("\t\t");
					export.Append(b.PulseDescription[i]);
					export.Append("\t\t");
					export.Append(b.DataPeriods[i].ToString());
					export.Append("\t\t");
					export.AppendLine(b.DataLevels[i].ToString());
				}
			}

			System.IO.File.WriteAllText("c:\\data\\output.txt", export.ToString());
			//string o = export.ToString();
			*/
		}

		/// <summary>
		/// Inverts the audio signal
		/// </summary>
		private void ToggleSignal()
		{
			signal = !signal;
		}

		/// <summary>
		/// Processes a TZX block
		/// </summary>
		private void ProcessBlock()
		{
			// process based on detected block ID
			switch (blockId)
			{
				// ID 10 - Standard Speed Data Block
				case 0x10:
					ProcessBlockID10();
					break;
				// ID 11 - Turbo Speed Data Block
				case 0x11:
					ProcessBlockID11();
					break;
				// ID 12 - Pure Tone
				case 0x12:
					ProcessBlockID12();
					break;
				// ID 13 - Pulse sequence
				case 0x13:
					ProcessBlockID13();
					break;
				// ID 14 - Pure Data Block
				case 0x14:
					ProcessBlockID14();
					break;
				// ID 15 - Direct Recording
				case 0x15:
					ProcessBlockID15();
					break;
				// ID 18 - CSW Recording
				case 0x18:
					ProcessBlockID18();
					break;
				// ID 19 - Generalized Data Block
				case 0x19:
					ProcessBlockID19();
					break;
				// ID 20 - Pause (silence) or 'Stop the Tape' command
				case 0x20:
					ProcessBlockID20();
					break;
				// ID 21 - Group start
				case 0x21:
					ProcessBlockID21();
					break;
				// ID 22 - Group end
				case 0x22:
					ProcessBlockID22();
					break;
				// ID 23 - Jump to block
				case 0x23:
					ProcessBlockID23();
					break;
				// ID 24 - Loop start
				case 0x24:
					ProcessBlockID24();
					break;
				// ID 25 - Loop end
				case 0x25:
					ProcessBlockID25();
					break;
				// ID 26 - Call sequence
				case 0x26:
					ProcessBlockID26();
					break;
				// ID 27 - Return from sequence
				case 0x27:
					ProcessBlockID27();
					break;
				// ID 28 - Select block
				case 0x28:
					ProcessBlockID28();
					break;
				// ID 2A - Stop the tape if in 48K mode
				case 0x2A:
					ProcessBlockID2A();
					break;
				// ID 2B - Set signal level
				case 0x2B:
					ProcessBlockID2B();
					break;
				// ID 30 - Text description
				case 0x30:
					ProcessBlockID30();
					break;
				// ID 31 - Message block
				case 0x31:
					ProcessBlockID31();
					break;
				// ID 32 - Archive info
				case 0x32:
					ProcessBlockID32();
					break;
				// ID 33 - Hardware type
				case 0x33:
					ProcessBlockID33();
					break;
				// ID 35 - Custom info block
				case 0x35:
					ProcessBlockID35();
					break;
				// ID 5A - "Glue" block
				case 0x5A:
					ProcessBlockID5A();
					break;

				// ID 16 - (deprecated) C64 ROM Type Data Block
				case 0x16:
					ProcessBlockID16();
					break;
				// ID 17 - (deprecated) C64 Turbo Tape Data Block
				case 0x17:
					ProcessBlockID17();
					break;
				// ID 34 - (deprecated) Emulation info
				case 0x34:
					ProcessBlockID34();
					break;
				// ID 40 - (deprecated) Snapshot block
				case 0x40:
					ProcessBlockID40();
					break;

				default:
					ProcessUnidentifiedBlock();
					break;
			}
		}

		/// <summary>
		/// 0x10 - Standard Data Block (ROM)
		/// </summary>
		private void ProcessBlockID10()
		{
			/* length: [02,03]+04
		        
			Offset	    Value	Type	    Description
			0x00	    -	    WORD	    Pause after this block (ms.) {1000}
			0x02	    N	    WORD	    Length of data that follow
			0x04	    -	    BYTE[N]	    Data as in .TAP files                   

			This block must be replayed with the standard Spectrum ROM timing values - see the values in 
			curly brackets in block ID 11. The pilot tone consists in 8063 pulses if the first data byte 
			(flag byte) is < 128, 3223 otherwise. This block can be used for the ROM loading routines AND 
			for custom loading routines that use the same timings as ROM ones do. */

			t = new TapeDataBlock
			{
				BlockID = 0x10,
				BlockDescription = BlockType.Standard_Speed_Data_Block
			};

			pauseLen = GetWordValue(data, _position);
			if (pauseLen == 0)
				pauseLen = 1000;

			t.PauseInMS = pauseLen;
			t.PauseInTStates = TranslatePause(pauseLen);

			blockLen = GetWordValue(data, _position + 2);

			_position += 4;

			// get the block data as a new array
			byte[] blockdata = new byte[blockLen];
			blockdata = data.Skip(_position).Take(blockLen).ToArray();

			// pilot count needs to be ascertained from flag byte
			//pilotCount = blockdata[0] < 128 ? 8063 : 3222; // 3223;
			pilotCount = blockdata[0] == 0 ? 8064 : 3219;

			pilotToneLength = 2168; // 2133;// 2168;
			sync1PulseLength = 667; // 632; // 667;
			sync2PulseLength = 735; // 711; // 735;
			bit0PulseLength = 855; // 869; // 855;
			bit1PulseLength = 1710; // 1738; // 1710;
			bitsInLastByte = 8;

			// metadata
			string description = string.Empty;

			if (blockdata[0] == 0x00 && blockLen == 19)
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
				description = "Data Block " + (blockLen - 2) + "bytes";
				t.AddMetaData(BlockDescriptorTitle.Data_Bytes, (blockLen - 2) + " Bytes");
			}
			else
			{
				// some other type (turbo data etc..)
				description = $"#{blockdata[0].ToString("X2")} block, {blockLen} bytes";
				//description += (crc != 0) ? $", crc bad (#{crcFile:X2}!=#{crcValue:X2})" : ", crc ok";
				t.AddMetaData(BlockDescriptorTitle.Undefined, description);
			}

			t.AddMetaData(BlockDescriptorTitle.Pilot_Pulse_Length, pilotToneLength + " T-States");
			t.AddMetaData(BlockDescriptorTitle.Pilot_Pulse_Count, pilotCount + " Pulses");
			t.AddMetaData(BlockDescriptorTitle.First_Sync_Length, sync1PulseLength + " T-States");
			t.AddMetaData(BlockDescriptorTitle.Second_Sync_Length, sync2PulseLength + " T-States");
			t.AddMetaData(BlockDescriptorTitle.Zero_Bit_Length, bit0PulseLength + " T-States");
			t.AddMetaData(BlockDescriptorTitle.One_Bit_Length, bit1PulseLength + " T-States");
			t.AddMetaData(BlockDescriptorTitle.Data_Length, blockLen + " Bytes");
			t.AddMetaData(BlockDescriptorTitle.Bits_In_Last_Byte, bitsInLastByte + " Bits");
			t.AddMetaData(BlockDescriptorTitle.Pause_After_Data, t.PauseInMS + " ms / " + t.PauseInTStates + "TStates");
			
			// add the raw data
			t.BlockData = blockdata;

			// decode
			DecodeData();

			// add the block to the datacorder
			_datacorder.DataBlocks.Add(t);

			// ready for next block
			ZeroVars();
		}

		/// <summary>
		/// 0x11 - Custom Data Block (Turbo)
		/// </summary>
		private void ProcessBlockID11()
		{
			/* length: [0F,10,11]+12

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

			t = new TapeDataBlock
			{
				BlockID = 0x11,
				BlockDescription = BlockType.Turbo_Speed_Data_Block
			};

			pilotToneLength = GetWordValue(data, _position);
			sync1PulseLength = GetWordValue(data, _position + 2);
			sync2PulseLength = GetWordValue(data, _position + 4);
			bit0PulseLength = GetWordValue(data, _position + 6);
			bit1PulseLength = GetWordValue(data, _position + 8);
			pilotCount = GetWordValue(data, _position + 10);
			bitsInLastByte = data[_position + 12];
			pauseLen = GetWordValue(data, _position + 13);
			blockLen = 0xFFFFFF & GetInt32(data, _position + 0x0F);

			t.PauseInMS = pauseLen;
			t.PauseInTStates = TranslatePause(pauseLen);

			_position += 0x12;

			byte[] blockdata = new byte[blockLen];
			blockdata = data.Skip(_position).Take(blockLen).ToArray();

			// metadata
			string description = string.Empty;

			if (blockdata[0] == 0x00 && blockLen == 19)
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
				description = "Data Block " + (blockLen - 2) + "bytes";
				t.AddMetaData(BlockDescriptorTitle.Data_Bytes, (blockLen - 2) + " Bytes");
			}
			else
			{
				// some other type (turbo data etc..)
				description = $"#{blockdata[0].ToString("X2")} block, {blockLen} bytes";
				//description += (crc != 0) ? $", crc bad (#{crcFile:X2}!=#{crcValue:X2})" : ", crc ok";
				t.AddMetaData(BlockDescriptorTitle.Undefined, description);
			}

			t.AddMetaData(BlockDescriptorTitle.Pilot_Pulse_Length, pilotToneLength + " T-States");
			t.AddMetaData(BlockDescriptorTitle.Pilot_Pulse_Count, pilotCount + " Pulses");
			t.AddMetaData(BlockDescriptorTitle.First_Sync_Length, sync1PulseLength + " T-States");
			t.AddMetaData(BlockDescriptorTitle.Second_Sync_Length, sync2PulseLength + " T-States");
			t.AddMetaData(BlockDescriptorTitle.Zero_Bit_Length, bit0PulseLength + " T-States");
			t.AddMetaData(BlockDescriptorTitle.One_Bit_Length, bit1PulseLength + " T-States");
			t.AddMetaData(BlockDescriptorTitle.Data_Length, blockLen + " Bytes");
			t.AddMetaData(BlockDescriptorTitle.Bits_In_Last_Byte, bitsInLastByte + " Bits");
			t.AddMetaData(BlockDescriptorTitle.Pause_After_Data, t.PauseInMS + " ms / " + t.PauseInTStates + "TStates");			

			// add the raw data
			t.BlockData = blockdata;

			// decode
			DecodeData();

			// add the block to the datacorder
			_datacorder.DataBlocks.Add(t);

			// ready for next block
			ZeroVars();
		}

		/// <summary>
		/// 0x12 - Pure Tone
		/// </summary>
		private void ProcessBlockID12()
		{
			/* length: 04

			Offset	Value	Type	Description
			0x00	-	    WORD	Length of one pulse in T-states
			0x02	-	    WORD	Number of pulses                                

			This will produce a tone which is basically the same as the pilot tone in the ID 10, ID 11 blocks. You can define how 
			long the pulse is and how many pulses are in the tone. */

			int blockLen = 4;

			t = new TapeDataBlock
			{
				BlockID = 0x12,
				BlockDescription = BlockType.Pure_Tone,
				PauseInMS = 0,
				PauseInTStates = 0
			};

			// get values
			pilotToneLength = GetWordValue(data, _position);
			pilotCount = GetWordValue(data, _position + 2);

			t.AddMetaData(BlockDescriptorTitle.Pulse_Length, pilotToneLength + " T-States");
			t.AddMetaData(BlockDescriptorTitle.Pulse_Count, pilotCount.ToString());

			// build period information
			while (pilotCount > 0)
			{
				ToggleSignal();
				t.DataPeriods.Add(pilotToneLength);				
				t.DataLevels.Add(signal);
				t.PulseDescription.Add("Pilot " + pilotCount);
				pilotCount--;
			}

			// add the block to the datacorder
			_datacorder.DataBlocks.Add(t);			

			// advance the position to the next block
			_position += blockLen;

			// ready for next block
			ZeroVars();
		}

		/// <summary>
		/// 0x13 - Pulse Sequence
		/// </summary>
		private void ProcessBlockID13()
		{
			/* length: [00]*02+01
			 * 
			Offset	Value	Type	Description
			0x00	N	    BYTE	Number of pulses
			0x01	-	    WORD[N]	Pulses' lengths

			This will produce N pulses, each having its own timing. Up to 255 pulses can be stored in this block; this is useful for non-standard 
			sync tones used by some protection schemes. */

			t = new TapeDataBlock
			{
				BlockID = 0x13,
				BlockDescription = BlockType.Pulse_Sequence,
				PauseInMS = 0,
				PauseInTStates = 0
			};

			// get pulse count
			pilotCount = data[_position];
			t.AddMetaData(BlockDescriptorTitle.Pulse_Count, pilotCount.ToString());
			_position++;

			// build period information
			while (pilotCount > 0)
			{
				pilotToneLength = GetWordValue(data, _position);
				t.AddMetaData(BlockDescriptorTitle.Needs_Parsing, "Pulse " + pilotCount + " Length\t" + pilotToneLength + " T-States");
				t.DataPeriods.Add(pilotToneLength);
				ToggleSignal();
				t.DataLevels.Add(signal);
				t.PulseDescription.Add("Pilot " + pilotCount);

				pilotCount--;
				_position += 2;
			}

			// add the block to the datacorder
			_datacorder.DataBlocks.Add(t);

			// ready for next block
			ZeroVars();
		}

		/// <summary>
		/// 0x14 - Pure Data BLock
		/// </summary>
		private void ProcessBlockID14()
		{
			/* length: [07,08,09]+0A

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

			t = new TapeDataBlock
			{
				BlockID = 0x14,
				BlockDescription = BlockType.Pure_Data_Block
			};

			pilotToneLength = 0;
			sync1PulseLength = 0;
			sync2PulseLength = 0;
			pilotCount = 0;

			bit0PulseLength = GetWordValue(data, _position + 0);
			bit1PulseLength = GetWordValue(data, _position + 2);			
			bitsInLastByte = data[_position + 4];
			pauseLen = GetWordValue(data, _position + 5);
			blockLen = 0xFFFFFF & GetInt32(data, _position + 0x07);

			t.PauseInMS = pauseLen;
			t.PauseInTStates = TranslatePause(pauseLen);

			_position += 0x0A;

			byte[] blockdata = new byte[blockLen];
			blockdata = data.Skip(_position).Take(blockLen).ToArray();

			t.AddMetaData(BlockDescriptorTitle.Pilot_Pulse_Length, pilotToneLength + " T-States");
			t.AddMetaData(BlockDescriptorTitle.Pilot_Pulse_Count, pilotCount + " Pulses");
			t.AddMetaData(BlockDescriptorTitle.First_Sync_Length, sync1PulseLength + " T-States");
			t.AddMetaData(BlockDescriptorTitle.Second_Sync_Length, sync2PulseLength + " T-States");
			t.AddMetaData(BlockDescriptorTitle.Zero_Bit_Length, bit0PulseLength + " T-States");
			t.AddMetaData(BlockDescriptorTitle.One_Bit_Length, bit1PulseLength + " T-States");
			t.AddMetaData(BlockDescriptorTitle.Data_Length, blockLen + " Bytes");
			t.AddMetaData(BlockDescriptorTitle.Bits_In_Last_Byte, bitsInLastByte + " Bits");
			t.AddMetaData(BlockDescriptorTitle.Pause_After_Data, t.PauseInMS + " ms / " + t.PauseInTStates + "TStates");			

			// add the raw data
			t.BlockData = blockdata;

			// decode
			DecodeData();

			// add the block to the datacorder
			_datacorder.DataBlocks.Add(t);

			// ready for next block
			ZeroVars();
		}

		/// <summary>
		/// 0x15 - Direct Recording Block
		/// </summary>
		private void ProcessBlockID15()
		{
			/* length: [05,06,07]+08

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

			TapeDataBlock t = new TapeDataBlock
			{
				BlockID = 0x15,
				BlockDescription = BlockType.Direct_Recording
			};			

			int tStatesPerSample = GetWordValue(data, _position);
			pauseLen = GetWordValue(data, _position + 2);
			bitsInLastByte = data[_position + 4];
			blockLen = 0xFFFFFF & GetInt32(data, _position + 0x05);		

			t.PauseInMS = pauseLen;
			t.PauseInTStates = TranslatePause(pauseLen);

			t.AddMetaData(BlockDescriptorTitle.TStatesPerSample, tStatesPerSample + " TStates per sample");
			t.AddMetaData(BlockDescriptorTitle.Data_Length, blockLen + " Bytes (one bit per sample");
			t.AddMetaData(BlockDescriptorTitle.Bits_In_Last_Byte, bitsInLastByte + " Bits (samples)");
			t.AddMetaData(BlockDescriptorTitle.Pause_After_Data, t.PauseInMS + " ms / " + t.PauseInTStates + "TStates");

			// skip to samples data
			_position += 8;

			while (blockLen > 0)
			{
				if (blockLen == 1)
				{
					// last byte - need to look at number of bits
					bitCount = bitsInLastByte;
				}
				else
				{
					// this is a full byte
					bitCount = 8;
				}

				// get the byte to be processed
				var currByte = data[_position++];

				// do the bits
				while (bitCount > 0)
				{
					if ((currByte & 0x80) != 0)
					{
						// high signal
						signal = true;
					}
					else
					{
						// low signal
						signal = false;
					}

					t.DataPeriods.Add(tStatesPerSample);
					t.DataLevels.Add(signal);

					currByte <<= 1;
					bitCount--;
				}

				blockLen--;
				_position++;
			}

			// pause processing
			DoPause();

			// add the block to the datacorder
			_datacorder.DataBlocks.Add(t);

			// ready for next block
			ZeroVars();
		}

		
		/// <summary>
		/// 0x18 - CSW Recording Block
		/// </summary>
		private void ProcessBlockID18()
		{
			/* length: [00,01,02,03]+04

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

			t = new TapeDataBlock
			{
				BlockID = 0x18,
				BlockDescription = BlockType.CSW_Recording
			};

			blockLen = GetInt32(data, _position);
			_position += 4;

			t.PauseInMS = GetWordValue(data, _position);
			t.PauseInTStates = TranslatePause(pauseLen);

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

			// pause processing
			DoPause();

			// add the block to the datacorder
			_datacorder.DataBlocks.Add(t);

			// ready for next block
			ZeroVars();
		}
		
		/// <summary>
		/// Pause Block
		/// </summary>
		private void ProcessBlockID20()
		{
			/* length: 02
			 * 
			Offset	Value	Type	Description
			0x00	-	    WORD	Pause duration (ms.)                 

			This will make a silence (low amplitude level (0)) for a given time in milliseconds. If the value is 0 then the 
			emulator or utility should (in effect) STOP THE TAPE, i.e. should not continue loading until the user or emulator requests it.     */
			t = new TapeDataBlock();
			t.BlockID = 0x20;
			t.DataPeriods = new List<int>();
			t.BlockDescription = BlockType.Pause_or_Stop_the_Tape;

			pauseLen = GetWordValue(data, _position);

			t.PauseInMS = pauseLen;
			t.PauseInTStates = TranslatePause(t.PauseInMS);

			if (pauseLen == 0)
			{
				// issue stop the tape command
				t.Command = TapeCommand.STOP_THE_TAPE;

			}

			// add to tape
			_datacorder.DataBlocks.Add(t);

			// advanced position to next block
			_position += 2;

			// generate PAUSE block
			DoPause();

			// ready for next block
			ZeroVars();
		}
		
		/// <summary>
		/// 0x21 - Group Start
		/// </summary>
		private void ProcessBlockID21()
		{
			/* length: [00]+01
			 * 
			Offset	Value	Type	Description
			0x00	L	BYTE	Length of the group name string
			0x01	-	CHAR[L]	Group name in ASCII format (please keep it under 30 characters long)                

			This block marks the start of a group of blocks which are to be treated as one single (composite) block. 
			This is very handy for tapes that use lots of subblocks like Bleepload (which may well have over 160 custom loading blocks). 
			You can also give the group a name (example 'Bleepload Block 1').
			For each group start block, there must be a group end block. Nesting of groups is not allowed.           */

			t = new TapeDataBlock();
			t.BlockID = 0x21;
			t.DataPeriods = new List<int>();
			t.BlockDescription = BlockType.Group_Start;

			int nameLength = data[_position];
			_position++;

			string name = Encoding.ASCII.GetString(data, _position, nameLength);
			//t.BlockDescription = "[GROUP: " + name + "]";
			t.Command = TapeCommand.BEGIN_GROUP;

			t.PauseInMS = 0;
			t.PauseInTStates = 0;

			// add to tape
			_datacorder.DataBlocks.Add(t);

			// advance to next block 
			_position += nameLength;

			// ready for next block
			ZeroVars();
		}
		
		/// <summary>
		/// 0x22 - Group End
		/// </summary>
		private void ProcessBlockID22()
		{
			/* length: 00              

			This indicates the end of a group. This block has no body.           */

			t = new TapeDataBlock();
			t.BlockID = 0x22;
			t.DataPeriods = new List<int>();
			t.BlockDescription = BlockType.Group_End;
			t.Command = TapeCommand.END_GROUP;

			t.PauseInMS = 0;
			t.PauseInTStates = 0;

			// add to tape
			_datacorder.DataBlocks.Add(t);

			// ready for next block
			ZeroVars();
		}
		
		/// <summary>
		/// 0x24 - Loop Start
		/// </summary>
		private void ProcessBlockID24()
		{
			/* length: 02

			Offset	Value	Type	Description
			0x00	-	    WORD	Number of repetitions (greater than 1)           

			If you have a sequence of identical blocks, or of identical groups of blocks, you can use this block to tell how many times they should 
			be repeated. This block is the same as the FOR statement in BASIC.
			For simplicity reasons don't nest loop blocks!           */

			t = new TapeDataBlock();
			t.BlockID = 0x24;
			t.BlockDescription = BlockType.Loop_Start;

			// loop should start from the next block
			int loopStart = _datacorder.DataBlocks.Count + 1;

			int numberOfRepetitions = GetWordValue(data, _position);

			// update loop counter
			_loopCounter.Add(
				new KeyValuePair<int, int>(
					loopStart,
					numberOfRepetitions));

			// update description
			//t.BlockDescription = "[LOOP START - " + numberOfRepetitions + " times]";

			t.PauseInMS = 0;
			t.PauseInTStates = 0;

			// add to tape
			_datacorder.DataBlocks.Add(t);

			// advance to next block 
			_position += 2;

			// ready for next block
			ZeroVars();
		}
		
		/// <summary>
		/// 0x25 - Loop End
		/// </summary>
		private void ProcessBlockID25()
		{
			/* length: 00    

			This is the same as BASIC's NEXT statement. It means that the utility should jump back to the start of the loop if it hasn't 
			been run for the specified number of times.
			This block has no body.          */

			t = new TapeDataBlock();
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
			int blockCnt = _datacorder.DataBlocks.Count - loopStart;

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

			// ready for next block
			ZeroVars();
		}

		/// <summary>
		/// 0x2A - Stop the Tape if in 48K Mode
		/// </summary>
		private void ProcessBlockID2A()
		{
			/* length: 04
			 * 
			Offset	Value	Type	Description
			0x00	0	    DWORD	Length of the block without these four bytes (0)

			When this block is encountered, the tape will stop ONLY if the machine is an 48K Spectrum. This block is to be used for 
			multiloading games that load one level at a time in 48K mode, but load the entire tape at once if in 128K mode.
			This block has no body of its own, but follows the extension rule.          */

			t = new TapeDataBlock();
			t.BlockID = 0x2A;
			t.DataPeriods = new List<int>();
			t.BlockDescription = BlockType.Stop_the_Tape_48K;
			t.Command = TapeCommand.STOP_THE_TAPE_48K;

			int blockSize = 4 + GetWordValue(data, _position);

			t.PauseInMS = 0;
			t.PauseInTStates = 0;

			// add to tape
			_datacorder.DataBlocks.Add(t);

			// advance to next block 
			_position += blockSize;

			// ready for next block
			ZeroVars();
		}
		
		/// <summary>
		/// 0x2B - Set Signal Level
		/// </summary>
		private void ProcessBlockID2B()
		{
			/* length: 05

			Offset	Value	Type	Description
			0x00	1	    DWORD	Block length (without these four bytes)
			0x04	-	    BYTE	Signal level (0=low, 1=high)

			This block sets the current signal level to the specified value (high or low). It should be used whenever it is necessary to avoid any 
			ambiguities, e.g. with custom loaders which are level-sensitive.         */

			t = new TapeDataBlock();
			t.BlockID = 0x2B;
			t.DataPeriods = new List<int>();
			t.BlockDescription = BlockType.Set_Signal_Level;

			t.PauseInMS = 0;
			t.PauseInTStates = 0;

			// we already flip the signal *before* adding to the buffer elsewhere
			// so set the opposite level specified in this block
			byte signalLev = data[_position + 4];
			if (signalLev == 0)
				signal = true;
			else
				signal = false;

			// add to tape
			_datacorder.DataBlocks.Add(t);

			// advance to next block 
			_position += 5;

			// ready for next block
			ZeroVars();
		}
		
		/// <summary>
		/// Text Description
		/// </summary>
		private void ProcessBlockID30()
		{
			/* length: [00]+01

			Offset	Value	Type	Description
			0x00	N	    BYTE	Length of the text description
			0x01	-	    CHAR[N]	Text description in ASCII format

			This is meant to identify parts of the tape, so you know where level 1 starts, where to rewind to when the game ends, etc. 
			This description is not guaranteed to be shown while the tape is playing, but can be read while browsing the tape or changing 
			the tape pointer.
			The description can be up to 255 characters long but please keep it down to about 30 so the programs can show it in one line 
			(where this is appropriate).
			Please use 'Archive Info' block for title, authors, publisher, etc.        */

			t = new TapeDataBlock();
			t.BlockID = 0x30;
			t.DataPeriods = new List<int>();
			t.BlockDescription = BlockType.Text_Description;

			int textLen = data[_position];
			_position++;

			string desc = Encoding.ASCII.GetString(data, _position, textLen);

			t.PauseInMS = 0;
			t.PauseInTStates = 0;

			t.AddMetaData(BlockDescriptorTitle.Text_Description, desc);

			// add to tape
			_datacorder.DataBlocks.Add(t);

			// advance to next block 
			_position += textLen;

			// ready for next block
			ZeroVars();
		}
		
		/// <summary>
		/// 0x32 - Archive Info
		/// </summary>
		private void ProcessBlockID32()
		{
			/* length: [00,01]+02

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

			t = new TapeDataBlock();
			t.BlockID = 0x32;
			t.BlockDescription = BlockType.Archive_Info;

			blockLen = GetWordValue(data, _position);
			_position += 2;
			int stringCount = data[_position++];

			// iterate through each string
			for (int s = 0; s < stringCount; s++)
			{
				// identify the type of text
				int type = data[_position++];

				// get text length
				int strLen = data[_position++];

				string title = string.Empty;
				title = "Info: ";

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

				// get string data
				string val = Encoding.ASCII.GetString(data, _position, strLen);
				t.AddMetaData(BlockDescriptorTitle.Archive_Info, val);

				t.PauseInMS = 0;
				t.PauseInTStates = 0;

				// advance to next string block
				_position += strLen;				
			}

			// add to tape
			_datacorder.DataBlocks.Add(t);

			// ready for next block
			ZeroVars();
		}
		
		/// <summary>
		/// 0x35 - Custom Info Block
		/// </summary>
		private void ProcessBlockID35()
		{
			/* length: [10,11,12,13]+14

			Offset	Value	Type	Description
			0x00	-	    CHAR[10]	Identification string (in ASCII)
			0x10	L	    DWORD	Length of the custom info
			0x14	-	    BYTE[L]	Custom info                                

			This block can be used to save any information you want. For example, it might contain some information written by a utility, 
			extra settings required by a particular emulator, or even poke data.               */

			t = new TapeDataBlock();
			t.BlockID = 0x35;
			t.BlockDescription = BlockType.Custom_Info_Block;

			t.PauseInMS = 0;
			t.PauseInTStates = 0;

			string info = Encoding.ASCII.GetString(data, _position, 0x10);
			t.AddMetaData(BlockDescriptorTitle.Custom_Info, info);
			_position += 0x10;

			int blockLen = BitConverter.ToInt32(data, _position);
			_position += 4;

			// add to tape
			_datacorder.DataBlocks.Add(t);

			// advance to next block 
			_position += blockLen;

			// ready for next block
			ZeroVars();
		}

		/// <summary>
		/// 0x5A - Glue Block
		/// </summary>
		private void ProcessBlockID5A()
		{
			/* length: 09

			Offset	Value	Type	Description
			0x00	-	    BYTE[9]	Value: { "XTape!",0x1A,MajR,MinR } 
									Just skip these 9 bytes and you will end up on the next ID.                                

			This block is generated when you merge two ZX Tape files together. It is here so that you can easily copy the files together and use 
			them. Of course, this means that resulting file would be 10 bytes longer than if this block was not used. All you have to do 
			if you encounter this block ID is to skip next 9 bytes.
			If you can avoid using this block for this purpose, then do so; it is preferable to use a utility to join the two files and 
			ensure that they are both of the higher version number.               */			

			t = new TapeDataBlock();
			t.BlockID = 0x5A;
			t.DataPeriods = new List<int>();
			t.BlockDescription = BlockType.Glue_Block;

			t.PauseInMS = 0;

			// add to tape
			_datacorder.DataBlocks.Add(t);

			// advance to next block 
			_position += 9;

			// ready for next block
			ZeroVars();
		}

		/// <summary>
		/// Skips an unknown block
		/// </summary>
		private void ProcessUnidentifiedBlock()
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

#if true // Not Implemented Yet

		/// <summary>
		/// 0x33 - Hardware Type
		/// </summary>
		private void ProcessBlockID33()
		{
			/* length: [00]*03+01

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

			// currently not implemented properly in ZXHawk

			t = new TapeDataBlock();
			t.BlockID = 0x33;
			t.DataPeriods = new List<int>();
			t.BlockDescription = BlockType.Hardware_Type;

			t.PauseInMS = 0;
			t.PauseInTStates = 0;

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

			// ready for next block
			ZeroVars();
		}

		/// <summary>
		/// 0x31 - Message Block
		/// </summary>
		private void ProcessBlockID31()
		{
			/* length: [01]+02

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

			// currently not implemented properly in ZXHawk

			t = new TapeDataBlock();
			t.BlockID = 0x31;
			t.DataPeriods = new List<int>();
			t.BlockDescription = BlockType.Message_Block;

			_position++;

			int msgLen = data[_position];
			_position++;

			string desc = Encoding.ASCII.GetString(data, _position, msgLen);

			t.Command = TapeCommand.SHOW_MESSAGE;

			t.PauseInMS = 0;

			// add to tape
			_datacorder.DataBlocks.Add(t);

			// advance to next block 
			_position += msgLen;

			// ready for next block
			ZeroVars();
		}

		/// <summary>
		/// 0x26 - Call Sequence
		/// </summary>
		private void ProcessBlockID26()
		{
			/* length: [00,01]*02+02

			Offset	Value	Type	Description
			0x00	N	    WORD	Number of calls to be made
			0x02	-	    WORD[N]	Array of call block numbers (relative-signed offsets)    

			This block is an analogue of the CALL Subroutine statement. It basically executes a sequence of blocks that are somewhere else and 
			then goes back to the next block. Because more than one call can be normally used you can include a list of sequences to be called. 
			The 'nesting' of call blocks is also not allowed for the simplicity reasons. You can, of course, use the CALL blocks in the LOOP 
			sequences and vice versa. The value is relative for the obvious reasons - so that you can add some blocks in the beginning of the 
			file without disturbing the call values. Please take a look at 'Jump To Block' for reference on the values.          */

			// block processing not implemented for this - just gets added for informational purposes only
			t = new TapeDataBlock();
			t.BlockID = 0x26;
			t.DataPeriods = new List<int>();
			t.BlockDescription = BlockType.Call_Sequence;

			int blockSize = 2 + 2 * GetWordValue(data, _position);

			t.PauseInMS = 0;
			t.PauseInTStates = 0;

			// add to tape
			_datacorder.DataBlocks.Add(t);

			// advance to next block 
			_position += blockSize;

			// ready for next block
			ZeroVars();
		}

		/// <summary>
		/// 0x27 - Return From Sequence
		/// </summary>
		private void ProcessBlockID27()
		{
			/* length: 00  

			This block indicates the end of the Called Sequence. The next block played will be the block after the last CALL block (or the next Call, 
			if the Call block had multiple calls).
			Again, this block has no body.          */

			// block processing not implemented for this - just gets added for informational purposes only
			t = new TapeDataBlock();
			t.BlockID = 0x27;
			t.BlockDescription = BlockType.Return_From_Sequence;
			t.PauseInMS = 0;
			t.PauseInTStates = 0;

			// add to tape
			_datacorder.DataBlocks.Add(t);

			// ready for next block
			ZeroVars();
		}

		/// <summary>
		/// 0x28 - Select Block
		/// </summary>
		private void ProcessBlockID28()
		{
			/* length: [00,01]+02

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

			// block processing not implemented for this - just gets added for informational purposes only
			t = new TapeDataBlock();
			t.BlockID = 0x28;
			t.DataPeriods = new List<int>();
			t.BlockDescription = BlockType.Select_Block;

			int blockSize = 2 + GetWordValue(data, _position);

			t.PauseInMS = 0;
			t.PauseInTStates = 0;

			// add to tape
			_datacorder.DataBlocks.Add(t);

			// advance to next block 
			_position += blockSize;

			// ready for next block
			ZeroVars();
		}

		/// <summary>
		/// 0x23 - Jump to Block
		/// </summary>
		private void ProcessBlockID23()
		{
			/* length: 02
			 * 
			Offset	Value	Type	Description
			0x00	-	    WORD	Relative jump value              

			This block will enable you to jump from one block to another within the file. The value is a signed short word 
			(usually 'signed short' in C); Some examples:
				Jump 0 = 'Loop Forever' - this should never happen
				Jump 1 = 'Go to the next block' - it is like NOP in assembler ;)
				Jump 2 = 'Skip one block'
				Jump -1 = 'Go to the previous block'
			All blocks are included in the block count!.           */


			// not implemented properly

			t = new TapeDataBlock();
			t.BlockID = 0x23;
			t.DataPeriods = new List<int>();
			t.BlockDescription = BlockType.Jump_to_Block;

			int relativeJumpValue = GetWordValue(data, _position);
			string result = string.Empty;

			switch (relativeJumpValue)
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
			t.PauseInTStates = 0;

			// add to tape
			_datacorder.DataBlocks.Add(t);

			// advance to next block 
			_position += 2;

			// ready for next block
			ZeroVars();
		}

		/// <summary>
		/// 0x19 - Generalized Data Block
		/// </summary>
		private void ProcessBlockID19()
		{
			/*  length: [00,01,02,03]+04
			 *  
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

			// ready for next block
			ZeroVars();
		}


		// These mostly should be ignored by ZXHawk - here for completeness

		private void ProcessBlockID16()
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

		private void ProcessBlockID17()
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

		private void ProcessBlockID34()
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
		private void ProcessBlockID40()
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


#endif

		/// <summary>
		/// Sets up variables for the next block
		/// </summary>
		private void ZeroVars()
		{
			pilotToneLength = 0;
			pilotCount = 0;
			sync1PulseLength = 0;
			sync2PulseLength = 0;
			bit0PulseLength = 0;
			bit1PulseLength = 0;
			bitsInLastByte = 0;
			pauseLen = 0;
			blockLen = 0;
			bitCount = 0;
		}

		/// <summary>
		/// Decode method for standard, turbo and pure data blocks
		/// </summary>
		private void DecodeData()
		{
			// generate pilot tone
			while (pilotCount > 0)
			{
				t.DataPeriods.Add(pilotToneLength);
				ToggleSignal();
				t.DataLevels.Add(signal);
				t.PulseDescription.Add("Pilot " + pilotCount);
				pilotCount--;
			}

			// syncro pulses
			if (sync1PulseLength > 0)
			{
				t.DataPeriods.Add(sync1PulseLength);
				ToggleSignal();
				t.DataLevels.Add(signal);
				t.PulseDescription.Add("Syncro 1");
			}

			if (sync2PulseLength > 0)
			{
				t.DataPeriods.Add(sync2PulseLength);
				ToggleSignal();
				t.DataLevels.Add(signal);
				t.PulseDescription.Add("Syncro 2");
			}

			// generate periods for actual data
			while (blockLen > 0)
			{
				if (blockLen == 1)
				{
					// last byte - need to look at number of bits
					bitCount = bitsInLastByte;
				}
				else
				{
					// this is a full byte
					bitCount = 8;
				}

				// get the byte to be processed
				var currByte = data[_position];

				// do the bits
				int currBitLength = 0;
				while (bitCount > 0)
				{
					if ((currByte & 0x80) != 0)
					{
						// this is a '1'
						currBitLength = bit1PulseLength;
					}
					else
					{
						// this is a '0'
						currBitLength = bit0PulseLength;
					}

					// play two pulses (a whole period) to generate an edge
					t.DataPeriods.Add(currBitLength);
					ToggleSignal();
					t.DataLevels.Add(signal);
					t.PulseDescription.Add("Data Pulse 1/2 (Byte: " + blockLen + " / Bit: " + bitCount + ")");

					t.DataPeriods.Add(currBitLength);
					ToggleSignal();
					t.DataLevels.Add(signal);
					t.PulseDescription.Add("Data Pulse 2/2 (Byte: " + blockLen + " / Bit: " + bitCount + ")");

					currByte <<= 1;
					bitCount--;
				}

				blockLen--;
				_position++;
			}

			// handle pause after block
			DoPause();
		}

		/// <summary>
		/// Generates pause data
		/// </summary>
		private void DoPause()
		{
			if (pauseLen > 0)
			{
				t.DataPeriods.Add(t.PauseInTStates);
				ToggleSignal();
				t.DataLevels.Add(signal);
				t.PulseDescription.Add("Pause after block: " + t.PauseInTStates);
			}
		}
	}
}
