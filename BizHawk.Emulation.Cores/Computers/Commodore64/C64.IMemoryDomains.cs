using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	public partial class C64
	{
		private IMemoryDomains memoryDomains;

		private void SetupMemoryDomains()
		{

			// chips must be initialized before this code runs!
		    var domains = new List<MemoryDomain>(1)
		    {
		        new MemoryDomain("System Bus", 0x10000, MemoryDomain.Endian.Little,
		            addr => unchecked((byte) board.cpu.Peek((int) addr)),
		            (addr, val) => board.cpu.Poke(unchecked((int) addr), val)),
		        new MemoryDomain("RAM", 0x10000, MemoryDomain.Endian.Little, addr => unchecked((byte) board.ram.Peek(addr)),
		            (addr, val) => board.ram.Poke(addr, val)),
		        new MemoryDomain("CIA0", 0x10, MemoryDomain.Endian.Little, addr => unchecked((byte)board.cia0.Peek((int)addr)),
		            (addr, val) => board.cia0.Poke(unchecked((int)addr), val)),
		        new MemoryDomain("CIA1", 0x10, MemoryDomain.Endian.Little, addr => unchecked((byte)board.cia1.Peek((int)addr)), (addr, val) => board.cia1.Poke(unchecked((int)addr), val)),
		        new MemoryDomain("VIC", 0x40, MemoryDomain.Endian.Little, addr => unchecked((byte) board.vic.Peek((int)addr)), (addr, val) => board.vic.Poke(unchecked((int)addr), val)),
		        new MemoryDomain("SID", 0x20, MemoryDomain.Endian.Little, addr => unchecked((byte)board.sid.Peek((int)addr)), (addr, val) => board.sid.Poke(unchecked((int)addr), val))
		    };
		    //domains.Add(new MemoryDomain("1541 Bus", 0x10000, MemoryDomain.Endian.Little, new Func<int, byte>(disk.Peek), new Action<int, byte>(disk.Poke)));
			//domains.Add(new MemoryDomain("1541 VIA0", 0x10, MemoryDomain.Endian.Little, new Func<int, byte>(disk.PeekVia0), new Action<int, byte>(disk.PokeVia0)));
			//domains.Add(new MemoryDomain("1541 VIA1", 0x10, MemoryDomain.Endian.Little, new Func<int, byte>(disk.PeekVia1), new Action<int, byte>(disk.PokeVia1)));
			//domains.Add(new MemoryDomain("1541 RAM", 0x1000, MemoryDomain.Endian.Little, new Func<int, byte>(disk.PeekRam), new Action<int, byte>(disk.PokeRam)));
			memoryDomains = new MemoryDomainList(domains);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(memoryDomains);
		}
	}
}
