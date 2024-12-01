using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.DiscSystem;

using Google.FlatBuffers;

using NymaTypes;

namespace BizHawk.Emulation.Cores.Waterbox
{
	public abstract unsafe partial class NymaCore : WaterboxCore
	{
		protected NymaCore(CoreComm comm,
			string systemId, string controllerDeckName,
			NymaSettings settings, NymaSyncSettings syncSettings)
			: base(comm, new Configuration { SystemId = systemId })
		{
			_settings = settings ?? new NymaSettings();
			_syncSettings = syncSettings ?? new NymaSyncSettings();
			_syncSettingsActual = _syncSettings;
			_controllerDeckName = controllerDeckName;
		}

		private WaterboxOptions NymaWaterboxOptions(string wbxFilename)
		{
			return new WaterboxOptions
			{
				Filename = wbxFilename,
				// WaterboxHost only saves parts of memory that have changed, so not much to be gained by making these precisely sized
				SbrkHeapSizeKB = 1024 * 16,
				SealedHeapSizeKB = 1024 * 48,
				InvisibleHeapSizeKB = 1024 * 48,
				PlainHeapSizeKB = 1024 * 48,
				MmapHeapSizeKB = 1024 * 48,
				SkipCoreConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			};
		}

		private LibNymaCore _nyma;

		protected T DoInit<T>(
			CoreLoadParameters<NymaSettings, NymaSyncSettings> lp,
			string wbxFilename,
			IDictionary<string, FirmwareID> firmwareIDMap = null
		)
			where T : LibNymaCore
		{
			return DoInit<T>(
				lp.Roms.Select(r => (r.RomData,
					Path.GetFileName(r.RomPath[(r.RomPath.LastIndexOf('|') + 1)..])!.ToLowerInvariant())).ToArray(),
				lp.Discs.Select(d => d.DiscData).ToArray(),
				wbxFilename,
				lp.Roms.FirstOrDefault()?.Extension,
				lp.DeterministicEmulationRequested,
				firmwareIDMap
			);
		}

		protected T DoInit<T>(
			GameInfo game, byte[] rom, Disc[] discs, string wbxFilename,
			string romExtension, bool deterministic, IDictionary<string, FirmwareID> firmwareIDMap = null)
			where T : LibNymaCore
		{
			return DoInit<T>(
				[ (Data: rom, Filename: game.FilesystemSafeName() + romExtension.ToLowerInvariant()) ],
				discs,
				wbxFilename,
				romExtension,
				deterministic,
				firmwareIDMap
			);
		}

		protected T DoInit<T>((byte[] Data, string Filename)[] roms, Disc[] discs, string wbxFilename,
			string romExtension, bool deterministic, IDictionary<string, FirmwareID> firmwareIDMap = null)
			where T : LibNymaCore
		{
			_settingsQueryDelegate = SettingsQuery;
			_cdTocCallback = CDTOCCallback;
			_cdSectorCallback = CDSectorCallback;

			var filesToRemove = new List<string>();

			var firmwareDelegate = new LibNymaCore.FrontendFirmwareNotify(name =>
			{
				if (firmwareIDMap != null && firmwareIDMap.TryGetValue(name, out var id))
				{
					var data = CoreComm.CoreFileProvider.GetFirmwareOrThrow(id, "Firmware files are usually required and may stop your game from loading");
					_exe.AddReadonlyFile(data, name);
					filesToRemove.Add(name);
				}
				else
				{
					throw new InvalidOperationException($"Core asked for firmware `{name}`, but that was not understood by the system");
				}
			});

			var t = PreInit<T>(NymaWaterboxOptions(wbxFilename), new Delegate[] { _settingsQueryDelegate, _cdTocCallback, _cdSectorCallback, firmwareDelegate });
			_nyma = t;

			using (_exe.EnterExit())
			{
				_nyma.PreInit();
				_nyma.SetFrontendFirmwareNotify(firmwareDelegate);
				var portData = GetInputPortsData();
				InitAllSettingsInfo(portData);
				_nyma.SetFrontendSettingQuery(_settingsQueryDelegate);

				var rtcStart = DateTime.Parse("2010-01-01", DateTimeFormatInfo.InvariantInfo);
				try
				{
					rtcStart = DateTime.Parse(SettingsQuery("nyma.rtcinitialtime"), DateTimeFormatInfo.InvariantInfo);
				}
				catch
				{
					Console.Error.WriteLine($"Couldn't parse DateTime \"{SettingsQuery("nyma.rtcinitialtime")}\"");
				}

				// Don't optimistically set deterministic, as some cores like faust can change this
				DeterministicEmulation = deterministic; // || SettingsQuery("nyma.rtcrealtime") == "0";
				InitializeRtc(rtcStart);
				_nyma.SetInitialTime(GetRtcTime(SettingsQuery("nyma.rtcrealtime") != "0"));

				if (discs?.Length > 0)
				{
					_disks = discs;
					_diskReaders = _disks.Select(d => new DiscSectorReader(d) { Policy = _diskPolicy }).ToArray();
					_nyma.SetCDCallbacks(_cdTocCallback, _cdSectorCallback);
					var didInit = _nyma.InitCd(_disks.Length);
					if (!didInit)
						throw new InvalidOperationException("Core rejected the CDs!");
				}
				else
				{
					foreach (var (data, filename) in roms)
					{
						_exe.AddReadonlyFile(data, filename);
					}

					var didInit = _nyma.InitRom(new LibNymaCore.InitData
					{
						FileNameBase = Path.GetFileNameWithoutExtension(roms[0].Filename),
						FileNameExt = romExtension.Trim('.').ToLowerInvariant(),
						FileNameFull = roms[0].Filename
					});

					if (!didInit)
						throw new InvalidOperationException("Core rejected the rom!");

					foreach (var (_, filename) in roms)
					{
						_exe.RemoveReadonlyFile(filename);
					}
				}

				foreach (var s in filesToRemove)
				{
					_exe.RemoveReadonlyFile(s);
				}
				// any later attempts to request a firmware will crash
				_nyma.SetFrontendFirmwareNotify(null);

				var info = *_nyma.GetSystemInfo();
				_videoBuffer = new int[Math.Max(info.MaxWidth * info.MaxHeight, info.LcmWidth * info.LcmHeight)];
				BufferWidth = info.NominalWidth;
				BufferHeight = info.NominalHeight;
				_mdfnNominalWidth = info.NominalWidth;
				_mdfnNominalHeight = info.NominalHeight;
				switch (info.VideoSystem)
				{
					// TODO: There seriously isn't any region besides these?
					case LibNymaCore.VideoSystem.PAL:
					case LibNymaCore.VideoSystem.SECAM:
						Region = DisplayType.PAL;
						break;
					case LibNymaCore.VideoSystem.PAL_M:
						Region = DisplayType.Dendy; // sort of...
						break;
					default:
						Region = DisplayType.NTSC;
						break;
				}
				VsyncNumerator = info.FpsFixed;
				VsyncDenominator = 1 << 24;
				ClockRate = info.MasterClock / (double)0x100000000;
				_soundBuffer = new short[22050 * 2];
				_isArcade = info.GameType == LibNymaCore.GameMediumTypes.GMT_ARCADE;

				InitControls(portData, discs?.Length ?? 0, ref info);
				PostInit();
				SettingsInfo.LayerNames = GetLayerData();
				_settings.Normalize(SettingsInfo);
				_syncSettings.Normalize(SettingsInfo);
				_nyma.SetFrontendSettingQuery(_settingsQueryDelegate);
				if (_disks != null)
					_nyma.SetCDCallbacks(_cdTocCallback, _cdSectorCallback);
				PutSettings(_settings);

				_frameThreadPtr = _nyma.GetFrameThreadProc();
				if (_frameThreadPtr != IntPtr.Zero)
				{
					// This will probably be fine, right?  TODO: Revisit
					// if (deterministic)
					// 	throw new InvalidOperationException("Internal error: Core set a frame thread proc in deterministic mode");
					Console.WriteLine($"Setting up waterbox thread for {_frameThreadPtr}");
					_frameThreadStart = CallingConventionAdapters.GetWaterboxUnsafeUnwrapped().GetDelegateForFunctionPointer<Action>(_frameThreadPtr);
				}
			}

			return t;
		}

		// inits only to get settings info
		// should only ever be called if no SettingsInfo cache exists statically within the core
		protected void InitForSettingsInfo(string wbxFilename)
		{
			_nyma = PreInit<LibNymaCore>(NymaWaterboxOptions(wbxFilename));

			using (_exe.EnterExit())
			{
				_nyma.PreInit();
				var portData = GetInputPortsData();
				InitAllSettingsInfo(portData);
			}
		}

		protected override void SaveStateBinaryInternal(BinaryWriter writer)
		{
			_controllerAdapter.SaveStateBinary(writer);
		}

		protected override void LoadStateBinaryInternal(BinaryReader reader)
		{
			_controllerAdapter.LoadStateBinary(reader);
			_nyma.SetFrontendSettingQuery(_settingsQueryDelegate);
			if (_disks != null)
				_nyma.SetCDCallbacks(_cdTocCallback, _cdSectorCallback);
			if (_frameThreadPtr != _nyma.GetFrameThreadProc())
				throw new InvalidOperationException("_frameThreadPtr mismatch");
		}

		// todo: bleh
		private GCHandle _frameAdvanceInputLock;

		private Task _frameThreadProcActive;

		private IController _currentController;

		protected bool _isArcade;

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			DriveLightOn = false;
			_currentController = controller; // need to remember this for rumble
			_controllerAdapter.SetBits(controller, _inputPortData);
			if (!_frameAdvanceInputLock.IsAllocated)
			{
				_frameAdvanceInputLock = GCHandle.Alloc(_inputPortData, GCHandleType.Pinned);
			}

			LibNymaCore.BizhawkFlags flags = 0;
			if (!render)
				flags |= LibNymaCore.BizhawkFlags.SkipRendering;
			if (!rendersound)
				flags |= LibNymaCore.BizhawkFlags.SkipSoundening;
			if (SettingsQuery("nyma.constantfb") != "0")
				flags |= LibNymaCore.BizhawkFlags.RenderConstantSize;
			int diskIndex = default;
			if (_disks is not null)
			{
				if (controller.IsPressed("Open Tray")) flags |= LibNymaCore.BizhawkFlags.OpenTray;
				if (controller.IsPressed("Close Tray")) flags |= LibNymaCore.BizhawkFlags.CloseTray;
				diskIndex = controller.AxisValue("Disk Index");
			}

			var ret = new LibNymaCore.FrameInfo
			{
				Flags = flags,
				Command = controller.IsPressed("Power")
					? LibNymaCore.CommandType.POWER
					: controller.IsPressed("Reset")
						? LibNymaCore.CommandType.RESET
						: _isArcade && controller.IsPressed("Insert Coin")
							? LibNymaCore.CommandType.INSERT_COIN
							: LibNymaCore.CommandType.NONE,
				InputPortData = (byte*)_frameAdvanceInputLock.AddrOfPinnedObject(),
				FrontendTime = GetRtcTime(SettingsQuery("nyma.rtcrealtime") != "0"),
				DiskIndex = diskIndex,
			};
			if (_frameThreadStart != null)
			{
				_frameThreadProcActive = Task.Run(_frameThreadStart);
			}
			return ret;
		}

		protected override void FrameAdvancePost()
		{
			_controllerAdapter.DoRumble(_currentController, _inputPortData);

			if (_frameThreadProcActive != null)
			{
				// The nyma core unmanaged code should always release the threadproc to completion
				// before returning from Emulate, but even when it does occasionally the threadproc
				// might not actually finish first

				// It MUST be allowed to finish now, because the theadproc doesn't know about or participate
				// in the waterbox core lockout (IMonitor) directly -- it assumes the parent has handled that
				_frameThreadProcActive.Wait();
				_frameThreadProcActive = null;
			}
		}

		private int _mdfnNominalWidth;
		private int _mdfnNominalHeight;
		public override int VirtualWidth => _mdfnNominalWidth;
		public override int VirtualHeight => _mdfnNominalHeight;

		public DisplayType Region { get; protected set; }

		public double ClockRate { get; private set; }

		/// <summary>
		/// Gets a string array of valid layers to pass to SetLayers, or an empty list if that method should not be called
		/// </summary>
		private List<string> GetLayerData()
		{
			var ret = new List<string>();
			var p = _nyma.GetLayerData();
			if (p == null)
				return ret;
			var q = p;
			while (true)
			{
				if (*q == 0)
				{
					if (q > p)
						ret.Add(Mershul.PtrToStringUtf8((IntPtr)p));
					else
						break;
					p = q + 1;
				}
				q++;
			}
			return ret;
		}

		private List<SettingT> GetSettingsData()
		{
			_exe.AddTransientFile(new byte[0], "settings");
			_nyma.DumpSettings();
			var settingsBuff = _exe.RemoveTransientFile("settings");
			return NymaTypes.Settings.GetRootAsSettings(new ByteBuffer(settingsBuff)).UnPack().Values;
		}

		private List<NPortInfoT> GetInputPortsData()
		{
			_exe.AddTransientFile(new byte[0], "inputs");
			_nyma.DumpInputs();
			var settingsBuff = _exe.RemoveTransientFile("inputs");
			return NymaTypes.NPorts.GetRootAsNPorts(new ByteBuffer(settingsBuff)).UnPack().Values;
		}

		private IntPtr _frameThreadPtr;
		private Action _frameThreadStart;

		public override void Dispose()
		{
			if (_disks != null)
			{
				foreach (var disk in _disks)
				{
					disk.Dispose();
				}
			}

			if (_frameAdvanceInputLock.IsAllocated)
			{
				_frameAdvanceInputLock.Free();
			}

			base.Dispose();
		}
	}
}
