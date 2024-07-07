using System.IO;
using BizHawk.Common.IOExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//http://kevtris.org/nes/nsfspec.txt
	//http://en.wikipedia.org/wiki/NES_Sound_Format
	public class NSFFormat
	{
		public byte[] NSFData;

		public byte Version;
		public byte TotalSongs;

		/// <summary>
		/// 1-indexed. 0 is an invalid value, I guess
		/// </summary>
		public byte StartingSong;

		public ushort LoadAddress;

		public ushort InitAddress;

		public ushort PlayAddress;

		public string SongName;

		public string ArtistName;

		public string CopyrightHolder;

		public ushort SpeedNTSC;

		public byte[] BankswitchInitValues = new byte[8];

		public ushort SpeedPAL;

		public bool IsNTSC;
		
		public bool IsPAL;

		[Flags]
		public enum eExtraChips
		{
			None = 0, VRC6 = 1, VRC7 = 2, FDS = 4, MMC5 = 8, Namco106 = 16, FME7 = 32
		}

		public eExtraChips ExtraChips;

		public void WrapByteArray(byte[] data)
		{
			NSFData = data;

			var ms = new MemoryStream(data);
			var br = new BinaryReader(ms);
			br.BaseStream.Position += 5;
			
			Version = br.ReadByte();
			TotalSongs = br.ReadByte();
			StartingSong = br.ReadByte();
			LoadAddress = br.ReadUInt16();
			InitAddress = br.ReadUInt16();
			PlayAddress = br.ReadUInt16();
			SongName = br.ReadStringFixedUtf8(32);
			ArtistName = br.ReadStringFixedUtf8(32);
			CopyrightHolder = br.ReadStringFixedUtf8(32);
			SpeedNTSC = br.ReadUInt16();
			br.Read(BankswitchInitValues, 0, 8);
			SpeedPAL = br.ReadUInt16();
			byte temp = br.ReadByte();
			if ((temp & 2) != 0) IsNTSC = IsPAL = true;
			else if ((temp & 1) != 0) IsPAL = true; else IsNTSC = true;
			ExtraChips = (eExtraChips)br.ReadByte();
		}
	}
}
