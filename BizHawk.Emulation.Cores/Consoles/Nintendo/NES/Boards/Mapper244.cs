using System.Collections.Generic;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public class Mapper244 : NES.NESBoardBase
	{
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER244":
					break;
				default:
					return false;
			}

			return true;
		}

		private List<List<byte>> prg_perm = new List<List<byte>>
		{
			new List<byte> { 0, 1, 2, 3, },
			new List<byte> { 3, 2, 1, 0, },
			new List<byte> { 0, 2, 1, 3, },
			new List<byte> { 3, 1, 2, 0, },
		};

		private List<List<byte>> chr_perm = new List<List<byte>>
		{
			new List<byte> { 0, 1, 2, 3, 4, 5, 6, 7, },
			new List<byte> { 0, 2, 1, 3, 4, 6, 5, 7, },
			new List<byte> { 0, 1, 4, 5, 2, 3, 6, 7, },
			new List<byte> { 0, 4, 1, 5, 2, 6, 3, 7, },
			new List<byte> { 0, 4, 2, 6, 1, 5, 3, 7, },
			new List<byte> { 0, 2, 4, 6, 1, 3, 5, 7, },
			new List<byte> { 7, 6, 5, 4, 3, 2, 1, 0, },
			new List<byte> { 7, 6, 5, 4, 3, 2, 1, 0, }
		};

		private int _chrRegister = 0;
		private int _prgRegister = 0;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chrRegister", ref _chrRegister);
			ser.Sync("prgRegister", ref _prgRegister);
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VROM[(_chrRegister * 0x2000) + (addr & 0x1FFF)];
			}

			return base.ReadPPU(addr);
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[(_prgRegister * 0x8000) + (addr & 0x7FFF)];
		}

		public override void WritePRG(int addr, byte value)
		{
			if ((value & 0x08) > 0)
			{
				_chrRegister = chr_perm[(value >> 4) & 7][value & 7];
			}
			else
			{
				_prgRegister = prg_perm[(value >> 4) & 3][value & 3];
			}
		}
	}
}
