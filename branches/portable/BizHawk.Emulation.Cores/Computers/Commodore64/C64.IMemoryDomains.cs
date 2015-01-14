using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	public partial class C64 : IMemoryDomains
	{
		public IMemoryDomainList MemoryDomains
		{
			get { return memoryDomains; }
		}

		private MemoryDomainList memoryDomains;

		private void SetupMemoryDomains()
		{

			// chips must be initialized before this code runs!
			var domains = new List<MemoryDomain>(1);
			domains.Add(new MemoryDomain("System Bus", 0x10000, MemoryDomain.Endian.Little, board.cpu.Peek, board.cpu.Poke));
			domains.Add(new MemoryDomain("RAM", 0x10000, MemoryDomain.Endian.Little, board.ram.Peek, board.ram.Poke));
			domains.Add(new MemoryDomain("CIA0", 0x10, MemoryDomain.Endian.Little, board.cia0.Peek, board.cia0.Poke));
			domains.Add(new MemoryDomain("CIA1", 0x10, MemoryDomain.Endian.Little, board.cia1.Peek, board.cia1.Poke));
			domains.Add(new MemoryDomain("VIC", 0x40, MemoryDomain.Endian.Little, board.vic.Peek, board.vic.Poke));
			domains.Add(new MemoryDomain("SID", 0x20, MemoryDomain.Endian.Little, board.sid.Peek, board.sid.Poke));
			//domains.Add(new MemoryDomain("1541 Bus", 0x10000, MemoryDomain.Endian.Little, new Func<int, byte>(disk.Peek), new Action<int, byte>(disk.Poke)));
			//domains.Add(new MemoryDomain("1541 VIA0", 0x10, MemoryDomain.Endian.Little, new Func<int, byte>(disk.PeekVia0), new Action<int, byte>(disk.PokeVia0)));
			//domains.Add(new MemoryDomain("1541 VIA1", 0x10, MemoryDomain.Endian.Little, new Func<int, byte>(disk.PeekVia1), new Action<int, byte>(disk.PokeVia1)));
			//domains.Add(new MemoryDomain("1541 RAM", 0x1000, MemoryDomain.Endian.Little, new Func<int, byte>(disk.PeekRam), new Action<int, byte>(disk.PokeRam)));
			memoryDomains = new MemoryDomainList(domains);
		}
	}
}
