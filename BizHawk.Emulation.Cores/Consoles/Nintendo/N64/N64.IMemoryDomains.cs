using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.N64.NativeApi;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	public partial class N64
	{
		private List<MemoryDomain> _memoryDomains = new List<MemoryDomain>();

		private IMemoryDomains MemoryDomains;

		private void MakeMemoryDomain(string name, mupen64plusApi.N64_MEMORY id, MemoryDomain.Endian endian, bool swizzled = false)
		{
			int size = api.get_memory_size(id);

			//if this type of memory isnt available, dont make the memory domain
			if (size == 0)
			{
				return;
			}

			IntPtr memPtr = api.get_memory_ptr(id);

			Func<int, byte> peekByte;
			Action<int, byte> pokeByte;

			if (swizzled)
			{
				peekByte = delegate(int addr)
				{
					if (addr < 0 || addr >= size)
					{
						throw new ArgumentOutOfRangeException();
					}

					return Marshal.ReadByte(memPtr, (addr ^ 3));
				};
				pokeByte = delegate(int addr, byte val)
				{
					if (addr < 0 || addr >= size)
					{
						throw new ArgumentOutOfRangeException();
					}

					Marshal.WriteByte(memPtr, (addr ^ 3), val);
				};
			}
			else
			{
				peekByte = delegate(int addr)
				{
					if (addr < 0 || addr >= size)
					{
						throw new ArgumentOutOfRangeException();
					}

					return Marshal.ReadByte(memPtr, (addr));
				};
				pokeByte = delegate(int addr, byte val)
				{
					if (addr < 0 || addr >= size)
					{
						throw new ArgumentOutOfRangeException();
					}

					Marshal.WriteByte(memPtr, (addr), val);
				};
			}

			var md = new MemoryDomain(name, size, endian, peekByte, pokeByte);

			_memoryDomains.Add(md);
		}

		private void InitMemoryDomains()
		{
			//zero 07-sep-2014 - made RDRAM big endian domain, but none others. others need to be studied individually.
			MakeMemoryDomain("RDRAM", mupen64plusApi.N64_MEMORY.RDRAM, MemoryDomain.Endian.Big, true);

			MakeMemoryDomain("ROM", mupen64plusApi.N64_MEMORY.THE_ROM, MemoryDomain.Endian.Big, true);

			MakeMemoryDomain("PI Register", mupen64plusApi.N64_MEMORY.PI_REG, MemoryDomain.Endian.Little);
			MakeMemoryDomain("SI Register", mupen64plusApi.N64_MEMORY.SI_REG, MemoryDomain.Endian.Little);
			MakeMemoryDomain("VI Register", mupen64plusApi.N64_MEMORY.VI_REG, MemoryDomain.Endian.Little);
			MakeMemoryDomain("RI Register", mupen64plusApi.N64_MEMORY.RI_REG, MemoryDomain.Endian.Little);
			MakeMemoryDomain("AI Register", mupen64plusApi.N64_MEMORY.AI_REG, MemoryDomain.Endian.Little);

			MakeMemoryDomain("EEPROM", mupen64plusApi.N64_MEMORY.EEPROM, MemoryDomain.Endian.Little);

			if (_syncSettings.Controllers[0].IsConnected &&
				_syncSettings.Controllers[0].PakType == N64SyncSettings.N64ControllerSettings.N64ControllerPakType.MEMORY_CARD)
			{
				MakeMemoryDomain("Mempak 1", mupen64plusApi.N64_MEMORY.MEMPAK1, MemoryDomain.Endian.Little);
			}

			if (_syncSettings.Controllers[1].IsConnected &&
				_syncSettings.Controllers[1].PakType == N64SyncSettings.N64ControllerSettings.N64ControllerPakType.MEMORY_CARD)
			{
				MakeMemoryDomain("Mempak 2", mupen64plusApi.N64_MEMORY.MEMPAK2, MemoryDomain.Endian.Little);
			}

			if (_syncSettings.Controllers[2].IsConnected &&
				_syncSettings.Controllers[2].PakType == N64SyncSettings.N64ControllerSettings.N64ControllerPakType.MEMORY_CARD)
			{
				MakeMemoryDomain("Mempak 3", mupen64plusApi.N64_MEMORY.MEMPAK3, MemoryDomain.Endian.Little);
			}

			if (_syncSettings.Controllers[3].IsConnected &&
				_syncSettings.Controllers[3].PakType == N64SyncSettings.N64ControllerSettings.N64ControllerPakType.MEMORY_CARD)
			{
				MakeMemoryDomain("Mempak 4", mupen64plusApi.N64_MEMORY.MEMPAK4, MemoryDomain.Endian.Little);
			}


			Func<int, byte> peekByte;
			Action<int, byte> pokeByte;

			peekByte = delegate(int addr)
			{
				return api.m64p_read_memory_8((uint)addr);
			};

			pokeByte = delegate(int addr, byte val)
			{
				api.m64p_write_memory_8((uint)addr, val);
			};

			_memoryDomains.Add(new MemoryDomain
				(
					name: "System Bus",
					size: 0, // special case for full 32bit memorydomain
					endian: MemoryDomain.Endian.Big,
					peekByte: peekByte,
					pokeByte: pokeByte
				));

			MemoryDomains = new MemoryDomainList(_memoryDomains);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(MemoryDomains);
		}
	}
}
