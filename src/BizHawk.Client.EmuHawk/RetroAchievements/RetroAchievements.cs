using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Atari.Atari2600;
using BizHawk.Emulation.Cores.Computers.MSX;
using BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Consoles.O2Hawk;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;
using BizHawk.Emulation.Cores.Consoles.Sega.PicoDrive;
using BizHawk.Emulation.Cores.Nintendo.BSNES;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.GBA;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.Sameboy;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.Nintendo.SNES9X;
using BizHawk.Emulation.Cores.Nintendo.SubGBHawk;
using BizHawk.Emulation.Cores.Nintendo.SubNESHawk;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Sega.MasterSystem;
using BizHawk.Emulation.Cores.Waterbox;
using BizHawk.Emulation.Cores.WonderSwan;
using BizHawk.Emulation.DiscSystem;

// TODO: Auto-update dll system

namespace BizHawk.Client.EmuHawk
{
	public class RetroAchievements
	{
		private static readonly RAInterface RA;

		private static readonly string _version;

		public static bool IsAvailable => RA != null;
		
		static RetroAchievements()
		{
			try
			{
				if (OSTailoredCode.IsUnixHost)
				{
					throw new NotSupportedException("RetroAchivements is Windows only!");
				}

				var resolver = new DynamicLibraryImportResolver("RA_Integration.dll", hasLimitedLifetime: false);
				RA = BizInvoker.GetInvoker<RAInterface>(resolver, CallingConventionAdapters.Native);
				_version = Marshal.PtrToStringAnsi(RA.IntegrationVersion());
				Console.WriteLine($"Loaded RetroAchievements v{_version}");
			}
			catch
			{
				RA = null;
				_version = "0.0";
			}
		}

		private bool AllGamesVerified { get; set; }

		private readonly MainForm _mainForm; // todo: encapsulate MainForm in an interface
		private readonly InputManager _inputManager;

		private IEmulator Emu => _mainForm.Emulator;
		private IMemoryDomains Domains => _mainForm.Emulator.AsMemoryDomains();
		private IGameInfo Game => _mainForm.Game;
		private IMovieSession MovieSession => _mainForm.MovieSession;
		private Config Config => _mainForm.Config;
		private ToolManager Tools => _mainForm.Tools;

		private readonly RAInterface.IsActiveDelegate _isActive;
		private readonly RAInterface.UnpauseDelegate _unpause;
		private readonly RAInterface.PauseDelegate _pause;
		private readonly RAInterface.RebuildMenuDelegate _rebuildMenu;
		private readonly RAInterface.EstimateTitleDelegate _estimateTitle;
		private readonly RAInterface.ResetEmulatorDelegate _resetEmulator;
		private readonly RAInterface.LoadROMDelegate _loadROM;

		private class MemFunctions
		{
			private readonly MemoryDomain _domain;
			private readonly int _domainAddrStart; // addr of _domain where bank begins

			public readonly RAInterface.ReadMemoryFunc ReadFunc;
			public readonly RAInterface.WriteMemoryFunc WriteFunc;
			public readonly RAInterface.ReadMemoryBlockFunc ReadBlockFunc;
			public readonly int BankSize;

			protected virtual int FixAddr(int addr)
				=> _domainAddrStart + addr;

			protected virtual byte ReadMem(int addr)
				=> _domain.PeekByte(FixAddr(addr));

			protected virtual void WriteMem(int addr, byte val)
				=> _domain.PokeByte(FixAddr(addr), val);

			protected virtual int ReadMemBlock(int addr, IntPtr buffer, int bytes)
			{
				addr = FixAddr(addr);

				if (addr >= (_domainAddrStart + BankSize))
				{
					return 0;
				}

				var end = Math.Min(addr + bytes, _domainAddrStart + BankSize);
				var ret = new byte[end - addr];
				_domain.BulkPeekByte(((long)addr).RangeToExclusive(end), ret);
				Marshal.Copy(ret, 0, buffer, end - addr);
				return end - addr;
			}
			
			public MemFunctions(MemoryDomain domain, int domainAddrStart, long bankSize)
			{
				_domain = domain;
				_domainAddrStart = domainAddrStart;

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

		private class IntelliMemFunctions : MemFunctions
		{
			protected override int FixAddr(int addr)
				=> (addr - 0x80) / 2;

			// a little dangerous, but don't care enough to do it properly
			public IntelliMemFunctions(MemoryDomain domain)
				: base(domain, 0, 0x40080)
			{
			}
		}

		private class ChanFMemFunctions : MemFunctions
		{
			private readonly IMemoryDomains _domains;
			private readonly IDebuggable _debuggable;

			protected override byte ReadMem(int addr)
			{
				if (addr < 0x40)
				{
					return (byte)_debuggable.GetCpuFlagsAndRegisters()["SPR" + addr].Value;
				}	
				else if (addr < 0x840)
				{
					addr -= 0x40;
					return (byte)(((_domains["VRAM"].PeekByte(addr * 4 + 0) & 3) << 6)
						| ((_domains["VRAM"].PeekByte(addr * 4 + 1) & 3) << 4)
						| ((_domains["VRAM"].PeekByte(addr * 4 + 2) & 3) << 2)
						| ((_domains["VRAM"].PeekByte(addr * 4 + 3) & 3) << 0));
				}
				else if (addr < 0x10840)
				{
					addr -= 0x840;
					return _domains.SystemBus.PeekByte(addr);
				}
				else if (addr < 0x10C40)
				{
					addr -= 0x10840;
					// this should be Mazes's RAM, but that isn't exposed for us
					return 0;
				}

				return 0;
			}

			protected override void WriteMem(int addr, byte val)
			{
				if (addr < 0x40)
				{
					_debuggable.SetCpuRegister("SPR" + addr, val);
				}
				else if (addr < 0x840)
				{
					addr -= 0x40;
					_domains["VRAM"].PokeByte(addr * 4 + 0, (byte)((val >> 6) & 3));
					_domains["VRAM"].PokeByte(addr * 4 + 1, (byte)((val >> 4) & 3));
					_domains["VRAM"].PokeByte(addr * 4 + 2, (byte)((val >> 2) & 3));
					_domains["VRAM"].PokeByte(addr * 4 + 3, (byte)((val >> 0) & 3));
				}
				else if (addr < 0x10840)
				{
					addr -= 0x840;
					_domains.SystemBus.PokeByte(addr, val);
				}
				else if (addr < 0x10C40)
				{
					addr -= 0x10840;
					// this should be Mazes's RAM, but that isn't exposed for us
				}
			}

			protected override int ReadMemBlock(int addr, IntPtr buffer, int bytes)
			{
				if (addr >= BankSize)
				{
					return 0;
				}

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
						val = ReadMem(i);
					}

					unsafe
					{
						((byte*)buffer)[i - addr] = val;
					}
				}
				return end - addr;
			}

			public ChanFMemFunctions(IMemoryDomains domains)
				: base(null, 0, 0x10C40)
			{
				_domains = domains;
				_debuggable = (IDebuggable)domains;
			}
		}

		private IReadOnlyList<MemFunctions> _memFunctions;

		private readonly Func<ToolStripItemCollection> _getRADropDownItems;
		private readonly RAInterface.MenuItem[] _menuItems = new RAInterface.MenuItem[40];

		private readonly Action _shutdownRACallback;

		private void RebuildMenu()
		{
			var numItems = RA.GetPopupMenuItems(_menuItems);
			var tsmiddi = _getRADropDownItems();
			tsmiddi.Clear();
			{
				var tsi = new ToolStripMenuItem("Shutdown RetroAchievements");
				tsi.Click += (_, _) =>
				{
					RA.Shutdown();
					_shutdownRACallback();
				};
				tsmiddi.Add(tsi);
				var tss = new ToolStripSeparator();
				tsmiddi.Add(tss);
			}
			for (int i = 0; i < numItems; i++)
			{
				if (_menuItems[i].Label != IntPtr.Zero)
				{
					var tsi = new ToolStripMenuItem(Marshal.PtrToStringUni(_menuItems[i].Label))
					{
						Checked = _menuItems[i].Checked != 0,
					};
					var id = _menuItems[i].ID;
					tsi.Click += (_, _) =>
					{
						RA.InvokeDialog(id);
						// ditto here
						_mainForm.UpdateWindowTitle();
					};
					tsmiddi.Add(tsi);
				}
				else
				{
					var tss = new ToolStripSeparator();
					tsmiddi.Add(tss);
				}
			}
		}

		private RAInterface.ConsoleID SysIdToRAId()
		{
			return Emu.SystemId switch
			{
				VSystemID.Raw.A26 => RAInterface.ConsoleID.Atari2600,
				VSystemID.Raw.A78 => RAInterface.ConsoleID.Atari7800,
				VSystemID.Raw.Amiga => RAInterface.ConsoleID.Amiga,
				VSystemID.Raw.AmstradCPC => RAInterface.ConsoleID.AmstradCPC,
				VSystemID.Raw.AppleII => RAInterface.ConsoleID.AppleII,
				VSystemID.Raw.C64 => RAInterface.ConsoleID.C64,
				VSystemID.Raw.ChannelF => RAInterface.ConsoleID.FairchildChannelF,
				VSystemID.Raw.Coleco => RAInterface.ConsoleID.Colecovision,
				VSystemID.Raw.DEBUG => RAInterface.ConsoleID.UnknownConsoleID,
				VSystemID.Raw.Dreamcast => RAInterface.ConsoleID.Dreamcast,
				VSystemID.Raw.GameCube => RAInterface.ConsoleID.GameCube,
				VSystemID.Raw.GB when Emu is IGameboyCommon gb => gb.IsCGBMode() ? RAInterface.ConsoleID.GBC : RAInterface.ConsoleID.GB,
				VSystemID.Raw.GBA => RAInterface.ConsoleID.GBA,
				VSystemID.Raw.GBC => RAInterface.ConsoleID.GBC, // Not actually used
				VSystemID.Raw.GBL => RAInterface.ConsoleID.GB, // actually can be a mix of GB and GBC
				VSystemID.Raw.GEN when Emu is GPGX gpgx => gpgx.IsMegaCD ? RAInterface.ConsoleID.SegaCD : RAInterface.ConsoleID.MegaDrive,
				VSystemID.Raw.GEN when Emu is PicoDrive pico => pico.Is32XActive ? RAInterface.ConsoleID.Sega32X : RAInterface.ConsoleID.MegaDrive,
				VSystemID.Raw.GG => RAInterface.ConsoleID.GameGear,
				VSystemID.Raw.GGL => RAInterface.ConsoleID.GameGear, // ???
				VSystemID.Raw.INTV => RAInterface.ConsoleID.Intellivision,
				VSystemID.Raw.Jaguar => RAInterface.ConsoleID.Jaguar, // Jaguar CD?
				VSystemID.Raw.Libretro => RAInterface.ConsoleID.UnknownConsoleID,
				VSystemID.Raw.Lynx => RAInterface.ConsoleID.Lynx,
				VSystemID.Raw.MAME => RAInterface.ConsoleID.Arcade,
				VSystemID.Raw.MSX => RAInterface.ConsoleID.MSX,
				VSystemID.Raw.N64 => RAInterface.ConsoleID.N64,
				VSystemID.Raw.NDS => RAInterface.ConsoleID.DS,
				VSystemID.Raw.NeoGeoCD => RAInterface.ConsoleID.NeoGeoCD,
				VSystemID.Raw.NES => RAInterface.ConsoleID.NES,
				VSystemID.Raw.NGP => RAInterface.ConsoleID.NeoGeoPocket,
				VSystemID.Raw.NULL => RAInterface.ConsoleID.UnknownConsoleID,
				VSystemID.Raw.O2 => RAInterface.ConsoleID.MagnavoxOdyssey,
				VSystemID.Raw.Panasonic3DO => RAInterface.ConsoleID.ThreeDO,
				VSystemID.Raw.PCE => RAInterface.ConsoleID.PCEngine,
				VSystemID.Raw.PCECD => RAInterface.ConsoleID.PCEngineCD,
				VSystemID.Raw.PCFX => RAInterface.ConsoleID.PCFX,
				VSystemID.Raw.PhillipsCDi => RAInterface.ConsoleID.CDi,
				VSystemID.Raw.Playdia => RAInterface.ConsoleID.UnknownConsoleID,
				VSystemID.Raw.PS2 => RAInterface.ConsoleID.PlayStation2,
				VSystemID.Raw.PSP => RAInterface.ConsoleID.PSP,
				VSystemID.Raw.PSX => RAInterface.ConsoleID.PlayStation,
				VSystemID.Raw.SAT => RAInterface.ConsoleID.Saturn,
				VSystemID.Raw.Sega32X => RAInterface.ConsoleID.Sega32X, // not actually used
				VSystemID.Raw.SG => RAInterface.ConsoleID.SG1000,
				VSystemID.Raw.SGB => RAInterface.ConsoleID.GB, // ???
				VSystemID.Raw.SGX => RAInterface.ConsoleID.PCEngine, // ???
				VSystemID.Raw.SGXCD => RAInterface.ConsoleID.PCEngineCD, // ???
				VSystemID.Raw.SMS => RAInterface.ConsoleID.MasterSystem,
				VSystemID.Raw.SNES => RAInterface.ConsoleID.SNES, // Check for SGB?
				VSystemID.Raw.TI83 => RAInterface.ConsoleID.UnknownConsoleID,
				VSystemID.Raw.TIC80 => RAInterface.ConsoleID.Tic80,
				VSystemID.Raw.UZE => RAInterface.ConsoleID.UnknownConsoleID,
				VSystemID.Raw.VB => RAInterface.ConsoleID.VirtualBoy,
				VSystemID.Raw.VEC => RAInterface.ConsoleID.Vectrex,
				VSystemID.Raw.Wii => RAInterface.ConsoleID.WII,
				VSystemID.Raw.WSWAN => RAInterface.ConsoleID.WonderSwan,
				VSystemID.Raw.ZXSpectrum => RAInterface.ConsoleID.ZXSpectrum,
				_ => RAInterface.ConsoleID.UnknownConsoleID,
			};
		}

		public RetroAchievements(MainForm mainForm, InputManager inputManager, Func<ToolStripItemCollection> getRADropDownItems, Action shutdownRACallback)
		{
			_mainForm = mainForm;
			_inputManager = inputManager;
			_getRADropDownItems = getRADropDownItems;
			_shutdownRACallback = shutdownRACallback;

			RA.InitClient(_mainForm.Handle, "BizHawk", VersionInfo.GetEmuVersion());

			_isActive = () => !Emu.IsNull();
			_unpause = () => _mainForm.UnpauseEmulator();
			_pause = () => _mainForm.PauseEmulator();
			_rebuildMenu = RebuildMenu;
			_estimateTitle = buffer =>
			{
				var name = Encoding.UTF8.GetBytes(Game?.Name ?? "No Game Info Available");
				Marshal.Copy(name, 0, buffer, Math.Min(name.Length, 256));
			};
			_resetEmulator = () => _mainForm.RebootCore();
			_loadROM = path => _mainForm.LoadRom(path, new LoadRomArgs { OpenAdvanced = OpenAdvancedSerializer.ParseWithLegacy(path) });

			RA.InstallSharedFunctionsExt(_isActive, _unpause, _pause, _rebuildMenu, _estimateTitle, _resetEmulator, _loadROM);

			RA.AttemptLogin(true);
		}

		public void OnSaveState(string path)
			=> RA.OnSaveState(path);

		public void OnLoadState(string path)
		{
			if (RA.HardcoreModeIsActive())
			{
				HandleHardcoreModeDisable("Loading savestates is not allowed in hardcore mode.");
			}

			RA.OnLoadState(path);
		}

		public void Restart()
		{
			var consoleId = SysIdToRAId();
			RA.SetConsoleID(consoleId);

			RA.ClearMemoryBanks();

			if (Emu.HasMemoryDomains())
			{
				_memFunctions = CreateMemoryBanks(consoleId, Domains);

				for (int i = 0; i < _memFunctions.Count; i++)
				{
					RA.InstallMemoryBank(i, _memFunctions[i].ReadFunc, _memFunctions[i].WriteFunc, _memFunctions[i].BankSize);
					RA.InstallMemoryBankBlockReader(i, _memFunctions[i].ReadBlockFunc);
				}
			}

			AllGamesVerified = true;

			if (_mainForm.CurrentlyOpenRomArgs is not null)
			{
				var ids = GetRAGameIds(_mainForm.CurrentlyOpenRomArgs.OpenAdvanced, consoleId);

				AllGamesVerified = !ids.Contains(0);

				if (ids.Count > 0 && ids[0] != 0)
				{
					RA.ActivateGame(ids[0]);
				}
			}

			CheckHardcoreModeConditions();
			RebuildMenu();

			// workaround a bug in RA which will cause the window title to be changed despite us not calling UpdateAppTitle
			_mainForm.UpdateWindowTitle();
		}

		// "Hardcore Mode" is a mode intended for RA's leaderboard, and places various restrictions on the emulator
		// To keep changes outside this file minimal, we'll simply check if any problematic condition arises and disable hardcore mode
		// todo: is this really simpler?

		private static readonly Type[] HardcoreProhibitedTools = new[]
		{
			typeof(LuaConsole), typeof(RamWatch), typeof(RamSearch),
			typeof(GameShark), typeof(SNESGraphicsDebugger), typeof(PceBgViewer),
			typeof(PceTileViewer), typeof(GenVdpViewer), typeof(SmsVdpViewer),
			typeof(PCESoundDebugger), typeof(MacroInputTool), typeof(GenericDebugger),
			typeof(NESNameTableViewer), typeof(TraceLogger), typeof(CDL),
			typeof(Cheats), typeof(NesPPU), typeof(GbaGpuView),
			typeof(GbGpuView), typeof(BasicBot), typeof(HexEditor),
			typeof(TAStudio),
		};

		private static readonly Dictionary<Type, string[]> CoreGraphicsLayers = new()
		{
			[typeof(MSX)] = new[] { "DispBG", "DispOBJ" },
			[typeof(Atari2600)] = new[] { "ShowBG", "ShowPlayer1", "ShowPlayer2", "ShowMissle1", "ShowMissle2", "ShowBall", "ShowPlayfield" },
			[typeof(O2Hawk)] = new[] { "Show_Chars", "Show_Quads", "Show_Sprites", "Show_G7400_Sprites", "Show_G7400_BG" },
			[typeof(BsnesCore)] = new[] { "ShowBG1_0", "ShowBG2_0", "ShowBG3_0", "ShowBG4_0", "ShowBG1_1", "ShowBG2_1", "ShowBG3_1", "ShowBG4_1", "ShowOBJ_0", "ShowOBJ_1", "ShowOBJ_2", "ShowOBJ_3" },
			[typeof(MGBAHawk)] = new[] { "DisplayBG0", "DisplayBG1", "DisplayBG2", "DisplayBG3", "DisplayOBJ" },
			[typeof(NES)] = new[] { "DispBackground", "DispSprites" },
			[typeof(Sameboy)] = new[] { "EnableBGWIN", "EnableOBJ" },
			[typeof(LibsnesCore)] = new[] { "ShowBG1_0", "ShowBG2_0", "ShowBG3_0", "ShowBG4_0", "ShowBG1_1", "ShowBG2_1", "ShowBG3_1", "ShowBG4_1", "ShowOBJ_0", "ShowOBJ_1", "ShowOBJ_2", "ShowOBJ_3" },
			[typeof(Snes9x)] = new[] { "ShowBg0", "ShowBg1", "ShowBg2", "ShowBg3", "ShowSprites0", "ShowSprites1", "ShowSprites2", "ShowSprites3", "ShowWindow", "ShowTransparency" },
			[typeof(PCEngine)] = new[] { "ShowBG1", "ShowOBJ1", "ShowBG2", "ShowOBJ2", },
			[typeof(GPGX)] = new[] { "DrawBGA", "DrawBGB", "DrawBGW", "DrawObj", },
			[typeof(SMS)] = new[] { "DispBG", "DispOBJ," },
			[typeof(WonderSwan)] = new[] { "EnableBG", "EnableFG", "EnableSprites", },
		};

		private void HandleHardcoreModeDisable(string reason)
		{
			_mainForm.ModalMessageBox($"{reason} Disabling hardcore mode.", "Warning", EMsgBoxIcon.Warning);
			RA.WarnDisableHardcore(null);
		}

		public void CheckHardcoreModeConditions()
		{
			if (RA.HardcoreModeIsActive())
			{
				if (!AllGamesVerified)
				{
					HandleHardcoreModeDisable("All loaded games were not verified.");
					return;
				}

				if (MovieSession.Movie.IsPlaying())
				{
					HandleHardcoreModeDisable("Playing a movie while in hardcore mode is not allowed.");
					return;
				}

				if (_inputManager.ClientControls["Frame Advance"])
				{
					HandleHardcoreModeDisable("Frame advancing in hardcore mode is not allowed.");
					return;
				}

				if (_mainForm.Rewinder?.Active == true)
				{
					HandleHardcoreModeDisable("Enabling Rewind in hardcore mode is not allowed.");
					return;
				}

				var fastForward = _inputManager.ClientControls["Fast Forward"] || _mainForm.FastForward;
				var speedPercent = fastForward ? Config?.SpeedPercentAlternate : Config?.SpeedPercent;
				if ((speedPercent ?? 100) < 100)
				{
					HandleHardcoreModeDisable("Slow motion in hardcore mode is not allowed.");
					return;
				}

				foreach (var t in HardcoreProhibitedTools)
				{
					if (Tools.IsLoaded(t))
					{
						HandleHardcoreModeDisable($"Using {t.Name} in hardcore mode is not allowed.");
						return;
					}
				}

				// can't know what external tools are doing, so just don't allow them here
				if (Tools.IsLoaded<IExternalToolForm>())
				{
					HandleHardcoreModeDisable($"Using external tools in hardcore mode is not allowed.");
					return;
				}

				if (Emu is SubNESHawk or SubBsnesCore or SubGBHawk)
				{
					// this is mostly due to wonkiness with subframes which can be used as pseudo slowdown
					HandleHardcoreModeDisable($"Using subframes in hardcore mode is not allowed.");
					return;
				}
				else if (Emu is NymaCore nyma)
				{
					if (nyma.GetSettings().DisabledLayers.Any())
					{
						HandleHardcoreModeDisable($"Disabling {Emu.GetType().Name}'s graphics layers in hardcore mode is not allowed.");
						return;
					}
				}
				else if (Emu is GambatteLink gl)
				{
					foreach (var ss in gl.GetSyncSettings()._linkedSyncSettings)
					{
						if (!ss.DisplayBG || !ss.DisplayOBJ || !ss.DisplayWindow)
						{
							HandleHardcoreModeDisable($"Disabling GambatteLink's graphics layers in hardcore mode is not allowed.");
							return;
						}
					}
				}
				else if (Emu is Gameboy gb)
				{
					var ss = gb.GetSyncSettings();
					if (!ss.DisplayBG || !ss.DisplayOBJ || !ss.DisplayWindow)
					{
						HandleHardcoreModeDisable($"Disabling Gambatte's graphics layers in hardcore mode is not allowed.");
						return;
					}
					if (ss.FrameLength is Gameboy.GambatteSyncSettings.FrameLengthType.UserDefinedFrames)
					{
						HandleHardcoreModeDisable($"Using subframes in hardcore mode is not allowed.");
						return;
					}
				}
				else if (CoreGraphicsLayers.TryGetValue(Emu.GetType(), out var layers))
				{
					var s = _mainForm.GetSettingsAdapterForLoadedCoreUntyped().GetSettings();
					var t = s.GetType();
					foreach (var layer in layers)
					{
						// annoyingly NES has fields instead of properties for layers
						if (!(bool)(t.GetProperty(layer)?.GetValue(s) ?? t.GetField(layer).GetValue(s)))
						{
							HandleHardcoreModeDisable($"Disabling {Emu.GetType().Name}'s {layer} in hardcore mode is not allowed.");
							return;
						}
					}
				}
			}
		}

		public void Update()
		{
			var input = _inputManager.ControllerOutput;
			foreach (var resetButton in input.Definition.BoolButtons.Where(b => b.Contains("Power") || b.Contains("Reset")))
			{
				if (input.IsPressed(resetButton))
				{
					RA.OnReset();
					break;
				}
			}

			if (_inputManager.ClientControls["Open RA Overlay"])
			{
				RA.SetPaused(true);
			}

			if (RA.IsOverlayFullyVisible())
			{
				var ci = new RAInterface.ControllerInput
				{
					UpPressed = _inputManager.ClientControls["RA Up"],
					DownPressed = _inputManager.ClientControls["RA Down"],
					LeftPressed = _inputManager.ClientControls["RA Left"],
					RightPressed = _inputManager.ClientControls["RA Right"],
					ConfirmPressed = _inputManager.ClientControls["RA Confirm"],
					CancelPressed = _inputManager.ClientControls["RA Cancel"],
					QuitPressed = _inputManager.ClientControls["RA Quit"],
				};

				RA.NavigateOverlay(ref ci);

				// todo: suppress user inputs with overlay active?
			}

			RA.DoAchievementsFrame();
		}

		// these consoles will use the entire system bus
		private static readonly RAInterface.ConsoleID[] UseFullSysBus = new[]
		{
			RAInterface.ConsoleID.GB, RAInterface.ConsoleID.NES,
			RAInterface.ConsoleID.C64, RAInterface.ConsoleID.AmstradCPC,
			RAInterface.ConsoleID.Atari7800,
		};

		// these consoles will use the entire main memory domain
		private static readonly RAInterface.ConsoleID[] UseFullMainMem = new[]
		{
			RAInterface.ConsoleID.N64, RAInterface.ConsoleID.PlayStation,
			RAInterface.ConsoleID.Lynx, RAInterface.ConsoleID.Lynx,
			RAInterface.ConsoleID.NeoGeoPocket, RAInterface.ConsoleID.Jaguar,
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

		// anything more complicated will be handled accordingly

		private static IReadOnlyList<MemFunctions> CreateMemoryBanks(
			RAInterface.ConsoleID consoleId, IMemoryDomains domains)
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
						// no System Bus on PicoDrive, so let's do this
						// todo: add System Bus to PicoDrive so this isn't needed
						mfs.Add(new(domains["68K RAM"], 0, domains["68K RAM"].Size));
						if (domains.Has("SRAM"))
						{
							mfs.Add(new(domains["SRAM"], 0, domains["SRAM"].Size));
						}
						break;
					case RAInterface.ConsoleID.SNES:
						// no System Bus on Faust & Snes9x, so let's do this
						// todo: add System Bus to Faust & Snes9x so this isn't needed
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
					case RAInterface.ConsoleID.GBC:
						mfs.Add(new(domains.SystemBus, 0, domains.SystemBus.Size));
						mfs.Add(new(domains["WRAM"], 0x2000, 0x6000));
						break;
					case RAInterface.ConsoleID.SegaCD:
						mfs.Add(new(domains["68K RAM"], 0, domains["68K RAM"].Size));
						mfs.Add(new(domains["CD PRG RAM"], 0, domains["CD PRG RAM"].Size));
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
						mfs.Add(new ChanFMemFunctions(domains));
						break;
					case RAInterface.ConsoleID.PCEngineCD:
						mfs.Add(new(domains["SystemBus (21 bit)"], 0x1F0000, 0x2000));
						mfs.Add(new(domains["SystemBus (21 bit)"], 0x100000, 0x10000));
						mfs.Add(new(domains["SystemBus (21 bit)"], 0xD0000, 0x30000));
						mfs.Add(new(domains["SystemBus (21 bit)"], 0x1EE000, 0x800));
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

		private static IReadOnlyList<int> GetRAGameIds(IOpenAdvanced ioa, RAInterface.ConsoleID consoleID)
		{
			var ret = new List<int>();
			switch (ioa.TypeName)
			{
				case OpenAdvancedTypes.OpenRom:
					{
						var ext = Path.GetExtension(Path.GetExtension(ioa.SimplePath.Replace("|", "")).ToLowerInvariant());

						static int HashDisc(string path, RAInterface.ConsoleID consoleID)
						{
							// this shouldn't throw in practice, this is only called when loading was succesful!
							using var disc = DiscExtensions.CreateAnyType(path, e => throw new Exception(e));
							var dsr = new DiscSectorReader(disc)
							{
								Policy = { DeterministicClearBuffer = false } // let's make this a little faster
							};

							var buf2048 = new byte[2048];
							var buffer = new List<byte>();

							switch (consoleID)
							{
								case RAInterface.ConsoleID.PCEngineCD:
									{
										dsr.ReadLBA_2048(1, buf2048, 0);
										buffer.AddRange(new ArraySegment<byte>(buf2048, 128 - 22, 22));
										var bootSector = (buf2048[2] << 16) | (buf2048[1] << 8) | buf2048[0];
										var numSectors = buf2048[3];
										for (int i = 0; i < numSectors; i++)
										{
											dsr.ReadLBA_2048(bootSector + i, buf2048, 0);
											buffer.AddRange(buf2048);
										}
										break;
									}
								case RAInterface.ConsoleID.PCFX:
									{
										dsr.ReadLBA_2048(1, buf2048, 0);
										buffer.AddRange(new ArraySegment<byte>(buf2048, 0, 128));
										var bootSector = (buf2048[35] << 24) | (buf2048[34] << 16) | (buf2048[33] << 8) | buf2048[32];
										var numSectors = (buf2048[39] << 24) | (buf2048[38] << 16) | (buf2048[37] << 8) | buf2048[36];
										for (int i = 0; i < numSectors; i++)
										{
											dsr.ReadLBA_2048(bootSector + i, buf2048, 0);
											buffer.AddRange(buf2048);
										}
										break;
									}
								case RAInterface.ConsoleID.PlayStation:
									{
										int GetFileSector(string filename, out int filesize)
										{
											dsr.ReadLBA_2048(16, buf2048, 0);
											var sector = (buf2048[160] << 16) | (buf2048[159] << 8) | buf2048[158];
											dsr.ReadLBA_2048(sector, buf2048, 0);
											var index = 0;
											while ((index + 33 + filename.Length) < 2048)
											{
												var term = buf2048[index + 33 + filename.Length];
												if (term == ';' || term == '\0')
												{
													var fn = Encoding.ASCII.GetString(buf2048, index + 33, filename.Length);
													if (filename == fn)
													{
														filesize = (buf2048[index + 13] << 24) | (buf2048[index + 12] << 16) | (buf2048[index + 11] << 8) | buf2048[index + 10];
														return (buf2048[index + 4] << 16) | (buf2048[index + 3] << 8) | buf2048[index + 2];
													}
												}
												index += buf2048[index];
											}

											filesize = 0;
											return -1;
										}

										string exePath = "PSX.EXE";

										// find SYSTEM.CNF sector
										var sector = GetFileSector("SYSTEM.CNF", out _);
										if (sector > 0)
										{
											// read SYSTEM.CNF sector
											dsr.ReadLBA_2048(sector, buf2048, 0);
											exePath = Encoding.ASCII.GetString(buf2048);

											// "BOOT = cdrom:\" precedes the path
											var index = exePath.IndexOf("BOOT = cdrom:\\");
											if (index < 0) break;
											exePath = exePath.Remove(0, index + 14);

											// end of the path has ;
											var end = exePath.IndexOf(';');
											if (end < 0) break;
											exePath = exePath.Substring(0, end);
										}

										buffer.AddRange(Encoding.ASCII.GetBytes(exePath));

										// get the filename
										// valid too if -1, as that means we already have the filename
										var start = exePath.LastIndexOf('\\');
										if (start > 0)
										{
											exePath = exePath.Remove(0, start + 1);
										}

										// get sector for exe
										sector = GetFileSector(exePath, out var exeSize);
										if (sector < 0) break;

										dsr.ReadLBA_2048(sector++, buf2048, 0);

										if ("PS-X EXE" == Encoding.ASCII.GetString(buf2048, 0, 8))
										{
											exeSize = (buf2048[31] << 24) | (buf2048[30] << 16) | (buf2048[29] << 8) | buf2048[28] + 2048;
										}

										buffer.AddRange(new ArraySegment<byte>(buf2048, 0, Math.Min(exeSize, 2048)));
										exeSize -= 2048;

										while (exeSize > 0)
										{
											dsr.ReadLBA_2048(sector++, buf2048, 0);
											buffer.AddRange(new ArraySegment<byte>(buf2048, 0, Math.Min(exeSize, 2048)));
											exeSize -= 2048;
										}

										break;
									}
								case RAInterface.ConsoleID.SegaCD:
								case RAInterface.ConsoleID.Saturn:
									dsr.ReadLBA_2048(0, buf2048, 0);
									buffer.AddRange(new ArraySegment<byte>(buf2048, 0, 512));
									break;
							}

							var hash = MD5Checksum.ComputeDigestHex(buffer.ToArray());
							return RA.IdentifyHash(hash);
						}

						if (ext == ".xml")
						{
							var xml = XmlGame.Create(new HawkFile(ioa.SimplePath));
							foreach (var kvp in xml.Assets)
							{
								if (Disc.IsValidExtension(Path.GetExtension(kvp.Key)))
								{
									ret.Add(HashDisc(kvp.Key, consoleID));
								}
								else
								{
									ret.Add(RA.IdentifyRom(kvp.Value, kvp.Value.Length));
								}
							}
						}
						else
						{
							if (Disc.IsValidExtension(Path.GetExtension(ext)))
							{
								ret.Add(HashDisc(ioa.SimplePath, consoleID));
							}
							else
							{
								using var file = new HawkFile(ioa.SimplePath);
								var rom = file.ReadAllBytes();
								ret.Add(RA.IdentifyRom(rom, rom.Length));
							}
						}
						break;
					}
				case OpenAdvancedTypes.MAME:
					{
						// Arcade wants to just hash the filename (with no extension)
						var name = Encoding.UTF8.GetBytes(Path.GetFileNameWithoutExtension(ioa.SimplePath));
						var hash = MD5Checksum.ComputeDigestHex(name);
						ret.Add(RA.IdentifyHash(hash));
						break;
					}
				case OpenAdvancedTypes.LibretroNoGame:
					// nothing to hash here
					break;
				case OpenAdvancedTypes.Libretro:
					{
						// can't know what's here exactly, so we'll just hash the entire thing
						using var file = new HawkFile(ioa.SimplePath);
						var rom = file.ReadAllBytes();
						ret.Add(RA.IdentifyRom(rom, rom.Length));
						break;
					}
			}

			return ret.AsReadOnly();
		}
	}
}
