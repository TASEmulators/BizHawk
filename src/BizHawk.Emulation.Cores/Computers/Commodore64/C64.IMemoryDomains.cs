using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	public partial class C64
	{
		private IMemoryDomains _memoryDomains;

		private void SetupMemoryDomains()
		{
			bool diskDriveEnabled = _board.DiskDrive != null;
			bool tapeDriveEnabled = _board.TapeDrive != null;

			var domains = new List<MemoryDomain>
			{
				C64MemoryDomainFactory.Create("System Bus", 0x10000, a => _board.Cpu.Peek(a), (a, v) => _board.Cpu.Poke(a, v)),
				C64MemoryDomainFactory.Create("RAM", 0x10000, a => _board.Ram.Peek(a), (a, v) => _board.Ram.Poke(a, v)),
				C64MemoryDomainFactory.Create("CIA0", 0x10, a => _board.Cia0.Peek(a), (a, v) => _board.Cia0.Poke(a, v)),
				C64MemoryDomainFactory.Create("CIA1", 0x10, a => _board.Cia1.Peek(a), (a, v) => _board.Cia1.Poke(a, v)),
				C64MemoryDomainFactory.Create("VIC", 0x40, a => _board.Vic.Peek(a), (a, v) => _board.Vic.Poke(a, v)),
				C64MemoryDomainFactory.Create("SID", 0x20, a => _board.Sid.Peek(a), (a, v) => _board.Sid.Poke(a, v)),
			};

			if (diskDriveEnabled)
			{
				domains.AddRange(new[]
				{
					C64MemoryDomainFactory.Create("1541 Bus", 0x10000, a => _board.DiskDrive.Peek(a), (a, v) => _board.DiskDrive.Poke(a, v)),
					C64MemoryDomainFactory.Create("1541 RAM", 0x800, a => _board.DiskDrive.Peek(a), (a, v) => _board.DiskDrive.Poke(a, v)),
					C64MemoryDomainFactory.Create("1541 VIA0", 0x10, a => _board.DiskDrive.PeekVia0(a), (a, v) => _board.DiskDrive.PokeVia0(a, v)),
					C64MemoryDomainFactory.Create("1541 VIA1", 0x10, a => _board.DiskDrive.PeekVia1(a), (a, v) => _board.DiskDrive.PokeVia1(a, v))
				});
			}

			if (tapeDriveEnabled && (_board.TapeDrive.TapeDataDomain != null))
			{
				domains.AddRange(new[]
				{
					C64MemoryDomainFactory.Create("Tape Data", _board.TapeDrive.TapeDataDomain.Length, a => _board.TapeDrive.TapeDataDomain[a], (a, v) => _board.TapeDrive.TapeDataDomain[a] = (byte)v)
				});
			}

			_memoryDomains = new MemoryDomainList(domains);
			((BasicServiceProvider)ServiceProvider).Register(_memoryDomains);
		}

		private static class C64MemoryDomainFactory
		{
			public static MemoryDomain Create(string name, int size, Func<int, int> peekByte, Action<int, int> pokeByte)
			{
				return new MemoryDomainDelegate(name, size, MemoryDomain.Endian.Little, addr => unchecked((byte)peekByte((int)addr)), (addr, val) => pokeByte(unchecked((int)addr), val), 1);
			}
		}
	}
}
