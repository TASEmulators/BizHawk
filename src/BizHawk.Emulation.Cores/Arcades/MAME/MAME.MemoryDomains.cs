using System;
using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	public partial class MAME : IMonitor
	{
		private IMemoryDomains _memoryDomains;

		private int _enterCount;

		public void Enter()
		{
			if (_enterCount == 0)
			{
				_mameCmd = MAME_CMD.WAIT;
				SafeWaitEvent(_mameCommandComplete);
			}

			_enterCount++;
		}

		public void Exit()
		{
			if (_enterCount <= 0)
			{
				throw new InvalidOperationException();
			}
			else if (_enterCount == 1)
			{
				_mameCommandWaitDone.Set();
			}

			_enterCount--;
		}

		public class MAMEMemoryDomain : MemoryDomain
		{
			private readonly IMonitor _monitor;
			private readonly int _firstOffset;
			private readonly int _systemBusAddressShift;
			private readonly long _systemBusSize;

			public MAMEMemoryDomain(string name, long size, Endian endian, int dataWidth, bool writable, IMonitor monitor, int firstOffset, int systemBusAddressShift, long systemBusSize)
			{
				Name = name;
				Size = size;
				EndianType = endian;
				WordSize = dataWidth;
				Writable = writable;

				_monitor = monitor;
				_firstOffset = firstOffset;
				_systemBusAddressShift = systemBusAddressShift;
				_systemBusSize = systemBusSize;
			}

			public override byte PeekByte(long addr)
			{
				if (addr < 0 || addr >= _systemBusSize) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");

				addr += _firstOffset;

				try
				{
					_monitor.Enter();
					return (byte)LibMAME.mame_read_byte((uint)addr << _systemBusAddressShift);
				}
				finally
				{
					_monitor.Exit();	
				}
			}

			public override void PokeByte(long addr, byte val)
			{
				if (Writable)
				{
					if (addr < 0 || addr >= _systemBusSize) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");

					addr += _firstOffset;

					try
					{
						_monitor.Enter();
						LibMAME.mame_lua_execute($"{MAMELuaCommand.GetSpace}:write_u8({addr << _systemBusAddressShift}, {val})");
					}
					finally
					{
						_monitor.Exit();
					}
				}
			}

			public override void Enter()
				=> _monitor.Enter();

			public override void Exit()
				=> _monitor.Exit();
		}

		private void InitMemoryDomains()
		{
			var domains = new List<MemoryDomain>();

			var systemBusAddressShift = LibMAME.mame_lua_get_int(MAMELuaCommand.GetSpaceAddressShift);
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

					domains.Add(new MAMEMemoryDomain(name, lastOffset - firstOffset + 1, endian,
						dataWidth, read != "rom", this, firstOffset, systemBusAddressShift, size));
				}
			}

			domains.Add(new MAMEMemoryDomain(deviceName + " : System Bus", size, endian, dataWidth, false, this, 0, systemBusAddressShift, size));

			_memoryDomains = new MemoryDomainList(domains);
			((MemoryDomainList)_memoryDomains).SystemBus = _memoryDomains[deviceName + " : System Bus"];
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(_memoryDomains);
		}
	}
}