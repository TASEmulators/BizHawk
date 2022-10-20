using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class RetroAchievements
	{
		private IReadOnlyList<MemFunctions> _memFunctions;

		private struct RAMemGuard : IMonitor
		{
			private readonly SemaphoreSlim _count;
			private readonly AutoResetEvent _start;
			private readonly AutoResetEvent _end;
			private readonly Func<bool> _isNotMainThread;
			// this is more or less a hacky workaround from dialog thread access causing lockups during DoAchievementsFrame
			private readonly SemaphoreSlim _asyncCount;
			private readonly Func<bool> _needsLock;
			private readonly ThreadLocal<bool> _isLocked;
			private readonly Mutex _memMutex;

			public RAMemGuard(SemaphoreSlim count, AutoResetEvent start, AutoResetEvent end, Func<bool> isNotMainThread, SemaphoreSlim asyncCount, Func<bool> needsLock)
			{
				_count = count;
				_start = start;
				_end = end;
				_isNotMainThread = isNotMainThread;
				_asyncCount = asyncCount;
				_needsLock = needsLock;
				_isLocked = new();
				_memMutex = new();
			}

			public void Enter()
			{
				_memMutex.WaitOne();

				if (_isNotMainThread())
				{
					if (_needsLock())
					{
						_count.Wait();
						_start.WaitOne();
						_isLocked.Value = true;
					}

					_asyncCount.Release();
				}
			}

			public void Exit()
			{
				if (_isNotMainThread())
				{
					if (_isLocked.Value)
					{
						_end.Set();
						_isLocked.Value = false;
					}

					_asyncCount.Wait();
				}

				_memMutex.ReleaseMutex();
			}
		}

		private readonly RAMemGuard _memGuard;

		private class DummyDomain : MemoryDomain
		{
			public DummyDomain(long size)
				=> Size = size;

			public override byte PeekByte(long addr)
				=> 0;

			public override void PokeByte(long addr, byte val)
			{
			}
		}

		private class MemFunctions
		{
			private readonly MemoryDomain _domain;
			private readonly int _domainAddrStart; // addr of _domain where bank begins
			private readonly int _addressMangler; // of course, let's *not* correct internal core byteswapping!

			public readonly RAInterface.ReadMemoryFunc ReadFunc;
			public readonly RAInterface.WriteMemoryFunc WriteFunc;
			public readonly RAInterface.ReadMemoryBlockFunc ReadBlockFunc;
			public readonly int BankSize;

			public RAMemGuard MemGuard { protected get; set; }

			protected virtual int FixAddr(int addr)
				=> _domainAddrStart + addr;

			protected virtual byte ReadMem(int addr)
			{
				using (MemGuard.EnterExit())
				{
					return _domain.PeekByte(FixAddr(addr) ^ _addressMangler);
				}
			}

			protected virtual void WriteMem(int addr, byte val)
			{
				using (MemGuard.EnterExit())
				{
					_domain.PokeByte(FixAddr(addr) ^ _addressMangler, val);
				}
			}
			
			protected virtual int ReadMemBlock(int addr, IntPtr buffer, int bytes)
			{
				addr = FixAddr(addr);

				if (addr >= (_domainAddrStart + BankSize))
				{
					return 0;
				}

				using (MemGuard.EnterExit())
				{
					var end = Math.Min(addr + bytes, _domainAddrStart + BankSize);
					if (_addressMangler == 0)
					{
						var ret = new byte[end - addr];
						_domain.BulkPeekByte(((long)addr).RangeToExclusive(end), ret);
						Marshal.Copy(ret, 0, buffer, end - addr);
					}
					else
					{
						unsafe
						{
							for (var i = addr; i < end; i++)
							{
								((byte*)buffer)[i - addr] = _domain.PeekByte(i ^ _addressMangler);
							}
						}
					}

					return end - addr;
				}
			}

			public MemFunctions(MemoryDomain domain, int domainAddrStart, long bankSize, int addressMangler = 0)
			{
				_domain = domain;
				_domainAddrStart = domainAddrStart;
				_addressMangler = addressMangler;

				ReadFunc = ReadMem;
				WriteFunc = WriteMem;
				ReadBlockFunc = ReadMemBlock;

				if (bankSize > int.MaxValue)
				{
					throw new OverflowException("bankSize is too big!");
				}

				BankSize = (int)bankSize;
			}
		}

		// this is a complete hack because the libretro Intelli core sucks and so achievements are made expecting this format
		private class IntelliMemFunctions : MemFunctions
		{
			protected override int FixAddr(int addr)
				=> ((addr - 0x80) >> 1) + (~addr & 1);

			protected override byte ReadMem(int addr)
			{
				if (addr < 0x80 || (addr & 2) != 0)
				{
					return 0;
				}

				return base.ReadMem(addr);
			}

			protected override void WriteMem(int addr, byte val)
			{
				if (addr < 0x80 || (addr & 2) != 0)
				{
					return;
				}

				base.WriteMem(addr, val);
			}

			protected override int ReadMemBlock(int addr, IntPtr buffer, int bytes)
			{
				if (addr >= 0x40080)
				{
					return 0;
				}

				var end = Math.Min(addr + bytes, 0x40080);
				unsafe
				{
					for (var i = addr; i < end; i++)
					{
						((byte*)buffer)[i - addr] = ReadMem(i);
					}
				}

				return end - addr;
			}

			public IntelliMemFunctions(MemoryDomain domain)
				: base(domain, 0, 0x40080)
			{
			}
		}

		private class ChanFMemFunctions : MemFunctions
		{
			private readonly IDebuggable _debuggable;
			private readonly MemoryDomain _vram; // our vram is unpacked, but RA expects it packed

			private byte ReadVRAMPacked(int addr)
			{
				return (byte)(((_vram.PeekByte(addr * 4 + 0) & 3) << 6)
					| ((_vram.PeekByte(addr * 4 + 1) & 3) << 4)
					| ((_vram.PeekByte(addr * 4 + 2) & 3) << 2)
					| ((_vram.PeekByte(addr * 4 + 3) & 3) << 0));
			}

			protected override byte ReadMem(int addr)
			{
				using (MemGuard.EnterExit())
				{
					if (addr < 0x40)
					{
						return (byte)_debuggable.GetCpuFlagsAndRegisters()["SPR" + addr].Value;
					}
					else
					{
						return ReadVRAMPacked(addr - 0x40);
					}
				}
			}

			protected override void WriteMem(int addr, byte val)
			{
				using (MemGuard.EnterExit())
				{
					if (addr < 0x40)
					{
						_debuggable.SetCpuRegister("SPR" + addr, val);
					}
					else
					{
						addr -= 0x40;
						_vram.PokeByte(addr * 4 + 0, (byte)((val >> 6) & 3));
						_vram.PokeByte(addr * 4 + 1, (byte)((val >> 4) & 3));
						_vram.PokeByte(addr * 4 + 2, (byte)((val >> 2) & 3));
						_vram.PokeByte(addr * 4 + 3, (byte)((val >> 0) & 3));
					}
				}
			}

			protected override int ReadMemBlock(int addr, IntPtr buffer, int bytes)
			{
				if (addr >= BankSize)
				{
					return 0;
				}

				using (MemGuard.EnterExit())
				{
					var regs = _debuggable.GetCpuFlagsAndRegisters();
					var end = Math.Min(addr + bytes, BankSize);
					for (int i = addr; i < end; i++)
					{
						byte val;
						if (i < 0x40)
						{
							val = (byte)regs["SPR" + i].Value;
						}
						else
						{
							val = ReadVRAMPacked(i - 0x40);
						}

						unsafe
						{
							((byte*)buffer)[i - addr] = val;
						}
					}

					return end - addr;
				}
			}

			public ChanFMemFunctions(IDebuggable debuggable, MemoryDomain vram)
				: base(null, 0, 0x840)
			{
				_debuggable = debuggable;
				_vram = vram;
			}
		}

		// these consoles will use the entire system bus
		private static readonly RAInterface.ConsoleID[] UseFullSysBus = new[]
		{
			RAInterface.ConsoleID.NES, RAInterface.ConsoleID.C64,
			RAInterface.ConsoleID.AmstradCPC, RAInterface.ConsoleID.Atari7800,
		};

		// these consoles will use the entire main memory domain
		private static readonly RAInterface.ConsoleID[] UseFullMainMem = new[]
		{
			RAInterface.ConsoleID.PlayStation, RAInterface.ConsoleID.Lynx,
			RAInterface.ConsoleID.Lynx, RAInterface.ConsoleID.NeoGeoPocket,
			RAInterface.ConsoleID.Jaguar, RAInterface.ConsoleID.JaguarCD,
			RAInterface.ConsoleID.DS, RAInterface.ConsoleID.AppleII,
			RAInterface.ConsoleID.Vectrex, RAInterface.ConsoleID.Tic80,
			RAInterface.ConsoleID.PCEngine,
		};

		// these consoles will use part of the system bus at an offset
		private static readonly Dictionary<RAInterface.ConsoleID, (int, int)[]> UsePartialSysBus = new()
		{
			[RAInterface.ConsoleID.MasterSystem] = new[] { (0xC000, 0x2000) },
			[RAInterface.ConsoleID.GameGear] = new[] { (0xC000, 0x2000) },
			[RAInterface.ConsoleID.Atari2600] = new[] { (0, 0x80) },
			[RAInterface.ConsoleID.Colecovision] = new[] { (0x6000, 0x400) },
			[RAInterface.ConsoleID.GBA] = new[] { (0x3000000, 0x8000), (0x2000000, 0x40000) },
			[RAInterface.ConsoleID.SG1000] = new[] { (0xC000, 0x2000), (0x2000, 0x2000), (0x8000, 0x2000) },
		};

		// GB is a bit complicated, since we could be a single core or a linked core, and GBC wants banks 2-7 of WRAM appended at the end
		private static void AddGBDomains(List<MemFunctions> mfs, IMemoryDomains domains)
		{
			string sysBus, cartRam, wram;
			if (domains.Has("P1 System Bus")) // GambatteLink
			{
				sysBus = "P1 System Bus";
				cartRam = "P1 CartRAM";
				wram = "P1 WRAM";
			}
			else if (domains.Has("System Bus L")) // GBHawkLink / GBHawkLink3x
			{
				sysBus = "System Bus L";
				cartRam = "Cart RAM L";
				wram = "Main RAM L";
			}
			else if (domains.Has("System Bus A")) // GBHawkLink4x
			{
				sysBus = "System Bus A";
				cartRam = "Cart RAM A";
				wram = "Main RAM A";
			}
			else // Gambatte / GBHawk
			{
				sysBus = "System Bus";
				cartRam = "CartRAM";
				wram = "WRAM";
			}

			mfs.Add(new(domains[sysBus], 0, 0xA000));
			if (domains.Has(cartRam))
			{
				if (domains[cartRam].Size == 0x200) // MBC2
				{
					mfs.Add(new(domains[cartRam], 0, 0x200));
					mfs.Add(new(new DummyDomain(0x1E00), 0, 0x1E00));
				}
				else
				{
					mfs.Add(new(domains[cartRam], 0, 0x2000));
				}
			}
			else
			{
				mfs.Add(new(domains[sysBus], 0xA000, 0x2000));
			}
			mfs.Add(new(domains[wram], 0x0000, 0x2000));
			mfs.Add(new(domains[sysBus], 0xE000, 0x2000));
			if (domains[wram].Size == 0x8000)
			{
				mfs.Add(new(domains[wram], 0x2000, 0x6000));
			}
		}

		// anything more complicated will be handled accordingly

		private static IReadOnlyList<MemFunctions> CreateMemoryBanks(
			RAInterface.ConsoleID consoleId, IMemoryDomains domains, IDebuggable debuggable)
		{
			var mfs = new List<MemFunctions>();

			if (Array.Exists(UseFullSysBus, id => id == consoleId))
			{
				mfs.Add(new(domains.SystemBus, 0, domains.SystemBus.Size));
			}
			else if (Array.Exists(UseFullMainMem, id => id == consoleId))
			{
				mfs.Add(new(domains.MainMemory, 0, domains.MainMemory.Size));
			}
			else if (UsePartialSysBus.TryGetValue(consoleId, out var pairs))
			{
				foreach (var pair in pairs)
				{
					mfs.Add(new(domains.SystemBus, pair.Item1, pair.Item2));
				}
			}
			else
			{
				switch (consoleId)
				{
					case RAInterface.ConsoleID.MegaDrive:
					case RAInterface.ConsoleID.Sega32X:
						mfs.Add(new(domains["68K RAM"], 0, domains["68K RAM"].Size, 1));
						if (domains.Has("32X RAM"))
						{
							mfs.Add(new(domains["32X RAM"], 0, domains["32X RAM"].Size, 1));
						}
						if (domains.Has("SRAM"))
						{
							// our picodrive doesn't byteswap its SRAM, so...
							var isGpgx = domains["SRAM"] is MemoryDomainIntPtrSwap16Monitor;
							mfs.Add(new(domains["SRAM"], 0, domains["SRAM"].Size, isGpgx ? 1 : 0));
						}
						break;
					case RAInterface.ConsoleID.SNES:
						mfs.Add(new(domains["WRAM"], 0, domains["WRAM"].Size));
						// annoying difference in BSNESv115+
						if (domains.Has("CARTRIDGE_RAM"))
						{
							mfs.Add(new(domains["CARTRIDGE_RAM"], 0, domains["CARTRIDGE_RAM"].Size));
						}
						else if (domains.Has("CARTRAM"))
						{
							mfs.Add(new(domains["CARTRAM"], 0, domains["CARTRAM"].Size));
						}
						break;
					case RAInterface.ConsoleID.GB:
					case RAInterface.ConsoleID.GBC:
						if (domains.Has("SGB_ROM"))
						{
							// uh oh, BSNESv115+ can't handle this case (todo: Expose more GB memory domains!!!)
							mfs.Add(new(domains["SGB CARTROM"], 0, 0x8000));
							mfs.Add(new(new DummyDomain(0x8000), 0, 0x8000));
						}
						else if (domains.Has("SGB CARTROM"))
						{
							// not as many domains, but should be functional enough
							mfs.Add(new(domains["SGB CARTROM"], 0, 0x8000));
							mfs.Add(new(new DummyDomain(0x2000), 0, 0x2000));
							if (domains.Has("SGB CARTRAM"))
							{
								if (domains["SGB CARTRAM"].Size == 0x200) // MBC2
								{
									mfs.Add(new(domains["SGB CARTRAM"], 0, 0x200));
									mfs.Add(new(new DummyDomain(0x1E00), 0, 0x1E00));
								}
								else
								{
									mfs.Add(new(domains["SGB CARTRAM"], 0, 0x2000));
								}
							}
							else
							{
								mfs.Add(new(new DummyDomain(0x2000), 0, 0x2000));
							}
							mfs.Add(new(domains["SGB WRAM"], 0, 0x2000));
							mfs.Add(new(domains["SGB WRAM"], 0, 0x1E00));
							mfs.Add(new(new DummyDomain(0x180), 0, 0x180));
							mfs.Add(new(domains["SGB HRAM"], 0, 0x80));
						}
						else
						{
							AddGBDomains(mfs, domains);
						}
						break;
					case RAInterface.ConsoleID.SegaCD:
						mfs.Add(new(domains["68K RAM"], 0, domains["68K RAM"].Size, 1));
						mfs.Add(new(domains["CD PRG RAM"], 0, domains["CD PRG RAM"].Size, 1));
						break;
					case RAInterface.ConsoleID.MagnavoxOdyssey:
						mfs.Add(new(domains["CPU RAM"], 0, domains["CPU RAM"].Size));
						mfs.Add(new(domains["Main RAM"], 0, domains["Main RAM"].Size));
						break;
					case RAInterface.ConsoleID.VirtualBoy:
						// todo: add System Bus so this isn't needed
						mfs.Add(new(domains["WRAM"], 0, domains["WRAM"].Size));
						mfs.Add(new(domains["CARTRAM"], 0, domains["CARTRAM"].Size));
						break;
					case RAInterface.ConsoleID.MSX:
						// no, can't use MainMemory here, as System Bus is that due to init ordering
						// todo: make this MainMemory
						mfs.Add(new(domains["RAM"], 0, domains["RAM"].Size));
						break;
					case RAInterface.ConsoleID.Saturn:
						// todo: add System Bus so this isn't needed
						mfs.Add(new(domains["Work Ram Low"], 0, domains["Work Ram Low"].Size));
						mfs.Add(new(domains["Work Ram High"], 0, domains["Work Ram High"].Size));
						break;
					case RAInterface.ConsoleID.Intellivision:
						// special case
						mfs.Add(new IntelliMemFunctions(domains.SystemBus));
						break;
					case RAInterface.ConsoleID.PCFX:
						// todo: add System Bus so this isn't needed
						mfs.Add(new(domains["Main RAM"], 0, domains["Main RAM"].Size));
						mfs.Add(new(domains["Backup RAM"], 0, domains["Backup RAM"].Size));
						mfs.Add(new(domains["Extra Backup RAM"], 0, domains["Extra Backup RAM"].Size));
						break;
					case RAInterface.ConsoleID.WonderSwan:
						mfs.Add(new(domains["RAM"], 0, domains["RAM"].Size));
						if (domains.Has("SRAM"))
						{
							mfs.Add(new(domains["SRAM"], 0, domains["SRAM"].Size));
						}
						else if (domains.Has("EEPROM"))
						{
							mfs.Add(new(domains["EEPROM"], 0, domains["EEPROM"].Size));
						}
						break;
					case RAInterface.ConsoleID.FairchildChannelF:
						// special case
						mfs.Add(new ChanFMemFunctions(debuggable, domains["VRAM"]));
						mfs.Add(new(domains.SystemBus, 0, domains.SystemBus.Size));
						break;
					case RAInterface.ConsoleID.PCEngineCD:
						mfs.Add(new(domains["System Bus (21 bit)"], 0x1F0000, 0x2000));
						mfs.Add(new(domains["System Bus (21 bit)"], 0x100000, 0x10000));
						mfs.Add(new(domains["System Bus (21 bit)"], 0xD0000, 0x30000));
						mfs.Add(new(domains["System Bus (21 bit)"], 0x1EE000, 0x800));
						break;
					case RAInterface.ConsoleID.N64:
						mfs.Add(new(domains.MainMemory, 0, domains.MainMemory.Size, 3));
						break;
					case RAInterface.ConsoleID.UnknownConsoleID:
					case RAInterface.ConsoleID.ZXSpectrum: // this doesn't actually have anything standardized, so...
					default:
						mfs.Add(new(domains.MainMemory, 0, domains.MainMemory.Size));
						break;
				}
			}

			return mfs.AsReadOnly();
		}
	}
}
