using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public abstract partial class RetroAchievements
	{
		protected class RAMemGuard : IMonitor, IDisposable
		{
			private readonly ManualResetEventSlim _memLock;
			private readonly SemaphoreSlim _memSema;
			private readonly object _memSync;

			public RAMemGuard(ManualResetEventSlim memLock, SemaphoreSlim memSema, object memSync)
			{
				_memLock = memLock;
				_memSema = memSema;
				_memSync = memSync;
			}

			public void Enter()
			{
				lock (_memSync)
				{
					_memLock.Wait();
					_memSema.Wait();
				}
			}

			public void Exit()
			{
				_memSema.Release();
			}

			public void Dispose()
			{
				_memLock.Dispose();
				_memSema.Dispose();
			}
		}

		protected class RAMemAccess : IMonitor
		{
			private readonly ManualResetEventSlim _memLock;
			private readonly SemaphoreSlim _memSema;
			private readonly object _memSync;
			private int _refCount;

			public RAMemAccess(ManualResetEventSlim memLock, SemaphoreSlim memSema, object memSync)
			{
				_memLock = memLock;
				_memSema = memSema;
				_memSync = memSync;
				_refCount = 0;
			}

			public void Enter()
			{
				if (_refCount == 0)
				{
					_memLock.Set();
				}

				_refCount++;
			}

			public void Exit()
			{
				switch (_refCount)
				{
					case <= 0:
						throw new InvalidOperationException($"Invalid {nameof(_refCount)}");
					case 1:
					{
						lock (_memSync)
						{
							_memLock.Reset();
							_memSema.Wait();
							_memSema.Release();
						}

						break;
					}
				}

				_refCount--;
			}
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate byte ReadMemoryFunc(uint address);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void WriteMemoryFunc(uint address, byte value);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate uint ReadMemoryBlockFunc(uint address, IntPtr buffer, uint bytes);

		protected class MemFunctions
		{
			protected readonly MemoryDomain _domain;
			private readonly uint _domainAddrStart; // addr of _domain where bank begins
			private readonly uint _addressMangler; // of course, let's *not* correct internal core byteswapping!

			public ReadMemoryFunc ReadFunc { get; protected init; }
			public WriteMemoryFunc WriteFunc { get; protected init; }
			public ReadMemoryBlockFunc ReadBlockFunc { get; protected init; }

			public uint StartAddress; // this is set for our rcheevos impl
			public readonly uint BankSize;

			public RAMemGuard MemGuard { get; set; }

			protected virtual uint FixAddr(uint addr)
				=> _domainAddrStart + addr;

			protected virtual byte ReadMem(uint addr)
			{
				using (MemGuard.EnterExit())
				{
					return _domain.PeekByte(FixAddr(addr) ^ _addressMangler);
				}
			}

			protected virtual void WriteMem(uint addr, byte val)
			{
				using (MemGuard.EnterExit())
				{
					_domain.PokeByte(FixAddr(addr) ^ _addressMangler, val);
				}
			}
			
			protected virtual uint ReadMemBlock(uint addr, IntPtr buffer, uint bytes)
			{
				addr = FixAddr(addr);

				if (addr >= _domainAddrStart + BankSize)
				{
					return 0;
				}

				using (MemGuard.EnterExit())
				{
					var end = Math.Min(addr + bytes, _domainAddrStart + BankSize);
					var length = end - addr;

					if (_addressMangler == 0)
					{
						var ret = new byte[length];
						_domain.BulkPeekByte(((long)addr).RangeToExclusive(end), ret);
						Marshal.Copy(ret, 0, buffer, (int)length);
					}
					else
					{
						unsafe
						{
							for (var i = addr; i < end; i++)
							{
								((byte*)buffer)![i - addr] = _domain.PeekByte(i ^ _addressMangler);
							}
						}
					}

					return length;
				}
			}

			public MemFunctions(MemoryDomain domain, uint domainAddrStart, long bankSize, uint addressMangler = 0)
			{
				_domain = domain;
				_domainAddrStart = domainAddrStart;
				_addressMangler = addressMangler;

				ReadFunc = ReadMem;
				WriteFunc = WriteMem;
				ReadBlockFunc = ReadMemBlock;

				// while rcheevos could go all the way to uint.MaxValue, RAIntegration is restricted to int.MaxValue
				if (bankSize > int.MaxValue)
				{
					throw new OverflowException("bankSize is too big!");
				}

				BankSize = (uint)bankSize;
			}
		}

		private class NullMemFunctions : MemFunctions
		{
			public NullMemFunctions(long bankSize)
				: base(null, 0, bankSize)
			{
				ReadFunc = null;
				WriteFunc = null;
				ReadBlockFunc = null;
			}
		}

		// this is a complete hack because the libretro Intelli core sucks and so achievements are made expecting this format
		private class IntelliMemFunctions : MemFunctions
		{
			protected override uint FixAddr(uint addr)
				=> (addr >> 1) + (~addr & 1);

			protected override byte ReadMem(uint addr)
			{
				if ((addr & 2) != 0)
				{
					return 0;
				}

				return base.ReadMem(addr);
			}

			protected override void WriteMem(uint addr, byte val)
			{
				if ((addr & 2) != 0)
				{
					return;
				}

				base.WriteMem(addr, val);
			}

			protected override uint ReadMemBlock(uint addr, IntPtr buffer, uint bytes)
			{
				if (addr >= BankSize)
				{
					return 0;
				}

				using (MemGuard.EnterExit())
				{
					var end = Math.Min(addr + bytes, BankSize);
					var length = end - addr;

					unsafe
					{
						for (var i = addr; i < end; i++)
						{
							if ((i & 2) != 0)
							{
								((byte*)buffer)![i - addr] = 0;
							}
							else
							{
								((byte*)buffer)![i - addr] = _domain.PeekByte(FixAddr(i));
							}
						}
					}

					return length;
				}
			}

			public IntelliMemFunctions(MemoryDomain domain)
				: base(domain, 0, 0x40000)
			{
			}
		}

		private class ChanFMemFunctions : MemFunctions
		{
			private readonly IDebuggable _debuggable;
			private readonly MemoryDomain _vram; // our vram is unpacked, but RA expects it packed

			private byte ReadVRAMPacked(uint addr)
			{
				return (byte)(((_vram.PeekByte(addr * 4 + 0) & 3) << 6)
					| ((_vram.PeekByte(addr * 4 + 1) & 3) << 4)
					| ((_vram.PeekByte(addr * 4 + 2) & 3) << 2)
					| ((_vram.PeekByte(addr * 4 + 3) & 3) << 0));
			}

			protected override byte ReadMem(uint addr)
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

			protected override void WriteMem(uint addr, byte val)
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

			protected override uint ReadMemBlock(uint addr, IntPtr buffer, uint bytes)
			{
				if (addr >= BankSize)
				{
					return 0;
				}

				using (MemGuard.EnterExit())
				{
					var regs = _debuggable.GetCpuFlagsAndRegisters();
					var end = Math.Min(addr + bytes, BankSize);
					for (var i = addr; i < end; i++)
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
							((byte*)buffer)![i - addr] = val;
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
		private static readonly ConsoleID[] UseFullSysBus =
		[
			ConsoleID.NES, ConsoleID.C64, ConsoleID.AmstradCPC, ConsoleID.Atari7800,
		];

		// these consoles will use the entire main memory domain
		private static readonly ConsoleID[] UseFullMainMem =
		[
			ConsoleID.Amiga, ConsoleID.Lynx, ConsoleID.NeoGeoPocket, ConsoleID.Jaguar,
			ConsoleID.JaguarCD, ConsoleID.DS, ConsoleID.DSi, ConsoleID.AppleII,
			ConsoleID.Vectrex, ConsoleID.Tic80, ConsoleID.PCEngine, ConsoleID.Uzebox,
			ConsoleID.Nintendo3DS,
		];

		// these consoles will use part of the system bus at an offset
		private static readonly Dictionary<ConsoleID, (uint Start, uint Size)[]> UsePartialSysBus = new()
		{
			[ConsoleID.SG1000] = [ (0xC000u, 0x2000u), (0x2000u, 0x2000u), (0x8000u, 0x2000u) ],
		};

		// anything more complicated will be handled accordingly

		protected static IReadOnlyList<MemFunctions> CreateMemoryBanks(ConsoleID consoleId, IMemoryDomains domains, IDebuggable debuggable)
		{
			var mfs = new List<MemFunctions>();

			void TryAddDomain(string domain, uint? size = null, uint addressMangler = 0)
			{
				if (domains.Has(domain))
				{
					if (size.HasValue && domains[domain]!.Size < size.Value)
					{
						mfs.Add(new(domains[domain], 0, domains[domain].Size, addressMangler));
						mfs.Add(new NullMemFunctions(size.Value - domains[domain].Size));
					}
					else
					{
						mfs.Add(new(domains[domain], 0, size ?? domains[domain]!.Size, addressMangler));
					}
				}
				else if (size.HasValue)
				{
					mfs.Add(new NullMemFunctions(size.Value));
				}
			}

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
				mfs.AddRange(pairs.Select(pair => new MemFunctions(domains.SystemBus, pair.Start, pair.Size)));
			}
			else
			{
				switch (consoleId)
				{
					case ConsoleID.MegaDrive:
					case ConsoleID.Sega32X:
						mfs.Add(new(domains["68K RAM"], 0, domains["68K RAM"].Size, 1));
						TryAddDomain("32X RAM", addressMangler: 1);
						TryAddDomain("SRAM");
						break;
					case ConsoleID.MasterSystem:
					case ConsoleID.GameGear:
						TryAddDomain("Main RAM", 0x2000);
						TryAddDomain("Cart (Volatile) RAM");
						TryAddDomain("Save RAM");
						TryAddDomain("SRAM");
						break;
					case ConsoleID.PlayStation:
						mfs.Add(new(domains["MainRAM"], 0, domains["MainRAM"].Size));
						mfs.Add(new(domains["DCache"], 0, domains["DCache"].Size));
						break;
					case ConsoleID.SNES:
						mfs.Add(new(domains["WRAM"], 0, domains["WRAM"].Size));
						TryAddDomain("CARTRAM");
						// sufami B sram
						// don't think this is actually hooked up at all anyways...
						TryAddDomain("CARTRAM B"); // Snes9x
						TryAddDomain("SUFAMI TURBO B RAM"); // new BSNES
						break;
					case ConsoleID.GB:
					case ConsoleID.GBC:
						if (domains.Has("SGB CARTROM")) // old/new BSNES
						{
							// old BSNES doesn't have as many domains (hence TryAddDomain use)
							// but it should still suffice in practice
							mfs.Add(new(domains["SGB CARTROM"], 0, 0x8000));
							TryAddDomain("SGB VRAM", 0x2000);
							TryAddDomain("SGB CARTRAM", 0x2000);
							mfs.Add(new(domains["SGB WRAM"], 0, 0x2000));
							mfs.Add(new(domains["SGB WRAM"], 0, 0x1E00));
							TryAddDomain("SGB OAM", 0xA0);
							TryAddDomain("SGB System Bus", 0xE0);
							mfs.Add(new(domains["SGB HRAM"], 0, domains["SGB HRAM"].Size));
							if (domains["SGB HRAM"].Size == 0x7F)
							{
								mfs.Add(new(domains["SGB IE"], 0, domains["SGB IE"].Size));
							}
							mfs.Add(new NullMemFunctions(0x6000));
							if (domains.Has("SGB CARTRAM") && domains["SGB CARTRAM"].Size > 0x2000)
							{
								mfs.Add(new(domains["SGB CARTRAM"], 0x2000, domains["SGB CARTRAM"].Size - 0x2000));
							}
						}
						else
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
							else // Gambatte / GBHawk / SameBoy
							{
								sysBus = "System Bus";
								cartRam = "CartRAM";
								wram = "WRAM";
							}

							mfs.Add(new(domains[sysBus], 0, 0xA000));
							TryAddDomain(cartRam, 0x2000);
							mfs.Add(new(domains[wram], 0x0000, 0x2000));
							mfs.Add(new(domains[sysBus], 0xE000, 0x2000));
							mfs.Add(domains[wram].Size == 0x8000 
								? new MemFunctions(domains[wram], 0x2000, 0x6000)
								: new NullMemFunctions(0x6000));
							if (domains.Has(cartRam) && domains[cartRam].Size > 0x2000)
							{
								mfs.Add(new(domains[cartRam], 0x2000, domains[cartRam].Size - 0x2000));
							}
						}
						break;
					case ConsoleID.GBA:
						mfs.Add(new(domains["IWRAM"], 0, domains["IWRAM"].Size));
						mfs.Add(new(domains["EWRAM"], 0, domains["EWRAM"].Size));
						mfs.Add(new(domains["SRAM"], 0, domains["SRAM"].Size));
						break;
					case ConsoleID.SegaCD:
						mfs.Add(new(domains["68K RAM"], 0, domains["68K RAM"].Size, 1));
						mfs.Add(new(domains["CD PRG RAM"], 0, domains["CD PRG RAM"].Size, 1));
						break;
					case ConsoleID.MagnavoxOdyssey:
						mfs.Add(new(domains["CPU RAM"], 0, domains["CPU RAM"].Size));
						mfs.Add(new(domains["Main RAM"], 0, domains["Main RAM"].Size));
						break;
					case ConsoleID.Atari2600:
						mfs.Add(new(domains["Main RAM"], 0, domains["Main RAM"].Size));
						break;
					case ConsoleID.VirtualBoy:
						// todo: add System Bus so this isn't needed
						mfs.Add(new(domains["WRAM"], 0, domains["WRAM"].Size));
						mfs.Add(new(domains["CARTRAM"], 0, domains["CARTRAM"].Size));
						break;
					case ConsoleID.MSX:
						// no, can't use MainMemory here, as System Bus is that due to init ordering
						// todo: make this MainMemory
						mfs.Add(new(domains["RAM"], 0, domains["RAM"].Size));
						break;
					case ConsoleID.AppleII:
						mfs.Add(new(domains["Main RAM"], 0, domains["Main RAM"].Size));
						mfs.Add(new(domains["Auxiliary RAM"], 0, domains["Auxiliary RAM"].Size));
						break;
					case ConsoleID.Saturn:
						// todo: add System Bus so this isn't needed
						mfs.Add(new(domains["Work Ram Low"], 0, domains["Work Ram Low"].Size, 1));
						mfs.Add(new(domains["Work Ram High"], 0, domains["Work Ram High"].Size, 1));
						break;
					case ConsoleID.Colecovision:
						mfs.Add(new(domains["Main RAM"], 0, domains["Main RAM"].Size));
						TryAddDomain("SGM Low RAM");
						TryAddDomain("SGM High RAM");
						break;
					case ConsoleID.Intellivision:
						// special case
						mfs.Add(new NullMemFunctions(0x80));
						mfs.Add(new IntelliMemFunctions(domains.SystemBus));
						break;
					case ConsoleID.PCFX:
						// todo: add System Bus so this isn't needed
						mfs.Add(new(domains["Main RAM"], 0, domains["Main RAM"].Size));
						mfs.Add(new(domains["Backup RAM"], 0, domains["Backup RAM"].Size));
						mfs.Add(new(domains["Extra Backup RAM"], 0, domains["Extra Backup RAM"].Size));
						break;
					case ConsoleID.WonderSwan:
						mfs.Add(new(domains["RAM"], 0, domains["RAM"].Size));
						TryAddDomain("SRAM");
						TryAddDomain("EEPROM");
						break;
					case ConsoleID.FairchildChannelF:
						// special case
						mfs.Add(new ChanFMemFunctions(debuggable, domains["VRAM"]));
						mfs.Add(new(domains.SystemBus, 0, domains.SystemBus.Size));
						break;
					case ConsoleID.PCEngineCD:
						mfs.Add(new(domains["System Bus (21 bit)"], 0x1F0000, 0x2000));
						mfs.Add(new(domains["System Bus (21 bit)"], 0x100000, 0x10000));
						mfs.Add(new(domains["System Bus (21 bit)"], 0xD0000, 0x30000));
						mfs.Add(new(domains["System Bus (21 bit)"], 0x1EE000, 0x800));
						break;
					case ConsoleID.N64:
						mfs.Add(new(domains.MainMemory, 0, domains.MainMemory.Size, 3));
						break;
					case ConsoleID.Arcade:
						mfs.AddRange(domains.Where(domain => domain.Name.Contains("ram"))
							.Select(domain => new MemFunctions(domain, 0, domain.Size)));
						break;
					case ConsoleID.TI83:
						TryAddDomain("RAM"); // Emu83
						TryAddDomain("Main RAM"); // TI83Hawk
						break;
					case ConsoleID.UnknownConsoleID:
					case ConsoleID.ZXSpectrum: // this doesn't actually have anything standardized, so...
					default:
						mfs.Add(new(domains.MainMemory, 0, domains.MainMemory.Size));
						break;
				}
			}

			return mfs.AsReadOnly();
		}
	}
}
