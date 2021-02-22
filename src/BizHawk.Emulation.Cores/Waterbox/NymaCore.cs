using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.DiscSystem;
using FlatBuffers;
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

		private LibNymaCore _nyma;
		protected T DoInit<T>(
			CoreLoadParameters<NymaSettings, NymaSyncSettings> lp,
			string wbxFilename,
			IDictionary<string, FirmwareID> firmwares = null
		)
			where T : LibNymaCore
		{
			return DoInit<T>(
				lp.Game,
				lp.Roms.FirstOrDefault()?.RomData,
				lp.Discs.Select(d => d.DiscData).ToArray(),
				wbxFilename,
				lp.Roms.FirstOrDefault()?.Extension,
				lp.DeterministicEmulationRequested,
				firmwares
			);
		}
		protected T DoInit<T>(GameInfo game, byte[] rom, Disc[] discs, string wbxFilename, string extension, bool deterministic,
			IDictionary<string, FirmwareID> firmwares = null)
			where T : LibNymaCore
		{
			_settingsQueryDelegate = SettingsQuery;
			_cdTocCallback = CDTOCCallback;
			_cdSectorCallback = CDSectorCallback;

			var filesToRemove = new List<string>();

			var firmwareDelegate = new LibNymaCore.FrontendFirmwareNotify((name) =>
			{
				if (firmwares != null && firmwares.TryGetValue(name, out var id))
				{
					var data = CoreComm.CoreFileProvider.GetFirmware(id, true,
						"Firmware files are usually required and may stop your game from loading");
					if (data != null)
					{
						_exe.AddReadonlyFile(data, name);
						filesToRemove.Add(name);
					}
				}
				else
				{
					throw new InvalidOperationException($"Core asked for firmware `{name}`, but that was not understood by the system");
				}
			});

			var t = PreInit<T>(new WaterboxOptions
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
			}, new Delegate[] { _settingsQueryDelegate, _cdTocCallback, _cdSectorCallback, firmwareDelegate });
			_nyma = t;

			using (_exe.EnterExit())
			{
				_nyma.PreInit();
				_nyma.SetFrontendFirmwareNotify(firmwareDelegate);
				var portData = GetInputPortsData();
				InitAllSettingsInfo(portData);
				_nyma.SetFrontendSettingQuery(_settingsQueryDelegate);

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
					var fn = game.FilesystemSafeName();
					_exe.AddReadonlyFile(rom, fn);

					var didInit = _nyma.InitRom(new LibNymaCore.InitData
					{
						// TODO: Set these as some cores need them
						FileNameBase = "",
						FileNameExt = extension.Trim('.').ToLowerInvariant(),
						FileNameFull = fn
					});

					if (!didInit)
						throw new InvalidOperationException("Core rejected the rom!");

					_exe.RemoveReadonlyFile(fn);
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
				_soundBuffer = new short[22050 * 2];

				InitControls(portData, discs?.Length > 0, ref info);
				PostInit();
				SettingsInfo.LayerNames = GetLayerData();
				_settings.Normalize(SettingsInfo);
				_syncSettings.Normalize(SettingsInfo);
				_nyma.SetFrontendSettingQuery(_settingsQueryDelegate);
				if (_disks != null)
					_nyma.SetCDCallbacks(_cdTocCallback, _cdSectorCallback);
				PutSettings(_settings);
				DateTime RtcStart = DateTime.Parse("2010-01-01");
				try
				{
					RtcStart = DateTime.Parse(SettingsQuery("nyma.rtcinitialtime"));
				}
				catch
				{
					Console.Error.WriteLine($"Couldn't parse DateTime \"{SettingsQuery("nyma.rtcinitialtime")}\"");
				}
				// Don't optimistically set deterministic, as some cores like faust can change this
				DeterministicEmulation = deterministic; // || SettingsQuery("nyma.rtcrealtime") == "0";
				InitializeRtc(RtcStart);
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

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			DriveLightOn = false;
			_controllerAdapter.SetBits(controller, _inputPortData);
			_frameAdvanceInputLock = GCHandle.Alloc(_inputPortData, GCHandleType.Pinned);
			LibNymaCore.BizhawkFlags flags = 0;
			if (!render)
				flags |= LibNymaCore.BizhawkFlags.SkipRendering;
			if (!rendersound)
				flags |= LibNymaCore.BizhawkFlags.SkipSoundening;
			if (SettingsQuery("nyma.constantfb") != "0")
				flags |= LibNymaCore.BizhawkFlags.RenderConstantSize;
			if (controller.IsPressed("Previous Disk"))
				flags |= LibNymaCore.BizhawkFlags.PreviousDisk;
			if (controller.IsPressed("Next Disk"))
				flags |= LibNymaCore.BizhawkFlags.NextDisk;

			var ret = new LibNymaCore.FrameInfo
			{
				Flags = flags,
				Command = controller.IsPressed("Power")
					? LibNymaCore.CommandType.POWER
					: controller.IsPressed("Reset")
						? LibNymaCore.CommandType.RESET
						: LibNymaCore.CommandType.NONE,
				InputPortData = (byte*)_frameAdvanceInputLock.AddrOfPinnedObject(),
				FrontendTime = GetRtcTime(SettingsQuery("nyma.rtcrealtime") != "0"),
			};
			if (_frameThreadStart != null)
			{
				_frameThreadProcActive = Task.Run(_frameThreadStart);
			}
			return ret;
		}
		protected override void FrameAdvancePost()
		{
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
		public override int VirtualHeight =>_mdfnNominalHeight;

		public DisplayType Region { get; protected set; }

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
	}
}
