using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
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
		private readonly LibMelonDS.FileCallbackInterface _fileCallbackInterface;

		private int GetFileLengthCallback(string path)
			=> _coreFiles.TryGetValue(path, out var file) ? file.Length : 0;

		private void GetFileDataCallback(string path, IntPtr buffer)
		{
			var file = _coreFiles[path];
			Marshal.Copy(file, 0, buffer, file.Length);
		}

		private void AddCoreFile(string path, byte[] file)
		{
			if (file.Length == 0)
			{
				throw new InvalidOperationException($"Tried to add 0-sized core file to {path}");
			}

			_coreFiles.Add(path, file);
		}

		private readonly LibMelonDS.LogCallback _logCallback;

		private static void LogCallback(LibMelonDS.LogLevel level, string message)
			=> Console.Write($"[{level}] {message}");

		private readonly MelonDSGLTextureProvider _glTextureProvider;
		private readonly IOpenGLProvider _openGLProvider;
		private readonly LibMelonDS.GetGLProcAddressCallback _getGLProcAddressCallback;
		private object _glContext;

		private IntPtr GetGLProcAddressCallback(string proc)
			=> _openGLProvider.GetGLProcAddress(proc);

		// TODO: Probably can make these into an interface (ITouchScreen with UntransformPoint/TransformPoint methods?)
		// Which case the hackiness of the current screen controls wouldn't be as bad
		public Vector2 GetTouchCoords(int x, int y)
		{
			if (_glContext != null)
			{
				_core.GetTouchCoords(ref x, ref y);
			}
			else
			{
				// no GL context, so nothing fancy can be applied
				y -= 192;
			}

			return new(x, y);
		}

		public Vector2 GetScreenCoords(float x, float y)
		{
			if (_glContext != null)
			{
				_core.GetScreenCoords(ref x, ref y);
			}
			else
			{
				// no GL context, so nothing fancy can be applied
				y += 192;
			}

			return new(x, y);
		}

		[CoreConstructor(VSystemID.Raw.NDS)]
		public NDS(CoreLoadParameters<NDSSettings, NDSSyncSettings> lp)
			: base(lp.Comm, new()
			{
				DefaultWidth = 256,
				DefaultHeight = 384,
				MaxWidth = 256 * 16,
				MaxHeight = (384 + 128) * 16,
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

			InitMemoryCallbacks();

			_traceCallback = MakeTrace;
			_threadStartCallback = ThreadStartCallback;

			_configCallbackInterface.GetBoolean = GetBooleanSettingCallback;
			_configCallbackInterface.GetInteger = GetIntegerSettingCallback;
			_configCallbackInterface.GetString = GetStringSettingCallback;
			_configCallbackInterface.GetArray = GetArraySettingCallback;

			_fileCallbackInterface.GetLength = GetFileLengthCallback;
			_fileCallbackInterface.GetData = GetFileDataCallback;

			_logCallback = LogCallback;

			_openGLProvider = CoreComm.OpenGLProvider;
			_getGLProcAddressCallback = GetGLProcAddressCallback;

			if (lp.DeterministicEmulationRequested)
			{
				_activeSyncSettings.ThreeDeeRenderer = NDSSyncSettings.ThreeDeeRendererType.Software;
			}

			if (_activeSyncSettings.ThreeDeeRenderer != NDSSyncSettings.ThreeDeeRendererType.Software)
			{
				// ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
				var (majorGlVersion, minorGlVersion) = _activeSyncSettings.ThreeDeeRenderer switch
				{
					NDSSyncSettings.ThreeDeeRendererType.OpenGL_Classic => (3, 2),
					// NDSSyncSettings.ThreeDeeRendererType.OpenGL_Compute => (4, 3),
					_ => throw new InvalidOperationException($"Invalid {nameof(NDSSyncSettings.ThreeDeeRenderer)}")
				};

				if (!_openGLProvider.SupportsGLVersion(majorGlVersion, minorGlVersion))
				{
					lp.Comm.Notify($"OpenGL {majorGlVersion}.{minorGlVersion} is not supported on this machine, falling back to software renderer", null);
					_activeSyncSettings.ThreeDeeRenderer = NDSSyncSettings.ThreeDeeRendererType.Software;
				}
				else
				{
					_glContext = _openGLProvider.RequestGLContext(majorGlVersion, minorGlVersion, true, false);
				}
			}

			if (_activeSyncSettings.ThreeDeeRenderer == NDSSyncSettings.ThreeDeeRendererType.Software)
			{
				if (!_openGLProvider.SupportsGLVersion(3, 1))
				{
					lp.Comm.Notify("OpenGL 3.1 is not supported on this machine, screen control options will not work.", null);
				}
				else
				{
					_glContext = _openGLProvider.RequestGLContext(3, 1, true, false);
				}
			}

			_core = PreInit<LibMelonDS>(new()
			{
				Filename = "melonDS.wbx",
				SbrkHeapSizeKB = 2 * 1024,
				SealedHeapSizeKB = 4,
				InvisibleHeapSizeKB = 4 * 1024,
				PlainHeapSizeKB = 4,
				MmapHeapSizeKB = 1920 * 1024,
				SkipCoreConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			}, new Delegate[]
			{
				_readCallback, _writeCallback, _execCallback, _traceCallback, _threadStartCallback,
				_configCallbackInterface.GetBoolean, _configCallbackInterface.GetInteger,
				_configCallbackInterface.GetString, _configCallbackInterface.GetArray,
				_fileCallbackInterface.GetLength, _fileCallbackInterface.GetData,
				_logCallback, _getGLProcAddressCallback
			});

			_activeSyncSettings.UseRealBIOS |= IsDSi;

			if (_activeSyncSettings.UseRealBIOS)
			{
				AddCoreFile("bios7.bin", CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios7")));
				AddCoreFile("bios9.bin", CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios9")));
				AddCoreFile("firmware.bin", CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", IsDSi ? "firmwarei" : "firmware")));
			}

			if (IsDSi)
			{
				AddCoreFile("bios7i.bin", CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios7i")));
				AddCoreFile("bios9i.bin", CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios9i")));
				AddCoreFile("nand.bin", DecideNAND(CoreComm.CoreFileProvider, (DSiTitleId.Upper & ~0xFF) == 0x00030000, roms[0][0x1B0]));
			}

			if (IsDSiWare)
			{
				AddCoreFile("tmd.rom", GetTMDData(DSiTitleId.Full));
				AddCoreFile("dsiware.rom", roms[0]);
			}
			else
			{
				AddCoreFile("nds.rom", roms[0]);
				if (roms.Count == 2)
				{
					AddCoreFile("gba.rom", roms[1]);
				}
			}

			_activeSyncSettings.FirmwareOverride |= !_activeSyncSettings.UseRealBIOS || lp.DeterministicEmulationRequested;

			// ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
			if (!IsDSi && _activeSyncSettings.FirmwareStartUp == NDSSyncSettings.StartUp.AutoBoot)
			{
				_activeSyncSettings.FirmwareLanguage |= (NDSSyncSettings.Language)0x40;
			}

			_activeSyncSettings.UseRealTime &= !lp.DeterministicEmulationRequested;
			var startTime = _activeSyncSettings.UseRealTime ? DateTime.Now : _activeSyncSettings.InitialTime;

			LibMelonDS.InitConfig initConfig;
			initConfig.SkipFW = _activeSyncSettings.SkipFirmware;
			initConfig.HasGBACart = roms.Count == 2;
			initConfig.DSi = IsDSi;
			initConfig.ClearNAND = _activeSyncSettings.ClearNAND || lp.DeterministicEmulationRequested;
			initConfig.LoadDSiWare = IsDSiWare;
			initConfig.IsWinApi = !OSTailoredCode.IsUnixHost;
			initConfig.ThreeDeeRenderer = _activeSyncSettings.ThreeDeeRenderer;
			initConfig.RenderSettings.SoftThreaded = _activeSyncSettings.ThreadedRendering;
			initConfig.RenderSettings.GLScaleFactor = _activeSyncSettings.GLScaleFactor;
			initConfig.RenderSettings.GLBetterPolygons = _activeSyncSettings.GLBetterPolygons;
			initConfig.StartTime.Year = startTime.Year % 100;
			initConfig.StartTime.Month = startTime.Month;
			initConfig.StartTime.Day = startTime.Day;
			initConfig.StartTime.Hour = startTime.Hour;
			initConfig.StartTime.Minute = startTime.Minute;
			initConfig.StartTime.Second = startTime.Second;
			_activeSyncSettings.GetFirmwareSettings(out initConfig.FirmwareSettings);

			if (_activeSyncSettings.UseRealBIOS)
			{
				var fw = _coreFiles["firmware.bin"];

				if (fw.Length is not (0x20000 or 0x40000 or 0x80000))
				{
					throw new InvalidOperationException("Invalid firmware length");
				}

				NDSFirmware.MaybeWarnIfBadFw(fw, CoreComm.ShowMessage);
			}

			var error = _core.Init(
				ref initConfig,
				_configCallbackInterface.AllCallbacksInArray(_adapter),
				_fileCallbackInterface.AllCallbacksInArray(_adapter),
				_logCallback,
				_glContext != null ? _getGLProcAddressCallback : null);

			if (error != IntPtr.Zero)
			{
				using (_exe.EnterExit())
				{
					throw new InvalidOperationException(Marshal.PtrToStringAnsi(error));
				}
			}

			// add DSiWare sav files to the core files, so we can import/export for SaveRAM
			if (IsDSiWare)
			{
				_core.DSiWareSavsLength(DSiTitleId.Lower, out var publicSavSize, out var privateSavSize, out var bannerSavSize);

				if (publicSavSize != 0)
				{
					AddCoreFile("public.sav", new byte[publicSavSize]);
				}

				if (privateSavSize != 0)
				{
					AddCoreFile("private.sav", new byte[privateSavSize]);
				}

				if (bannerSavSize != 0)
				{
					AddCoreFile("banner.sav", new byte[bannerSavSize]);
				}

				DSiWareSaveLength = publicSavSize + privateSavSize + bannerSavSize;
			}

			PostInit();

			((MemoryDomainList)this.AsMemoryDomains()).SystemBus = new NDSSystemBus(this.AsMemoryDomains()["ARM9 System Bus"], this.AsMemoryDomains()["ARM7 System Bus"]);

			DeterministicEmulation = lp.DeterministicEmulationRequested || !_activeSyncSettings.UseRealTime;

			_frameThreadPtr = _core.GetFrameThreadProc();
			if (_frameThreadPtr != IntPtr.Zero)
			{
				Console.WriteLine($"Setting up waterbox thread for 0x{(ulong)_frameThreadPtr:X16}");
				_frameThread = new(FrameThreadProc) { IsBackground = true };
				_frameThread.Start();
				_frameThreadAction = CallingConventionAdapters
					.GetWaterboxUnsafeUnwrapped()
					.GetDelegateForFunctionPointer<Action>(_frameThreadPtr);
				_core.SetThreadStartCallback(_threadStartCallback);
			}

			_disassembler = new(_core);
			_serviceProvider.Register<IDisassemblable>(_disassembler);

			const string TRACE_HEADER = "ARM9+ARM7: Opcode address, opcode, registers (r0, r1, r2, r3, r4, r5, r6, r7, r8, r9, r10, r11, r12, SP, LR, PC, Cy, CpuMode)";
			Tracer = new TraceBuffer(TRACE_HEADER);
			_serviceProvider.Register(Tracer);

			if (_glContext != null)
			{
				_glTextureProvider = new(this, _core, () => _openGLProvider.ActivateGLContext(_glContext));
				_serviceProvider.Register<IVideoProvider>(_glTextureProvider);
				RefreshScreenSettings(_settings);
			}
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
			var nandOptions = new List<string> { "JPN", "USA", "EUR", "AUS", "CHN", "KOR" };
			if (isDSiEnhanced) // NB: Core makes cartridges region free regardless, DSiWare must follow DSi region locking however (we'll enforce it regardless)
			{
				nandOptions.Clear();
				if (regionFlags.Bit(0)) nandOptions.Add("JPN");
				if (regionFlags.Bit(1)) nandOptions.Add("USA");
				if (regionFlags.Bit(2)) nandOptions.Add("EUR");
				if (regionFlags.Bit(3)) nandOptions.Add("AUS");
				if (regionFlags.Bit(4)) nandOptions.Add("CHN");
				if (regionFlags.Bit(5)) nandOptions.Add("KOR");
			}

			foreach (var option in nandOptions)
			{
				var ret = cfp.GetFirmware(new("NDS", $"NAND_{option}"));
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
			if (_glContext != null)
			{
				_openGLProvider.ActivateGLContext(_glContext);
			}

			_core.SetTraceCallback(Tracer.IsEnabled() ? _traceCallback : null, _settings.GetTraceMask());

			return new LibMelonDS.FrameInfo
			{
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
		private readonly LibMelonDS.ThreadStartCallback _threadStartCallback;

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

			if (_glContext != null)
			{
				_openGLProvider.ReleaseGLContext(_glContext);
				_glContext = null;
			}

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

			if (_glTextureProvider != null)
			{
				_glTextureProvider.VideoDirty = true;
			}
		}

		protected override void LoadStateBinaryInternal(BinaryReader reader)
		{
			SetMemoryCallbacks();
			_core.SetThreadStartCallback(_threadStartCallback);
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
