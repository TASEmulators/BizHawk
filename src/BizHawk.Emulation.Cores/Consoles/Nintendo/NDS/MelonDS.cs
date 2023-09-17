using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Common.IOExtensions;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Properties;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	[PortedCore(CoreNames.MelonDS, "Arisotura", "0.9.5", "https://melonds.kuribo64.net/")]
	[ServiceNotApplicable(new[] { typeof(IDriveLight), typeof(IRegionable) })]
	public partial class NDS : WaterboxCore
	{
		private readonly LibMelonDS _core;
		private readonly NDSDisassembler _disassembler;

		private readonly Dictionary<string, byte[]> _coreFiles = new();
		private readonly Dictionary<LibMelonDS.ConfigEntry, string> _configEntryToPath = new();
		private readonly LibMelonDS.FileCallbackInterface _fileCallbackInterface;

		private int GetFileLengthCallback(string path)
			=> _coreFiles.TryGetValue(path, out var file) ? file.Length : 0;

		private void GetFileDataCallback(string path, IntPtr buffer)
		{
			var file = _coreFiles[path];
			Marshal.Copy(file, 0, buffer, file.Length);
		}

		private void AddCoreFile(LibMelonDS.ConfigEntry configEntry, string path, byte[] file)
		{
			if (file.Length == 0)
			{
				throw new InvalidOperationException($"Tried to add 0-sized core file to {path}");
			}

			_configEntryToPath.Add(configEntry, path);
			_coreFiles.Add(path, file);
		}

		private readonly LibMelonDS.LogCallback _logCallback;

		private static void LogCallback(LibMelonDS.LogLevel level, string message)
			=> Console.Write($"[{level}] {message}");

		[CoreConstructor(VSystemID.Raw.NDS)]
		public NDS(CoreLoadParameters<NDSSettings, NDSSyncSettings> lp)
			: base(lp.Comm, new()
			{
				DefaultWidth = 256,
				DefaultHeight = 384,
				MaxWidth = 256,
				MaxHeight = 384,
				MaxSamples = 1024,
				DefaultFpsNumerator = 33513982,
				DefaultFpsDenominator = 560190,
				SystemId = VSystemID.Raw.NDS,
			})
		{
			_syncSettings = lp.SyncSettings ?? new();
			_settings = lp.Settings ?? new();

			_activeSyncSettings = _syncSettings.Clone();

			IsDSi = _activeSyncSettings.UseDSi;

			var roms = lp.Roms.Select(r => r.RomData).ToList();
			
			DSiTitleId = GetDSiTitleId(roms[0]);
			IsDSi |= IsDSiWare;

			if (roms.Count > (IsDSi ? 1 : 2))
			{
				throw new InvalidOperationException("Wrong number of ROMs!");
			}

			var gbacartpresent = roms.Count == 2;

			InitMemoryCallbacks();

			_tracecb = MakeTrace;
			_threadstartcb = ThreadStartCallback;

			_configCallbackInterface.GetBoolean = GetBooleanSettingCallback;
			_configCallbackInterface.GetInteger = GetIntegerSettingCallback;
			_configCallbackInterface.GetString = GetStringSettingCallback;
			_configCallbackInterface.GetArray = GetArraySettingCallback;

			_fileCallbackInterface.GetLength = GetFileLengthCallback;
			_fileCallbackInterface.GetData = GetFileDataCallback;

			_logCallback = LogCallback;

			_core = PreInit<LibMelonDS>(new()
			{
				Filename = "melonDS.wbx",
				SbrkHeapSizeKB = 2 * 1024,
				SealedHeapSizeKB = 4,
				InvisibleHeapSizeKB = 4 * 1024,
				PlainHeapSizeKB = 4,
				MmapHeapSizeKB = 1024 * 1024,
				SkipCoreConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			}, new Delegate[]
			{
				_readcb, _writecb, _execcb, _tracecb, _threadstartcb,
				_configCallbackInterface.GetBoolean, _configCallbackInterface.GetInteger,
				_configCallbackInterface.GetString, _configCallbackInterface.GetArray,
				_fileCallbackInterface.GetLength, _fileCallbackInterface.GetData,
				_logCallback
			});

			_activeSyncSettings.UseRealBIOS |= IsDSi;

			if (_activeSyncSettings.UseRealBIOS)
			{
				AddCoreFile(LibMelonDS.ConfigEntry.BIOS7Path, "bios7.bin",
					CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios7")));
				AddCoreFile(LibMelonDS.ConfigEntry.BIOS9Path, "bios9.bin",
					CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios9")));
			}

			if (IsDSi)
			{
				AddCoreFile(LibMelonDS.ConfigEntry.DSi_BIOS7Path, "bios7i.bin",
					CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios7i")));
				AddCoreFile(LibMelonDS.ConfigEntry.DSi_BIOS9Path, "bios9i.bin",
					CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios9i")));
				AddCoreFile(LibMelonDS.ConfigEntry.DSi_FirmwarePath, "firmwarei.bin",
					CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "firmwarei")));
				AddCoreFile(LibMelonDS.ConfigEntry.DSi_NANDPath, "nand.bin",
					DecideNAND(CoreComm.CoreFileProvider, (DSiTitleId.Upper & ~0xFF) == 0x00030000, roms[0][0x1B0]));
			}
			else if (_activeSyncSettings.UseRealBIOS)
			{
				AddCoreFile(LibMelonDS.ConfigEntry.FirmwarePath, "firmware.bin",
					CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "firmware")));
			}

			if (IsDSiWare)
			{
				_coreFiles.Add("tmd.rom", GetTMDData(DSiTitleId.Full));
				_coreFiles.Add("dsiware.rom", roms[0]);
			}
			else
			{
				_coreFiles.Add("nds.rom", roms[0]);
				if (gbacartpresent)
				{
					_coreFiles.Add("gba.rom", roms[1]);
				}
			}

			LibMelonDS.InitConfig initConfig;
			initConfig.SkipFW = _activeSyncSettings.SkipFirmware;
			initConfig.HasGBACart = gbacartpresent;
			initConfig.DSi = IsDSi;
			initConfig.ClearNAND = _activeSyncSettings.ClearNAND || lp.DeterministicEmulationRequested;
			initConfig.LoadDSiWare = IsDSiWare;
			initConfig.ThreeDeeRenderer = _activeSyncSettings.ThreeDeeRenderer;
			initConfig.RenderSettings.SoftThreaded = _activeSyncSettings.ThreadedRendering;
			initConfig.RenderSettings.GLScaleFactor = 1; // TODO
			initConfig.RenderSettings.GLBetterPolygons = false; // TODO

			_activeSyncSettings.FirmwareOverride |= !_activeSyncSettings.UseRealBIOS || lp.DeterministicEmulationRequested;

			// ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
			if (!IsDSi && _syncSettings.FirmwareStartUp == NDSSyncSettings.StartUp.AutoBoot)
			{
				_activeSyncSettings.FirmwareLanguage |= (NDSSyncSettings.Language)0x40;
			}

			if (_activeSyncSettings.UseRealBIOS)
			{
				var fw = _coreFiles[_configEntryToPath[IsDSi ? LibMelonDS.ConfigEntry.DSi_FirmwarePath : LibMelonDS.ConfigEntry.FirmwarePath]];
				if (IsDSi || NDSFirmware.MaybeWarnIfBadFw(fw, CoreComm)) // fw checks dont work on dsi firmware, don't bother
				{
					if (_activeSyncSettings.FirmwareOverride)
					{
						NDSFirmware.SanitizeFw(fw);
					}
				}
			}

			var error = _core.Init(
				ref initConfig,
				_configCallbackInterface.AllCallbacksInArray(_adapter),
				_fileCallbackInterface.AllCallbacksInArray(_adapter),
				_logCallback);
			if (error != IntPtr.Zero)
			{
				using (_exe.EnterExit())
				{
					throw new InvalidOperationException(Marshal.PtrToStringAnsi(error));
				}
			}

			// the semantics of firmware override mean that sync settings will override firmware on a hard reset, which we don't want here
			_activeSyncSettings.FirmwareOverride = false;

			PostInit();

			((MemoryDomainList)this.AsMemoryDomains()).SystemBus = new NDSSystemBus(this.AsMemoryDomains()["ARM9 System Bus"], this.AsMemoryDomains()["ARM7 System Bus"]);

			DeterministicEmulation = lp.DeterministicEmulationRequested || !_activeSyncSettings.UseRealTime;
			InitializeRtc(_activeSyncSettings.InitialTime);

			_frameThreadPtr = _core.GetFrameThreadProc();
			if (_frameThreadPtr != IntPtr.Zero)
			{
				Console.WriteLine($"Setting up waterbox thread for 0x{(ulong)_frameThreadPtr:X16}");
				_frameThread = new(FrameThreadProc) { IsBackground = true };
				_frameThread.Start();
				_frameThreadAction = CallingConventionAdapters
					.GetWaterboxUnsafeUnwrapped()
					.GetDelegateForFunctionPointer<Action>(_frameThreadPtr);
				_core.SetThreadStartCallback(_threadstartcb);
			}

			_disassembler = new(_core);
			_serviceProvider.Register<IDisassemblable>(_disassembler);

			const string TRACE_HEADER = "ARM9+ARM7: Opcode address, opcode, registers (r0, r1, r2, r3, r4, r5, r6, r7, r8, r9, r10, r11, r12, SP, LR, PC, Cy, CpuMode)";
			Tracer = new TraceBuffer(TRACE_HEADER);
			_serviceProvider.Register(Tracer);
		}

		private static (ulong Full, uint Upper, uint Lower) GetDSiTitleId(IReadOnlyList<byte> file)
		{
			ulong titleId = 0;
			for (var i = 0; i < 8; i++)
			{
				titleId <<= 8;
				titleId |= file[0x237 - i];
			}
			return (titleId, (uint)(titleId >> 32), (uint)(titleId & 0xFFFFFFFFU));
		}

		private static byte[] DecideNAND(ICoreFileProvider cfp, bool isDSiEnhanced, byte regionFlags)
		{
			// TODO: priority settings?
			var nandOptions = new List<string> { "NAND (JPN)", "NAND (USA)", "NAND (EUR)", "NAND (AUS)", "NAND (CHN)", "NAND (KOR)" };
			if (isDSiEnhanced) // NB: Core makes cartridges region free regardless, DSiWare must follow DSi region locking however (we'll enforce it regardless)
			{
				nandOptions.Clear();
				if (regionFlags.Bit(0)) nandOptions.Add("NAND (JPN)");
				if (regionFlags.Bit(1)) nandOptions.Add("NAND (USA)");
				if (regionFlags.Bit(2)) nandOptions.Add("NAND (EUR)");
				if (regionFlags.Bit(3)) nandOptions.Add("NAND (AUS)");
				if (regionFlags.Bit(4)) nandOptions.Add("NAND (CHN)");
				if (regionFlags.Bit(5)) nandOptions.Add("NAND (KOR)");
			}

			foreach (var option in nandOptions)
			{
				var ret = cfp.GetFirmware(new("NDS", option));
				if (ret is not null) return ret;
			}

			throw new MissingFirmwareException("Suitable NAND file not found!");
		}

		private static byte[] GetTMDData(ulong titleId)
		{
			using var zip = new ZipArchive(Zstd.DecompressZstdStream(new MemoryStream(Resources.TMDS.Value)), ZipArchiveMode.Read, false);
			using var tmd = zip.GetEntry($"{titleId:x16}.tmd")?.Open() ?? throw new($"Cannot find TMD for title ID {titleId:x16}, please report");
			return tmd.ReadAllBytes();
		}

		// todo: wire this up w/ frontend
		public byte[] GetNAND()
		{
			var length = _core.GetNANDSize();

			if (length > 0)
			{
				var ret = new byte[length];
				_core.GetNANDData(ret);
				return ret;
			}

			return null;
		}

		public bool IsDSi { get; }

		public bool IsDSiWare => DSiTitleId.Upper == 0x00030004;

		private (ulong Full, uint Upper, uint Lower) DSiTitleId { get; }

		public override ControllerDefinition ControllerDefinition => NDSController;

		public static readonly ControllerDefinition NDSController = new ControllerDefinition("NDS Controller")
		{
			BoolButtons =
			{
				"Up", "Down", "Left", "Right", "Start", "Select", "B", "A", "Y", "X", "L", "R", "LidOpen", "LidClose", "Touch", "Power"
			}
		}.AddXYPair("Touch {0}", AxisPairOrientation.RightAndUp, 0.RangeTo(255), 128, 0.RangeTo(191), 96)
			.AddAxis("Mic Volume", (0).RangeTo(100), 0)
			.AddAxis("GBA Light Sensor", 0.RangeTo(10), 0)
			.MakeImmutable();

		private static LibMelonDS.Buttons GetButtons(IController c)
		{
			LibMelonDS.Buttons b = 0;
			if (c.IsPressed("Up"))
				b |= LibMelonDS.Buttons.UP;
			if (c.IsPressed("Down"))
				b |= LibMelonDS.Buttons.DOWN;
			if (c.IsPressed("Left"))
				b |= LibMelonDS.Buttons.LEFT;
			if (c.IsPressed("Right"))
				b |= LibMelonDS.Buttons.RIGHT;
			if (c.IsPressed("Start"))
				b |= LibMelonDS.Buttons.START;
			if (c.IsPressed("Select"))
				b |= LibMelonDS.Buttons.SELECT;
			if (c.IsPressed("B"))
				b |= LibMelonDS.Buttons.B;
			if (c.IsPressed("A"))
				b |= LibMelonDS.Buttons.A;
			if (c.IsPressed("Y"))
				b |= LibMelonDS.Buttons.Y;
			if (c.IsPressed("X"))
				b |= LibMelonDS.Buttons.X;
			if (c.IsPressed("L"))
				b |= LibMelonDS.Buttons.L;
			if (c.IsPressed("R"))
				b |= LibMelonDS.Buttons.R;
			if (c.IsPressed("LidOpen"))
				b |= LibMelonDS.Buttons.LIDOPEN;
			if (c.IsPressed("LidClose"))
				b |= LibMelonDS.Buttons.LIDCLOSE;
			if (c.IsPressed("Touch"))
				b |= LibMelonDS.Buttons.TOUCH;
			if (c.IsPressed("Power"))
				b |= LibMelonDS.Buttons.POWER;

			return b;
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			_core.SetTraceCallback(Tracer.IsEnabled() ? _tracecb : null, _settings.GetTraceMask());
			return new LibMelonDS.FrameInfo
			{
				Time = GetRtcTime(!DeterministicEmulation),
				Keys = GetButtons(controller),
				TouchX = (byte)controller.AxisValue("Touch X"),
				TouchY = (byte)controller.AxisValue("Touch Y"),
				MicVolume = (byte)controller.AxisValue("Mic Volume"),
				GBALightSensor = (byte)controller.AxisValue("GBA Light Sensor"),
				ConsiderAltLag = _settings.ConsiderAltLag,
			};
		}

		private readonly IntPtr _frameThreadPtr;
		private readonly Action _frameThreadAction;
		private readonly LibMelonDS.ThreadStartCallback _threadstartcb;

		private readonly Thread _frameThread;
		private readonly SemaphoreSlim _frameThreadStartEvent = new(0, 1);
		private readonly SemaphoreSlim _frameThreadEndEvent = new(0, 1);
		private bool _isDisposing;
		private bool _renderThreadRanThisFrame;

		public override void Dispose()
		{
			_isDisposing = true;
			_frameThreadStartEvent.Release();
			_frameThread?.Join();
			_frameThreadStartEvent.Dispose();
			_frameThreadEndEvent.Dispose();
			base.Dispose();
		}

		private void FrameThreadProc()
		{
			while (true)
			{
				_frameThreadStartEvent.Wait();
				if (_isDisposing) break;
				_frameThreadAction();
				_frameThreadEndEvent.Release();
			}
		}

		private void ThreadStartCallback()
		{
			if (_renderThreadRanThisFrame)
			{
				// This is technically possible due to the game able to force another frame to be rendered by touching vcount
				// (ALSO MEANS VSYNC NUMBERS ARE KIND OF A LIE)
				_frameThreadEndEvent.Wait();
			}

			_renderThreadRanThisFrame = true;
			_frameThreadStartEvent.Release();
		}

		protected override void FrameAdvancePost()
		{
			if (_renderThreadRanThisFrame)
			{
				_frameThreadEndEvent.Wait();
				_renderThreadRanThisFrame = false;
			}
		}

		protected override void LoadStateBinaryInternal(BinaryReader reader)
		{
			SetMemoryCallbacks();
			_core.SetThreadStartCallback(_threadstartcb);
			if (_frameThreadPtr != _core.GetFrameThreadProc())
			{
				throw new InvalidOperationException("_frameThreadPtr mismatch");
			}
		}

		// omega hack
		public class NDSSystemBus : MemoryDomain
		{
			private readonly MemoryDomain Arm9Bus;
			private readonly MemoryDomain Arm7Bus;

			public NDSSystemBus(MemoryDomain arm9, MemoryDomain arm7)
			{
				Name = "System Bus";
				Size = 1L << 32;
				WordSize = 4;
				EndianType = Endian.Little;
				Writable = false;

				Arm9Bus = arm9;
				Arm7Bus = arm7;
			}

			public bool UseArm9 { get; set; } = true;

			public override byte PeekByte(long addr) => UseArm9 ? Arm9Bus.PeekByte(addr) : Arm7Bus.PeekByte(addr);

			public override void PokeByte(long addr, byte val) => throw new InvalidOperationException();
		}
	}
}
