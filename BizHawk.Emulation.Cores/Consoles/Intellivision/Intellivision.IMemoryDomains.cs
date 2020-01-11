using System.Collections.Generic;
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
				new MemoryDomainDelegate(
					"System Ram",
					SystemRam.Length * 2,
					MemoryDomain.Endian.Little,
					addr => ReadByteFromShortArray(addr, SystemRam),
					(addr, value) => WriteByteToShortArray(addr, value, SystemRam),
					1
				),
				new MemoryDomainDelegate(
					"Executive Rom",
					ExecutiveRom.Length * 2,
					MemoryDomain.Endian.Little,
					addr => ReadByteFromShortArray(addr, ExecutiveRom),
					(addr, value) => WriteByteToShortArray(addr, value, ExecutiveRom),
					1
				),
				new MemoryDomainDelegate(
					"System Bus",
					0X20000,
					MemoryDomain.Endian.Little,
					addr => PeekSystemBus(addr),
					(addr, value) => PokeSystemBus(addr, value),
					1
				)
			};

			MemoryDomains = new MemoryDomainList(domains);
			((BasicServiceProvider) ServiceProvider).Register(MemoryDomains);
		}

		private byte PeekSystemBus(long addr)
		{
			if (addr % 2 == 0)
			{
				long index = addr / 2;
				return (byte)(ReadMemory((ushort)index, true) >> 8);
			}
			else
			{
				long index = (addr - 1) / 2;
				return (byte)(ReadMemory((ushort)index, true) & 0xFF);
			}
		}

		private void PokeSystemBus(long addr, byte value)
		{
			if (addr % 2 == 0)
			{
				long index = addr / 2;
				int temp = (ReadMemory((ushort)index, true) >> 8);
				WriteMemory((ushort)index, (ushort)(temp & (value << 8)), true);
			}
			else
			{
				long index = (addr - 1) / 2;
				int temp = ((ReadMemory((ushort)index, true) & 0xFF)<<8);
				WriteMemory((ushort)index, (ushort)(temp & value), true);
			}
		}

		// TODO: move these to a common library and maybe add an endian parameter
		// Little endian
		private byte ReadByteFromShortArray(long addr, ushort[] array)
		{
			if (addr % 2 == 0)
			{
				long index = addr / 2;
				return (byte)(array[index] >> 8);
				
			}
			else
			{
				long index = (addr - 1) / 2;
				return (byte)(array[index] & 0xFF);
			}
		}

		private void WriteByteToShortArray(long addr, byte value, ushort[] array)
		{
			if (addr % 2 == 0)
			{
				long index = (addr - 1) / 2;
				ushort val = (ushort)((value << 8) + (array[index] & 0xFF));
				array[index] = val;

			}
			else
			{
				long index = addr / 2;
				ushort val = (ushort)((((array[index] >> 8) & 0xFF) << 8) + value);
				array[index] = val;
			}
		}
	}
}
