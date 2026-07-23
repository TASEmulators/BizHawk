using BizHawk.Common.NumberExtensions;

using System.Collections.Generic;
using System.Text;

using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Cores.Tapes;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// Responsible for Compressed Square Wave conversion
	/// https://web.archive.org/web/20171024182530/http://ramsoft.bbk.org.omegahg.com/csw.html
	/// </summary>
	public sealed class CswConverter : MediaConverter
	{
		/// <summary>
		/// The type of serializer
		/// </summary>
		private readonly MediaConverterType _formatType = MediaConverterType.CSW;
		public override MediaConverterType FormatType => _formatType;

		/// <summary>
		/// Position counter
		/// </summary>
		private int _position = 0;

		/// <summary>
		/// Signs whether this class can be used to read the data format
		/// </summary>
		public override bool IsReader => true;

		/// <summary>
		/// Signs whether this class can be used to write the data format
		/// </summary>
		public override bool IsWriter => false;

		protected override string SelfTypeName
			=> nameof(CswConverter);

		private readonly DatacorderDevice _datacorder;

		public CswConverter(DatacorderDevice _tapeDevice)
		{
			_datacorder = _tapeDevice;
		}

		/// <summary>
		/// Returns TRUE if pzx header is detected
		/// </summary>
		public override bool CheckType(byte[] data)
		{
			// CSW Header. The 22-byte signature is followed by a 0x1A terminator at 0x16. Guard against shorter
			// files first: CheckType runs on every tape during type-detection, so a short file of another
			// format must not throw here.
			if (data == null || data.Length < 23)
				return false;

			if (data[0x16] != 0x1A)
				return false;

			string ident = Encoding.ASCII.GetString(data, 0, 22);
			return "COMPRESSED SQUARE WAVE".EqualsIgnoreCase(ident);
		}

		/// <summary>
		/// DeSerialization method
		/// </summary>
		public override void Read(byte[] data)
		{
			// clear existing tape blocks
			_datacorder.DataBlocks.Clear();

			// CSW Header

			// check whether this is a valid csw format file by looking at the identifier in the header
			// (first 22 bytes of the file)
			string ident = Encoding.ASCII.GetString(data, 0, 22);

			if (!"COMPRESSED SQUARE WAVE".EqualsIgnoreCase(ident))
			{
				// this is not a valid CSW format file
				throw new Exception($"{nameof(CswConverter)}: This is not a valid CSW format file");
			}

			if (data[0x16] != 0x1a)
			{
				// invalid terminator code
				throw new Exception($"{nameof(CswConverter)}: This image reports as a CSW but has an invalid terminator code");
			}

			_position = 0;

			// version info
			int majorVer = data[0x17];
			int minorVer = data[0x18];

			int sampleRate;
			int totalPulses;
			byte compressionType;
			byte flags;
			byte headerExtensionLen;
			byte[] cswData;
			byte[] cswDataUncompressed;

			if (majorVer == 2)
			{
				/*
                    CSW-2 Header
                    CSW global file header - status: required
                    Offset	    Value	Type	Description
                    0x00	    (note)	ASCII[22]	"Compressed Square Wave" signature
                    0x16	    0x1A	BYTE	    Terminator code
                    0x17	    0x02	BYTE	    CSW major revision number
                    0x18	    0x00	BYTE	    CSW minor revision number
                    0x19	    -	    DWORD	    Sample rate
                    0x1D	    -	    DWORD	    Total number of pulses (after decompression)
                    0x21	    -	    BYTE	    Compression type (see notes below)
                                                        0x01: RLE
                                                        0x02: Z-RLE
                    0x22	    -	    BYTE	    Flags
                                                        b0: initial polarity; if set, the signal starts at logical high
                    0x23	    HDR	    BYTE	    Header extension length in bytes (0x00)
                                                        For future expansions only, see note below.
                    0x24	    -	    ASCIIZ[16]	Encoding application description
                                                        Information about the tool which created the file (e.g. name and version)
                    0x34	    -	    BYTE[HDR]	Header extension data (if present)
                    0x34+HDR	-	    -	        CSW data.
                */

				_position = 0x19;
				sampleRate = GetInt32(data, _position);
				_position += 4;

				totalPulses = GetInt32(data, _position);
				cswDataUncompressed = new byte[totalPulses + 1];
				_position += 4;

				compressionType = data[_position++];
				flags = data[_position++];
				headerExtensionLen = data[_position++];

				_position = 0x34 + headerExtensionLen;

				cswData = new byte[data.Length - _position];
				Array.Copy(data, _position, cswData, 0, cswData.Length);

				ProcessCSWV2(cswData, ref cswDataUncompressed, compressionType, totalPulses);
			}
			else if (majorVer == 1)
			{
				/*
                    CSW-1 Header
                    CSW global file header - status: required
                    Offset	Value	Type	    Description
                    0x00	(note)	ASCII[22]	"Compressed Square Wave" signature
                    0x16	0x1A	BYTE	    Terminator code
                    0x17	0x01	BYTE	    CSW major revision number
                    0x18	0x01	BYTE	    CSW minor revision number
                    0x19	-	    WORD	    Sample rate
                    0x1B	0x01	BYTE	    Compression type
                                                    0x01: RLE
                    0x1C	-	    BYTE	    Flags
                                                    b0: initial polarity; if set, the signal starts at logical high
                    0x1D	0x00	BYTE[3]	    Reserved.
                    0x20	-	    -	        CSW data.
                */

				_position = 0x19;
				sampleRate = GetWordValue(data, _position);
				_position += 2;

				compressionType = data[_position++];
				flags = data[_position++];

				_position += 3;

				totalPulses = -1; // v1 has no pulse count; the buffer holds exactly the pulse data
				cswDataUncompressed = new byte[data.Length - _position];

				if (compressionType == 1)
					Array.Copy(data, _position, cswDataUncompressed, 0, cswDataUncompressed.Length);
				else
					throw new Exception($"{nameof(CswConverter)}: CSW Format unknown compression type");
			}
			else
			{
				throw new Exception($"{nameof(CswConverter)}: CSW Format Version {majorVer}.{minorVer} is not currently supported");
			}

			// create the single tape block
			// (use DATA block for now so initial signal level is handled correctly by the datacorder device)
			TapeDataBlock t = new TapeDataBlock();
			t.BlockDescription = BlockType.CSW_Recording;
			t.BlockID = 0x18;
			t.DataPeriods = new List<int>();
			t.DataLevels = new List<bool>();

			if (flags.Bit(0))
				t.InitialPulseLevel = true;
			else
				t.InitialPulseLevel = false;

			bool currLevel = !t.InitialPulseLevel;

			var rate = (69888 * 50) / sampleRate;

			// each pulse is one sample-count byte; a zero byte means the next 4 bytes hold a 32-bit sample
			// count. Period T-states = samples * (T-states per sample). For v2 stop after 'totalPulses' pulses
			// (its buffer is padded by one byte, which would otherwise misfire the extended-pulse path and read
			// past the end); v1 has no count so the whole buffer is consumed.
			int idx = 0;
			int emitted = 0;
			while (idx < cswDataUncompressed.Length && (totalPulses < 0 || emitted < totalPulses))
			{
				int samples = cswDataUncompressed[idx++];
				if (samples == 0 && idx + 4 <= cswDataUncompressed.Length)
				{
					samples = GetInt32(cswDataUncompressed, idx);
					idx += 4;
				}

				t.DataPeriods.Add(samples * rate);
				currLevel = !currLevel;
				t.DataLevels.Add(currLevel);
				emitted++;
			}

			// add closing period
			t.DataPeriods.Add((69888 * 50) / 10);
			currLevel = !currLevel;
			t.DataLevels.Add(currLevel);

			// add to datacorder
			_datacorder.DataBlocks.Add(t);
		}

		/// <summary>
		/// Processes a CSW v2 data block
		/// </summary>
		public static void ProcessCSWV2(
			byte[] srcBuff,
			ref byte[] destBuff,
			byte compType,
			int pulseCount)
		{
			if (compType == 1)
			{
				Array.Copy(srcBuff, 0, destBuff, 0, pulseCount);
			}
			else if (compType == 2)
			{
				DecompressZRLE(srcBuff, ref destBuff);
			}
			else
				throw new Exception("CSW Format unknown compression type");
		}
	}
}
