using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.DiscSystem;
using FlatBuffers;
using NymaTypes;

namespace BizHawk.Emulation.Cores.Waterbox
{
	public unsafe abstract partial class NymaCore : WaterboxCore
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
		protected T DoInit<T>(GameInfo game, byte[] rom, Disc[] discs, string wbxFilename, string extension, bool deterministic,
			ICollection<KeyValuePair<string, byte[]>> firmwares = null)
			where T : LibNymaCore
		{
			var t = PreInit<T>(new WaterboxOptions
			{
				Filename = wbxFilename,
				// MemoryBlock understands reserve vs commit semantics, so nothing to be gained by making these precisely sized
				SbrkHeapSizeKB = 1024 * 16,
				SealedHeapSizeKB = 1024 * 48,
				InvisibleHeapSizeKB = 1024 * 48,
				PlainHeapSizeKB = 1024 * 48,
				MmapHeapSizeKB = 1024 * 48,
				SkipCoreConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			});
			_nyma = t;
			_settingsQueryDelegate = new LibNymaCore.FrontendSettingQuery(SettingsQuery);

			using (_exe.EnterExit())
			{
				_nyma.PreInit();
				var portData = GetInputPortsData();
				InitSyncSettingsInfo(portData);
				_nyma.SetFrontendSettingQuery(_settingsQueryDelegate);
				if (firmwares != null)
				{
					foreach (var kvp in firmwares)
					{
						_exe.AddReadonlyFile(kvp.Value, kvp.Key);
					}
				}
				if (discs?.Length > 0)
				{
					_disks = discs;
					_diskReaders = _disks.Select(d => new DiscSectorReader(d) { Policy = _diskPolicy }).ToArray();
					_cdTocCallback = CDTOCCallback;
					_cdSectorCallback = CDSectorCallback;
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
				if (firmwares != null)
				{
					foreach (var kvp in firmwares)
					{
						_exe.RemoveReadonlyFile(kvp.Key);
					}
				}

				var info = *_nyma.GetSystemInfo();
				_videoBuffer = new int[info.MaxWidth * info.MaxHeight];
				BufferWidth = info.NominalWidth;
				BufferHeight = info.NominalHeight;
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

				InitControls(portData);
				_nyma.SetFrontendSettingQuery(null);
				if (_disks != null)
					_nyma.SetCDCallbacks(null, null);
				PostInit();
				SettingsInfo.LayerNames = GetLayerData();
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
					if (deterministic)
						throw new InvalidOperationException("Internal error: Core set a frame thread proc in deterministic mode");
					Console.WriteLine($"Setting up waterbox thread for {_frameThreadPtr}");
					_frameThreadStart = CallingConventionAdapters.Waterbox.GetDelegateForFunctionPointer<Action>(_frameThreadPtr);
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

		private volatile bool _frameThreadProcActive;

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			DriveLightOn = false;
			_controllerAdapter.SetBits(controller, _inputPortData);
			_frameAdvanceInputLock = GCHandle.Alloc(_inputPortData, GCHandleType.Pinned);
			var ret = new LibNymaCore.FrameInfo
			{
				SkipRendering = (short)(render ? 0 : 1),
				SkipSoundening =(short)(rendersound ? 0 : 1),
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
				_frameThreadProcActive = true;
				Task.Run(() =>
				{
					_frameThreadStart();
					_frameThreadProcActive = false;
				});
			}
			return ret;
		}
		protected override void FrameAdvancePost()
		{
			while (_frameThreadProcActive)
			{
				// The nyma core unmanaged code should always release the threadproc to completion
				// before returning from Emulate, but even when it does occasionally the threadproc
				// might not actually finish first

				// It MUST be allowed to finish now, because the theadproc doesn't know about or participate
				// in the waterbox core lockout (IMonitor) directly -- it assumes the parent has handled that
			}
			_frameAdvanceInputLock.Free();
		}

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
