using System;
using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public partial class LibsnesCore
	{
		private readonly List<MemoryDomain> _memoryDomainList = new List<MemoryDomain>();
		private IMemoryDomains _memoryDomains;

		private void SetupMemoryDomains(byte[] romData, byte[] sgbRomData)
		{
			// lets just do this entirely differently for SGB
			if (IsSGB)
			{
				// NOTE: CGB has 32K of wram, and DMG has 8KB of wram. Not sure how to control this right now.. bsnes might not have any ready way of doign that? I couldnt spot it. 
				// You wouldnt expect a DMG game to access excess wram, but what if it tried to? maybe an oversight in bsnes?
				MakeMemoryDomain("SGB WRAM", LibsnesApi.SNES_MEMORY.SGB_WRAM, MemoryDomain.Endian.Little);

				var romDomain = new MemoryDomainByteArray("SGB CARTROM", MemoryDomain.Endian.Little, romData, true, 1);
				_memoryDomainList.Add(romDomain);

				// the last 1 byte of this is special.. its an interrupt enable register, instead of ram. weird. maybe its actually ram and just getting specially used?
				MakeMemoryDomain("SGB HRAM", LibsnesApi.SNES_MEMORY.SGB_HRAM, MemoryDomain.Endian.Little);

				MakeMemoryDomain("SGB CARTRAM", LibsnesApi.SNES_MEMORY.SGB_CARTRAM, MemoryDomain.Endian.Little);

				MakeMemoryDomain("WRAM", LibsnesApi.SNES_MEMORY.WRAM, MemoryDomain.Endian.Little);

				var sgbromDomain = new MemoryDomainByteArray("SGB.SFC ROM", MemoryDomain.Endian.Little, sgbRomData, true, 1);
				_memoryDomainList.Add(sgbromDomain);
			}
			else
			{
				MakeMemoryDomain("WRAM", LibsnesApi.SNES_MEMORY.WRAM, MemoryDomain.Endian.Little);

				MakeMemoryDomain("CARTROM", LibsnesApi.SNES_MEMORY.CARTRIDGE_ROM, MemoryDomain.Endian.Little, byteSize: 2);
				MakeMemoryDomain("CARTRAM", LibsnesApi.SNES_MEMORY.CARTRIDGE_RAM, MemoryDomain.Endian.Little, byteSize: 2);
				MakeMemoryDomain("VRAM", LibsnesApi.SNES_MEMORY.VRAM, MemoryDomain.Endian.Little, byteSize: 2);
				MakeMemoryDomain("OAM", LibsnesApi.SNES_MEMORY.OAM, MemoryDomain.Endian.Little, byteSize: 2);
				MakeMemoryDomain("CGRAM", LibsnesApi.SNES_MEMORY.CGRAM, MemoryDomain.Endian.Little, byteSize: 2);
				MakeMemoryDomain("APURAM", LibsnesApi.SNES_MEMORY.APURAM, MemoryDomain.Endian.Little, byteSize: 2);

				if (!DeterministicEmulation)
				{
					_memoryDomainList.Add(new MemoryDomainDelegate(
						"System Bus",
						0x1000000,
						MemoryDomain.Endian.Little,
						addr => api.QUERY_peek(LibsnesApi.SNES_MEMORY.SYSBUS, (uint)addr),
						(addr, val) => api.QUERY_poke(LibsnesApi.SNES_MEMORY.SYSBUS, (uint)addr, val), wordSize: 2));
				}
				else
				{
					// limited function bus
					MakeFakeBus();
				}
			}

			_memoryDomains = new MemoryDomainList(_memoryDomainList);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(_memoryDomains);
		}

		private unsafe void MakeMemoryDomain(string name, LibsnesApi.SNES_MEMORY id, MemoryDomain.Endian endian, int byteSize = 1)
		{
			int size = api.QUERY_get_memory_size(id);
			int mask = size - 1;
			bool pow2 = Util.IsPowerOfTwo(size);

			// if this type of memory isnt available, dont make the memory domain (most commonly save ram)
			if (size == 0)
			{
				return;
			}

			byte* blockptr = api.QUERY_get_memory_data(id);

			MemoryDomain md;

			if (id == LibsnesApi.SNES_MEMORY.OAM)
			{
				// OAM is actually two differently sized banks of memory which arent truly considered adjacent. 
				// maybe a better way to visualize it is with an empty bus and adjacent banks
				// so, we just throw away everything above its size of 544 bytes
				if (size != 544)
				{
					throw new InvalidOperationException("oam size isnt 544 bytes.. wtf?");
				}

				md = new MemoryDomainDelegate(
					name,
					size,
					endian,
					addr => addr < 544 ? blockptr[addr] : (byte)0x00,
					(addr, value) => { if (addr < 544) { blockptr[addr] = value; } },
					byteSize);
			}
			else if (pow2)
			{
				md = new MemoryDomainDelegate(
					name,
					size,
					endian,
					addr => blockptr[addr & mask],
					(addr, value) => blockptr[addr & mask] = value,
					byteSize);
			}
			else
			{
				md = new MemoryDomainDelegate(
					name,
					size,
					endian,
					addr => blockptr[addr % size],
					(addr, value) => blockptr[addr % size] = value,
					byteSize);
			}

			_memoryDomainList.Add(md);
		}

		private unsafe void MakeFakeBus()
		{
			int size = api.QUERY_get_memory_size(LibsnesApi.SNES_MEMORY.WRAM);
			if (size != 0x20000)
			{
				throw new InvalidOperationException();
			}

			byte* blockptr = api.QUERY_get_memory_data(LibsnesApi.SNES_MEMORY.WRAM);

			var md = new MemoryDomainDelegate("System Bus", 0x1000000, MemoryDomain.Endian.Little,
				addr =>
				{
					var a = FakeBusMap((int)addr);
					if (a.HasValue)
					{
						return blockptr[a.Value];
					}

					return FakeBusRead((int)addr);
				},
				(addr, val) =>
				{
					var a = FakeBusMap((int)addr);
					if (a.HasValue)
						blockptr[a.Value] = val;
				}, wordSize: 2);
			_memoryDomainList.Add(md);
		}
	}
}
