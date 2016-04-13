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

			Func<long, byte> peekByte;
			Action<long, byte> pokeByte;

			if (swizzled)
			{
				peekByte = delegate(long addr)
				{
					if (addr < 0 || addr >= size)
					{
						throw new ArgumentOutOfRangeException();
					}

					return Marshal.ReadByte(memPtr, (int)(addr ^ 3));
				};
				pokeByte = delegate(long addr, byte val)
				{
					if (addr < 0 || addr >= size)
					{
						throw new ArgumentOutOfRangeException();
					}

					Marshal.WriteByte(memPtr, (int)(addr ^ 3), val);
				};
			}
			else
			{
				peekByte = delegate(long addr)
				{
					if (addr < 0 || addr >= size)
					{
						throw new ArgumentOutOfRangeException();
					}

					return Marshal.ReadByte(memPtr, (int)(addr));
				};
				pokeByte = delegate(long addr, byte val)
				{
					if (addr < 0 || addr >= size)
					{
						throw new ArgumentOutOfRangeException();
					}

					Marshal.WriteByte(memPtr, (int)(addr), val);
				};
			}

			var md = new MemoryDomainDelegate(name, size, endian, peekByte, pokeByte, 4);

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


			Func<long, byte> peekByte;
			Action<long, byte> pokeByte;

			peekByte = delegate(long addr)
			{
				return api.m64p_read_memory_8((uint)addr);
			};

			pokeByte = delegate(long addr, byte val)
			{
				api.m64p_write_memory_8((uint)addr, val);
			};

			_memoryDomains.Add(new MemoryDomainDelegate
				(
					"System Bus",
					uint.MaxValue,
					 MemoryDomain.Endian.Big,
					peekByte,
					pokeByte, 4
				));

			MemoryDomains = new MemoryDomainList(_memoryDomains);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(MemoryDomains);
		}
	}
}
