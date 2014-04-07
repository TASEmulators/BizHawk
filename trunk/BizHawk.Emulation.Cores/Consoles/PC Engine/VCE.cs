using System;
using System.Globalization;
using System.IO;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.PCEngine
{
	// HuC6260 Video Color Encoder
	public sealed class VCE
	{
		public ushort VceAddress;
		public ushort[] VceData = new ushort[512];
		public int[] Palette = new int[512];
		public byte CR;

		public int NumberOfScanlines { get { return ((CR & 4) != 0) ? 263 : 262; } }

		public void WriteVCE(int port, byte value)
		{
			port &= 0x07;
			switch (port)
			{
				case 0: // Control Port
					CR = value;
					break;
				case 2: // Address LSB
					VceAddress &= 0xFF00;
					VceAddress |= value;
					break;
				case 3: // Address MSB
					VceAddress &= 0x00FF;
					VceAddress |= (ushort)(value << 8);
					VceAddress &= 0x01FF;
					break;
				case 4: // Data LSB
					VceData[VceAddress] &= 0xFF00;
					VceData[VceAddress] |= value;
					PrecomputePalette(VceAddress);
					break;
				case 5: // Data MSB
					VceData[VceAddress] &= 0x00FF;
					VceData[VceAddress] |= (ushort)(value << 8);
					PrecomputePalette(VceAddress);
					VceAddress++;
					VceAddress &= 0x1FF;
					break;
			}
		}

		public byte ReadVCE(int port)
		{
			port &= 0x07;
			switch (port)
			{
				case 4: // Data LSB
					return (byte)(VceData[VceAddress] & 0xFF);
				case 5: // Data MSB
					byte value = (byte)((VceData[VceAddress] >> 8) | 0xFE);
					VceAddress++;
					VceAddress &= 0x1FF;
					return value;
				default: return 0xFF;
			}
		}

		static readonly byte[] PalConvert = { 0, 36, 72, 109, 145, 182, 218, 255 };

		public void PrecomputePalette(int slot)
		{
			byte r = PalConvert[(VceData[slot] >> 3) & 7];
			byte g = PalConvert[(VceData[slot] >> 6) & 7];
			byte b = PalConvert[VceData[slot] & 7];
			Palette[slot] = Colors.ARGB(r, g, b);
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("VCE");
			ser.Sync("VceAddress", ref VceAddress);
			ser.Sync("CR", ref CR);
			ser.Sync("VceData", ref VceData, false);
			ser.EndSection();

			if (ser.IsReader)
				for (int i = 0; i < VceData.Length; i++)
					PrecomputePalette(i);
		}
	}
}