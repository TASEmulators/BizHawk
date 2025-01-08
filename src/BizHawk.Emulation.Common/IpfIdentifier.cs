using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Common.StringExtensions;


namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// Even though we don't currently support IPF files, it makes sense for the future that we can identify them
	/// (or more precisely, the core that we need to pass them too if an entry is not present in the gamedb)
	/// The IPF INFO record does contain a platform entry that assists in this.
	/// </summary>
	public class IpfIdentifier
	{
		/// <summary>
		/// Default fallthrough to Amiga
		/// </summary>
		public string IdentifiedSystem { get; set; } = VSystemID.Raw.Amiga;

		private readonly byte[] _data;

		public IpfIdentifier(byte[] imageData)
		{
			_data = imageData;
			ParseIpfImage();
		}

		private void ParseIpfImage()
		{
			// look for standard magic string
			string ident = Encoding.ASCII.GetString(_data, 0, 16);

			if (!ident.Contains("CAPS", StringComparison.OrdinalIgnoreCase))
			{
				// incorrect format
				return;
			}

			int pos = 0;

			List<IPFBlock> blocks = new List<IPFBlock>();

			while (pos < _data.Length)
			{
				try
				{
					var block = IPFBlock.ParseNextBlock(ref pos, _data, blocks);

					if (block == null)
					{
						// EOF
						break;
					}

					if (block.RecordType == RecordHeaderType.INFO)
					{
						blocks.Add(block);
						break;
					}
				}
				catch (Exception)
				{
					// fallthrough
					return;
				}
			}

			// process the INFO block
			var infoBlock = blocks.Find(static a => a.RecordType == RecordHeaderType.INFO);

			if (infoBlock != null)
			{
				// platform records consist of an array of 4 byte integers
				// this is because an image can potentially run on multiple platforms
				// for now, just take the first bizhawk supported platform we find

				bool found = false;

				switch (infoBlock.INFOplatform1)
				{
					case 1:
						IdentifiedSystem = VSystemID.Raw.Amiga;
						found = true;
						break;
					case 4:
						IdentifiedSystem = VSystemID.Raw.AmstradCPC;
						found = true;
						break;
					case 5:
						IdentifiedSystem = VSystemID.Raw.ZXSpectrum;
						found = true;
						break;
					case 8:
						IdentifiedSystem = VSystemID.Raw.C64;
						found = true;
						break;

					case 2:     // Atari ST
					case 3:		// PC
					case 6:		// Sam Coupe
					case 7:		// Archimedes
					case 9:     // Atari 8-bit
					case 0:     // None
					default:	// Unknown
						break;
				}

				if (found)
				{
					return;
				}

				switch (infoBlock.INFOplatform2)
				{
					case 1:
						IdentifiedSystem = VSystemID.Raw.Amiga;
						found = true;
						break;
					case 4:
						IdentifiedSystem = VSystemID.Raw.AmstradCPC;
						found = true;
						break;
					case 5:
						IdentifiedSystem = VSystemID.Raw.ZXSpectrum;
						found = true;
						break;
					case 8:
						IdentifiedSystem = VSystemID.Raw.C64;
						found = true;
						break;

					case 2:     // Atari ST
					case 3:     // PC
					case 6:     // Sam Coupe
					case 7:     // Archimedes
					case 9:     // Atari 8-bit
					case 0:     // None
					default:    // Unknown
						break;
				}

				if (found)
				{
					return;
				}

				switch (infoBlock.INFOplatform3)
				{
					case 1:
						IdentifiedSystem = VSystemID.Raw.Amiga;
						found = true;
						break;
					case 4:
						IdentifiedSystem = VSystemID.Raw.AmstradCPC;
						found = true;
						break;
					case 5:
						IdentifiedSystem = VSystemID.Raw.ZXSpectrum;
						found = true;
						break;
					case 8:
						IdentifiedSystem = VSystemID.Raw.C64;
						found = true;
						break;

					case 2:     // Atari ST
					case 3:     // PC
					case 6:     // Sam Coupe
					case 7:     // Archimedes
					case 9:     // Atari 8-bit
					case 0:     // None
					default:    // Unknown
						break;
				}

				if (found)
				{
					return;
				}

				switch (infoBlock.INFOplatform4)
				{
					case 1:
						IdentifiedSystem = VSystemID.Raw.Amiga;
						found = true;
						break;
					case 4:
						IdentifiedSystem = VSystemID.Raw.AmstradCPC;
						found = true;
						break;
					case 5:
						IdentifiedSystem = VSystemID.Raw.ZXSpectrum;
						found = true;
						break;
					case 8:
						IdentifiedSystem = VSystemID.Raw.C64;
						found = true;
						break;

					case 2:     // Atari ST
					case 3:     // PC
					case 6:     // Sam Coupe
					case 7:     // Archimedes
					case 9:     // Atari 8-bit
					case 0:     // None
					default:    // Unknown
						break;
				}
			}
		}

		

		/// <summary>
		/// Returns an int32 from a byte array based on offset (in BIG ENDIAN format)
		/// </summary>
		public static int GetBEInt32(byte[] buf, int offsetIndex)
		{
			byte[] b = new byte[4];
			Array.Copy(buf, offsetIndex, b, 0, 4);
			byte[] buffer = b.Reverse().ToArray();
			int pos = 0;
			return buffer[pos++] | buffer[pos++] << 8 | buffer[pos++] << 16 | buffer[pos++] << 24;
		}

		public class IPFBlock
		{
			public RecordHeaderType RecordType;
			public int BlockLength;
			public int CRC;
			public byte[]? RawBlockData;
			public int StartPos;

			public int INFOmediaType;
			public int INFOencoderType;
			public int INFOencoderRev;
			public int INFOfileKey;
			public int INFOfileRev;
			public int INFOorigin;
			public int INFOminTrack;
			public int INFOmaxTrack;
			public int INFOminSide;
			public int INFOmaxSide;
			public int INFOcreationDate;
			public int INFOcreationTime;
			public int INFOplatform1;
			public int INFOplatform2;
			public int INFOplatform3;
			public int INFOplatform4;
			public int INFOdiskNumber;
			public int INFOcreatorId;

			public int IMGEtrack;
			public int IMGEside;
			public int IMGEdensity;
			public int IMGEsignalType;
			public int IMGEtrackBytes;
			public int IMGEstartBytePos;
			public int IMGEstartBitPos;
			public int IMGEdataBits;
			public int IMGEgapBits;
			public int IMGEtrackBits;
			public int IMGEblockCount;
			public int IMGEencoderProcess;
			public int IMGEtrackFlags;
			public int IMGEdataKey;

			public int DATAlength;
			public int DATAbitSize;
			public int DATAcrc;
			public int DATAdataKey;
			public byte[]? DATAextraDataRaw;

			public static IPFBlock? ParseNextBlock(ref int startPos, byte[] data, List<IPFBlock> blockCollection)
			{
				IPFBlock ipf = new IPFBlock();
				ipf.StartPos = startPos;

				if (startPos >= data.Length)
				{
					// EOF
					return null;
				}

				// assume the startPos passed in is actually the start of a new block
				// look for record header ident
				string ident = Encoding.ASCII.GetString(data, startPos, 4);
				startPos += 4;
				try
				{
					ipf.RecordType = (RecordHeaderType) Enum.Parse(typeof(RecordHeaderType), ident);
				}
				catch
				{
					ipf.RecordType = RecordHeaderType.None;
				}

				// setup for actual block size
				ipf.BlockLength = GetBEInt32(data, startPos); startPos += 4;
				ipf.CRC = GetBEInt32(data, startPos); startPos += 4;
				ipf.RawBlockData = new byte[ipf.BlockLength];
				Array.Copy(data, ipf.StartPos, ipf.RawBlockData, 0, ipf.BlockLength);

				switch (ipf.RecordType)
				{
					// Nothing to process / unknown
					// just move ahead
					case RecordHeaderType.CAPS:
					case RecordHeaderType.TRCK:
					case RecordHeaderType.DUMP:
					case RecordHeaderType.CTEI:
					case RecordHeaderType.CTEX:
					default:
						startPos = ipf.StartPos + ipf.BlockLength;
						break;

					// INFO block
					case RecordHeaderType.INFO:
						// INFO header is followed immediately by an INFO block
						ipf.INFOmediaType = GetBEInt32(data, startPos); startPos += 4;
						ipf.INFOencoderType = GetBEInt32(data, startPos); startPos += 4;
						ipf.INFOencoderRev = GetBEInt32(data, startPos); startPos += 4;
						ipf.INFOfileKey = GetBEInt32(data, startPos); startPos += 4;
						ipf.INFOfileRev = GetBEInt32(data, startPos); startPos += 4;
						ipf.INFOorigin = GetBEInt32(data, startPos); startPos += 4;
						ipf.INFOminTrack = GetBEInt32(data, startPos); startPos += 4;
						ipf.INFOmaxTrack = GetBEInt32(data, startPos); startPos += 4;
						ipf.INFOminSide = GetBEInt32(data, startPos); startPos += 4;
						ipf.INFOmaxSide = GetBEInt32(data, startPos); startPos += 4;
						ipf.INFOcreationDate = GetBEInt32(data, startPos); startPos += 4;
						ipf.INFOcreationTime = GetBEInt32(data, startPos); startPos += 4;
						ipf.INFOplatform1 = GetBEInt32(data, startPos); startPos += 4;
						ipf.INFOplatform2 = GetBEInt32(data, startPos); startPos += 4;
						ipf.INFOplatform3 = GetBEInt32(data, startPos); startPos += 4;
						ipf.INFOplatform4 = GetBEInt32(data, startPos); startPos += 4;
						ipf.INFOdiskNumber = GetBEInt32(data, startPos); startPos += 4;
						ipf.INFOcreatorId = GetBEInt32(data, startPos); startPos += 4;
						startPos += 12; // reserved
						break;

					case RecordHeaderType.IMGE:
						ipf.IMGEtrack = GetBEInt32(data, startPos); startPos += 4;
						ipf.IMGEside = GetBEInt32(data, startPos); startPos += 4;
						ipf.IMGEdensity = GetBEInt32(data, startPos); startPos += 4;
						ipf.IMGEsignalType = GetBEInt32(data, startPos); startPos += 4;
						ipf.IMGEtrackBytes = GetBEInt32(data, startPos); startPos += 4;
						ipf.IMGEstartBytePos = GetBEInt32(data, startPos); startPos += 4;
						ipf.IMGEstartBitPos = GetBEInt32(data, startPos); startPos += 4;
						ipf.IMGEdataBits = GetBEInt32(data, startPos); startPos += 4;
						ipf.IMGEgapBits = GetBEInt32(data, startPos); startPos += 4;
						ipf.IMGEtrackBits = GetBEInt32(data, startPos); startPos += 4;
						ipf.IMGEblockCount = GetBEInt32(data, startPos); startPos += 4;
						ipf.IMGEencoderProcess = GetBEInt32(data, startPos); startPos += 4;
						ipf.IMGEtrackFlags = GetBEInt32(data, startPos); startPos += 4;
						ipf.IMGEdataKey = GetBEInt32(data, startPos); startPos += 4;
						startPos += 12; // reserved
						break;

					case RecordHeaderType.DATA:
						ipf.DATAlength = GetBEInt32(data, startPos);
						if (ipf.DATAlength == 0)
						{
							ipf.DATAextraDataRaw = Array.Empty<byte>();
							ipf.DATAlength = 0;
						}
						else
						{
							ipf.DATAextraDataRaw = new byte[ipf.DATAlength];
						}
						startPos += 4;
						ipf.DATAbitSize = GetBEInt32(data, startPos); startPos += 4;
						ipf.DATAcrc = GetBEInt32(data, startPos); startPos += 4;
						ipf.DATAdataKey = GetBEInt32(data, startPos); startPos += 4;

						if (ipf.DATAlength != 0)
						{
							Array.Copy(data, startPos, ipf.DATAextraDataRaw, 0, ipf.DATAlength);
						}

						startPos += ipf.DATAlength;
						break;
				}

				return ipf;
			}
		}

		public enum RecordHeaderType
		{
			None,
			CAPS,
			DUMP,
			DATA,
			TRCK,
			INFO,
			IMGE,
			CTEI,
			CTEX,
		}
	}
}
