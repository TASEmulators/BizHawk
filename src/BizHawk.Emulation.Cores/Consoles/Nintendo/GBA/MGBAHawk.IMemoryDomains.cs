using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBAHawk
	{
		private MemoryDomainList _memoryDomains;

		private MemoryDomainIntPtr _iwram;
		private MemoryDomainIntPtr _ewram;
		private MemoryDomainIntPtr _bios;
		private MemoryDomainIntPtr _palram;
		private MemoryDomainIntPtr _vram;
		private MemoryDomainIntPtr _oam;
		private MemoryDomainIntPtr _rom;
		private MemoryDomainIntPtr _sram;
		private MemoryDomainDelegate _cwram;

		private void CreateMemoryDomains(int romsize)
		{
			const MemoryDomain.Endian le = MemoryDomain.Endian.Little;

			var mm = new List<MemoryDomain>
			{
				(_iwram = new MemoryDomainIntPtr("IWRAM", le, IntPtr.Zero, 32 * 1024, true, 4)),
				(_ewram = new MemoryDomainIntPtr("EWRAM", le, IntPtr.Zero, 256 * 1024, true, 4)),
				(_bios = new MemoryDomainIntPtr("BIOS", le, IntPtr.Zero, 16 * 1024, false, 4)),
				(_palram = new MemoryDomainIntPtr("PALRAM", le, IntPtr.Zero, 1024, true, 4)),
				(_vram = new MemoryDomainIntPtr("VRAM", le, IntPtr.Zero, 96 * 1024, true, 4)),
				(_oam = new MemoryDomainIntPtr("OAM", le, IntPtr.Zero, 1024, true, 4)),
				(_rom = new MemoryDomainIntPtr("ROM", le, IntPtr.Zero, romsize, true, 4)),
				// 128 KB is the max size for GBA savedata
				// mGBA does not know a game's save type (and as a result actual savedata size) on startup.
				// Instead, BizHawk's savedata buffer will be accessed directly for a consistent interface.
				(_sram = new MemoryDomainIntPtr("SRAM", le, IntPtr.Zero, 128 * 1024, true, 4)),
				(_cwram = new MemoryDomainDelegate("Combined WRAM", (256 + 32) * 1024, le, null, null, 4)),

				new MemoryDomainDelegate("System Bus", 0x10000000, le,
				addr =>
				{
					var a = (uint)addr;
					if (a >= 0x10000000) throw new ArgumentOutOfRangeException(paramName: nameof(addr), a, message: "address out of range");
					return LibmGBA.BizReadBus(Core, a);
				},
				(addr, val) =>
				{
					var a = (uint)addr;
					if (a >= 0x10000000) throw new ArgumentOutOfRangeException(paramName: nameof(addr), a, message: "address out of range");
					LibmGBA.BizWriteBus(Core, a, val);
				}, 4)
			};

			_memoryDomains = new(mm);
			WireMemoryDomainPointers();
		}

		private void WireMemoryDomainPointers()
		{
			LibmGBA.BizGetMemoryAreas(Core, out var s);

			_iwram.Data = s.iwram;
			_ewram.Data = s.wram;
			_bios.Data = s.bios;
			_palram.Data = s.palram;
			_vram.Data = s.vram;
			_oam.Data = s.oam;
			_rom.Data = s.rom;
			_sram.Data = s.sram;

			// special combined ram memory domain
			const int SIZE_COMBINED = (256 + 32) * 1024;
			_cwram.Peek =
				addr =>
				{
					if (addr is < 0 or >= SIZE_COMBINED) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "invalid address");
					if (addr >= 256 * 1024)
					{
						return PeekWRAM(s.iwram, addr & 32767);
					}

					return PeekWRAM(s.wram, addr);
				};
			_cwram.Poke =
				(addr, val) =>
				{
					if (addr is < 0 or >= SIZE_COMBINED) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "invalid address");
					if (addr >= 256 * 1024)
					{
						PokeWRAM(s.iwram, addr & 32767, val);
					}
					else
					{
						PokeWRAM(s.wram, addr, val);
					}
				};

			_gpumem = new()
			{
				mmio = s.mmio,
				oam = s.oam,
				palram = s.palram,
				vram = s.vram
			};
		}

		private unsafe byte PeekWRAM(IntPtr xwram, long addr)
		{
			return ((byte*)xwram)[addr];
		}

		private unsafe void PokeWRAM(IntPtr xwram, long addr, byte value)
		{
			((byte*)xwram)[addr] = value;
		}
	}
}
