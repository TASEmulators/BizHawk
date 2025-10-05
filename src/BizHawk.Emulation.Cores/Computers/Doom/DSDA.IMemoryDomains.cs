using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public partial class DSDA
	{
		private IMemoryDomains MemoryDomains;

		private void SetupMemoryDomains()
		{
			var pagesDomain = _elf.GetPagesDomain();
			var internalMemorySize = pagesDomain.Size * 4096;

			var domains = new List<MemoryDomain>
			{
				new MemoryDomainDelegate(
					"Players",
					0x10000,
					MemoryDomain.Endian.Little,
					addr =>
					{
						if (addr > 0xFFFF)
						{
							throw new ArgumentOutOfRangeException(
								paramName: nameof(addr),
								addr, message: "address out of range");
						}
						return _core.dsda_read_memory_array(LibDSDA.MemoryArrayType.Players, (uint) addr);
					},
					null,
					1),
				new MemoryDomainDelegate(
					"Things",
					0x10000000,
					MemoryDomain.Endian.Little,
					addr =>
					{
						if (addr > 0xFFFFFFF)
						{
							throw new ArgumentOutOfRangeException(
								paramName: nameof(addr),
								addr, message: "address out of range");
						}
						return _core.dsda_read_memory_array(LibDSDA.MemoryArrayType.Things, (uint) addr);
					},
					null,
					1),
				new MemoryDomainDelegate(
					"Lines",
					0x1000000,
					MemoryDomain.Endian.Little,
					addr =>
					{
						if (addr > 0xFFFFFF)
						{
							throw new ArgumentOutOfRangeException(
								paramName: nameof(addr),
								addr, message: "address out of range");
						}
						return _core.dsda_read_memory_array(LibDSDA.MemoryArrayType.Lines, (uint) addr);
					},
					null,
					1),
				new MemoryDomainDelegate(
					"Sectors",
					0x1000000,
					MemoryDomain.Endian.Little,
					addr =>
					{
						if (addr > 0xFFFFFF)
						{
							throw new ArgumentOutOfRangeException(
								paramName: nameof(addr),
								addr, message: "address out of range");
						}
						return _core.dsda_read_memory_array(LibDSDA.MemoryArrayType.Sectors, (uint) addr);
					},
					null,
					1),
				new MemoryDomainDelegate(
					"Internal Memory",
					internalMemorySize,
					MemoryDomain.Endian.Little,
					addr =>
					{
						if (addr >= internalMemorySize)
						{
							throw new ArgumentOutOfRangeException(
								paramName: nameof(addr),
								addr, message: "address out of range");
						}

						var baseAddress = (IntPtr) 0x36f_0000_0000; // hardcoded for wbx
						var page = addr / 4096;
						const int readablePageBit = 1;

						if ((pagesDomain.PeekByte(page) & readablePageBit) is not 0)
						{
							byte ret = 0;
							unsafe
							{
								ret = ((byte*)baseAddress)[addr];
							}
							return ret;
						}
						else
						{
							return 0;
						}
					},
					null,
					1),
				pagesDomain,
			};
			MemoryDomains = new MemoryDomainList(domains);
			((BasicServiceProvider)ServiceProvider).Register(MemoryDomains);
		}
	}
}
