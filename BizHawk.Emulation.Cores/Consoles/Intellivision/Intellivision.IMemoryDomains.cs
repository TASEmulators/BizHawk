using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Intellivision
{
	public sealed partial class Intellivision
	{
		internal IMemoryDomains MemoryDomains;

		private void SetupMemoryDomains()
		{
			// TODO: is 8bit for byte arrays and 16bit for ushort correct here?
			// If ushort is correct, how about little endian?
			var domains = new List<MemoryDomain>
			{
				new MemoryDomainDelegate(
					"Main RAM",
					ScratchpadRam.Length,
					MemoryDomain.Endian.Little,
					addr => ScratchpadRam[addr],
					(addr, value) => ScratchpadRam[addr] = value,
					1),
				new MemoryDomainDelegate(
					"Graphics RAM",
					GraphicsRam.Length,
					MemoryDomain.Endian.Little,
					addr => GraphicsRam[addr],
					(addr, value) => GraphicsRam[addr] = value,
					1),
				new MemoryDomainDelegate(
					"Graphics ROM",
					GraphicsRom.Length,
					MemoryDomain.Endian.Little,
					addr => GraphicsRom[addr],
					(addr, value) => GraphicsRom[addr] = value,
					1),
			};

			MemoryDomains = new MemoryDomainList(domains);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(MemoryDomains);
		}
	}
}
