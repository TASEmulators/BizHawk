using System;
using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	public partial class MAME
	{
		private IMemoryDomains _memoryDomains;
		private int _systemBusAddressShift = 0;

		private byte _peek(long addr, int firstOffset, long size)
		{
			if (addr < 0 || addr >= size)
			{
				throw new ArgumentOutOfRangeException();
			}

			if (!_memAccess)
			{
				_memAccess = true;
				_mamePeriodicComplete.WaitOne();
			}

			addr += firstOffset;

			var val = (byte)LibMAME.mame_read_byte((uint)addr << _systemBusAddressShift);

			_memoryAccessComplete.Set();

			return val;
		}

		private void _poke(long addr, byte val, int firstOffset, long size)
		{
			if (addr < 0 || addr >= size)
			{
				throw new ArgumentOutOfRangeException();
			}

			if (!_memAccess)
			{
				_memAccess = true;
				_mamePeriodicComplete.WaitOne();
			}

			addr += firstOffset;

			LibMAME.mame_lua_execute($"{ MAMELuaCommand.GetSpace }:write_u8({ addr << _systemBusAddressShift }, { val })");

			_memoryAccessComplete.Set();
		}

		private void InitMemoryDomains()
		{
			var domains = new List<MemoryDomain>();

			_systemBusAddressShift = LibMAME.mame_lua_get_int(MAMELuaCommand.GetSpaceAddressShift);
			var dataWidth = LibMAME.mame_lua_get_int(MAMELuaCommand.GetSpaceDataWidth) >> 3; // mame returns in bits
			var size = (long)LibMAME.mame_lua_get_double(MAMELuaCommand.GetSpaceAddressMask) + dataWidth;
			var endianString = MameGetString(MAMELuaCommand.GetSpaceEndianness);
			var deviceName = MameGetString(MAMELuaCommand.GetMainCPUName);
			//var addrSize = (size * 2).ToString();

			MemoryDomain.Endian endian = MemoryDomain.Endian.Unknown;

			if (endianString == "little")
			{
				endian = MemoryDomain.Endian.Little;
			}
			else if (endianString == "big")
			{
				endian = MemoryDomain.Endian.Big;
			}

			var mapCount = LibMAME.mame_lua_get_int(MAMELuaCommand.GetSpaceMapCount);

			for (int i = 1; i <= mapCount; i++)
			{
				var read = MameGetString($"return { MAMELuaCommand.SpaceMap }[{ i }].read.handlertype");
				var write = MameGetString($"return { MAMELuaCommand.SpaceMap }[{ i }].write.handlertype");

				if (read == "ram" && write == "ram" || read == "rom")
				{
					var firstOffset = LibMAME.mame_lua_get_int($"return { MAMELuaCommand.SpaceMap }[{ i }].address_start");
					var lastOffset = LibMAME.mame_lua_get_int($"return { MAMELuaCommand.SpaceMap }[{ i }].address_end");
					var name = $"{ deviceName } : { read } : 0x{ firstOffset:X}-0x{ lastOffset:X}";

					domains.Add(new MemoryDomainDelegate(name, lastOffset - firstOffset + 1, endian,
						delegate (long addr)
						{
							return _peek(addr, firstOffset, size);
						},
						read == "rom"
							? null
							: (long addr, byte val) => _poke(addr, val, firstOffset, size),
						dataWidth));
				}
			}

			domains.Add(new MemoryDomainDelegate(deviceName + " : System Bus", size, endian,
				delegate (long addr)
				{
					return _peek(addr, 0, size);
				},
				null, dataWidth));

			_memoryDomains = new MemoryDomainList(domains);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(_memoryDomains);
		}
	}
}