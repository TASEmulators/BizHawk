using System.Collections.Generic;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	/// <summary>
	/// These are used by SetMirroring() to provide the base class nametable mirroring service.
	/// Apparently, these are not used for internal build configuration logic
	/// </summary>
	internal enum EMirrorType
	{
		Vertical, Horizontal, OneScreenA, OneScreenB
	}

	[NesBoardImpl]
	internal abstract class NesBoardBase : INesBoard
	{
		public virtual void Create(NES nes)
		{
			NES = nes;
		}

		public virtual void NesSoftReset()
		{
		}

		public Dictionary<string, string> InitialRegisterValues { get; set; }

		public abstract bool Configure(EDetectionOrigin origin);
		public virtual void ClockPpu() { }
		public virtual void ClockCpu() { }
		public virtual void AtVsyncNmi() { }

		public CartInfo Cart => NES.cart;
		public NES NES { get; set; }

		//this is set to true when SyncState is called, so that we know the base class SyncState was used
		public bool SyncStateFlag;

		public virtual NES.CDLog_MapResults MapMemory(ushort addr, bool write)
		{
			NES.CDLog_MapResults ret = new NES.CDLog_MapResults();
			ret.Type = NES.CDLog_AddrType.None;

			if (addr < 0x2000)
			{
				ret.Type = NES.CDLog_AddrType.MainRAM;
				ret.Address = addr & 0x7FF;
			}

			return ret;
		}

		public virtual void SyncState(Serializer ser)
		{
			ser.Sync(nameof(_vram), ref _vram, true);
			ser.Sync(nameof(_wram), ref _wram, true);
			for (int i = 0; i < 4; i++) ser.Sync("mirroring" + i, ref _mirroring[i]);
			ser.Sync(nameof(_irqSignal), ref _irqSignal);
			SyncStateFlag = true;
		}

		public virtual void SyncIRQ(bool flag)
		{
			IrqSignal = flag;
		}

		private bool _irqSignal;
		public bool IrqSignal
		{
			get => _irqSignal;
			set => _irqSignal = value;
		}

		private readonly int[] _mirroring = new int[4];
		protected void SetMirroring(int a, int b, int c, int d)
		{
			_mirroring[0] = a;
			_mirroring[1] = b;
			_mirroring[2] = c;
			_mirroring[3] = d;
		}

		protected void ApplyMemoryMapMask(int mask, byte[] map)
		{
			byte byteMask = (byte)mask;
			for (int i = 0; i < map.Length; i++)
			{
				map[i] &= byteMask;
			}
		}

		// make sure you have bank-masked the map 
		protected int ApplyMemoryMap(int blockSizeBits, byte[] map, int addr)
		{
			int bank = addr >> blockSizeBits;
			int ofs = addr & ((1 << blockSizeBits) - 1);
			bank = map[bank];
			addr = (bank << blockSizeBits) | ofs;
			return addr;
		}

		public static EMirrorType CalculateMirrorType(int pad_h, int pad_v)
		{
			if (pad_h == 0)
			{
				return pad_v == 0
					? EMirrorType.OneScreenA
					: EMirrorType.Horizontal;
			}

			if (pad_v == 0)
			{
				return EMirrorType.Vertical;
			}

			return EMirrorType.OneScreenB;
		}

		protected void SetMirrorType(int pad_h, int pad_v)
		{
			SetMirrorType(CalculateMirrorType(pad_h, pad_v));
		}

		public void SetMirrorType(EMirrorType mirrorType)
		{
			switch (mirrorType)
			{
				case EMirrorType.Horizontal: SetMirroring(0, 0, 1, 1); break;
				case EMirrorType.Vertical: SetMirroring(0, 1, 0, 1); break;
				case EMirrorType.OneScreenA: SetMirroring(0, 0, 0, 0); break;
				case EMirrorType.OneScreenB: SetMirroring(1, 1, 1, 1); break;
				default: SetMirroring(-1, -1, -1, -1); break; //crash!
			}
		}

		protected int ApplyMirroring(int addr)
		{
			int block = (addr >> 10) & 3;
			block = _mirroring[block];
			int ofs = addr & 0x3FF;
			return (block << 10) | ofs;
		}

		protected byte HandleNormalPRGConflict(int addr, byte value)
		{
			value &= ReadPrg(addr);

			//Debug.Assert(old_value == value, "Found a test case of bus conflict. please report.");
			//report: pinball quest (J). also: double dare
			return value;
		}

		public virtual byte ReadPrg(int addr) => Rom[addr];

		public virtual void WritePrg(int addr, byte value)
		{
		}

		public virtual void WriteWram(int addr, byte value)
		{
			if (_wram != null)
			{
				_wram[addr & _wramMask] = value;
			}
		}

		private int _wramMask;
		public virtual void PostConfigure()
		{
			_wramMask = (Cart.WramSize * 1024) - 1;
		}

		public virtual byte ReadWram(int addr)
		{
			return _wram?[addr & _wramMask] ?? NES.DB;
		}

		public virtual void WriteExp(int addr, byte value)
		{
		}

		public virtual byte ReadExp(int addr)
		{ 
			return NES.DB;
		}

		public virtual byte ReadReg2xxx(int addr)
		{
			return NES.ppu.ReadReg(addr & 7);
		}

		public virtual byte PeekReg2xxx(int addr)
		{
			return NES.ppu.PeekReg(addr & 7);
		}

		public virtual void WriteReg2xxx(int addr, byte value)
		{
			NES.ppu.WriteReg(addr, value);
		}

		public virtual void WritePpu(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				if (Vram != null)
				{
					Vram[addr] = value;
				}
			}
			else
			{
				NES.CIRAM[ApplyMirroring(addr)] = value;
			}
		}

		public virtual void AddressPpu(int addr)
		{
		}

		public virtual byte PeekPPU(int addr) => ReadPpu(addr);

		protected virtual byte ReadPPUChr(int addr)
		{
			return Vrom?[addr] ?? Vram[addr];
		}

		public virtual byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				return Vrom?[addr] ?? Vram[addr];
			}

			return NES.CIRAM[ApplyMirroring(addr)];
		}

		/// <summary>
		/// derived classes should override this if they have peek-unsafe logic
		/// </summary>
		public virtual byte PeekCart(int addr)
		{
			byte ret;
			if (addr >= 0x8000)
			{
				ret = ReadPrg(addr - 0x8000); // easy optimization, since rom reads are so common, move this up (reordering the rest of these else ifs is not easy)
			}
			else if (addr < 0x6000)
			{
				ret = ReadExp(addr - 0x4000);
			}
			else
			{
				ret = ReadWram(addr - 0x6000);
			}

			return ret;
		}

		public virtual byte[] SaveRam => Cart.WramBattery ? Wram : null;

		public byte[] Wram
		{
			get => _wram;
			set => _wram = value;
		}
		public byte[] Vram
		{
			get => _vram;
			set => _vram = value;
		}
		public byte[] Rom { get; set; }
		public byte[] Vrom { get; set; }

		private byte[] _wram, _vram;

		protected void Assert(bool test, string comment, params object[] args)
		{
			if (!test) throw new Exception(string.Format(comment, args));
		}

		protected void Assert(bool test)
		{
			if (!test) throw new Exception("assertion failed in board setup!");
		}

		protected void AssertPrg(params int[] prg) => AssertMemType(Cart.PrgSize, "prg", prg);
		protected void AssertChr(params int[] chr) => AssertMemType(Cart.ChrSize, "chr", chr);
		protected void AssertWram(params int[] wram) => AssertMemType(Cart.WramSize, "wram", wram);
		protected void AssertVram(params int[] vram) => AssertMemType(Cart.VramSize, "vram", vram);

		protected void AssertMemType(int value, string name, int[] valid)
		{
			// only disable vram and wram asserts, as UNIF knows its prg and chr sizes
			if (DisableConfigAsserts && (name == "wram" || name == "vram")) return;
			foreach (int i in valid) if (value == i) return;
			Assert(false, "unhandled {0} size of {1}", name,value);
		}

		protected void AssertBattery(bool hasBattery) => Assert(Cart.WramBattery == hasBattery);

		public virtual void ApplyCustomAudio(short[] samples)
		{
		}

		public bool DisableConfigAsserts { get; set; }
	}
}
