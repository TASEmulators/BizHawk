using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// 46-in-1 (D1038) [p1][U][!]
	// 65-in-1 (NT766) [p1][U][!]
	// 74-in-1 (NT886) [p1][U][!]
	// 77-in-1 (NT141) [p1][U][!]
	public sealed class UNIF_BMC_D1038 : NES.NESBoardBase
	{
		[MapperProp]
		public int D1038_Dipswitch;

		private int _reg;

		private int DipswitchMask = 3;

		private bool Prg16kMode {  get { return _reg.Bit(7);  } }

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_BMC-D1038":
					break;
				default:
					return false;
			}

			AutoMapperProps.Apply(this);

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("reg", ref _reg);
			ser.Sync("D1038_Dipswitch", ref D1038_Dipswitch);
		}

		public override void WritePRG(int addr, byte value)
		{
			_reg = addr;

			SetMirrorType(_reg.Bit(3) ? EMirrorType.Horizontal : EMirrorType.Vertical);
		}

		public override byte ReadPRG(int addr)
		{
			if ((_reg & 0x100) > 0)
			{
				return (byte)(D1038_Dipswitch & DipswitchMask);
			}

			if (Prg16kMode)
			{
				int bank = (_reg >> 4) & 7;
				return ROM[(bank * 0x4000) + (addr & 0x3FFF)];
			}
			else
			{
				int bank = (_reg >> 5) & 3;
				return ROM[(bank * 0x8000) + (addr & 0x7FFF)];
			}
		}

		public override byte ReadPPU(int addr)
		{
			if ((_reg & 0x100) > 0)
			{
				return (byte)D1038_Dipswitch;
			}

			if (addr < 0x2000)
			{
				int bank = _reg & 7;
				return VROM[(bank * 0x2000) + (addr & 0x1FFF)];
			}

			return base.ReadPPU(addr);
		}
	}
}
