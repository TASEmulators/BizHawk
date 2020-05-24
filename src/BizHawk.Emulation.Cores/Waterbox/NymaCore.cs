using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Waterbox
{
	public unsafe abstract partial class NymaCore : WaterboxCore
	{
		protected NymaCore(GameInfo game, byte[] rom, CoreComm comm, string systemId, string controllerDeckName,
			NymaSettings settings, NymaSyncSettings syncSettings)
			: base(comm, new Configuration { SystemId = systemId })
		{
			_settings = settings ?? new NymaSettings();
			_syncSettings = syncSettings ?? new NymaSyncSettings();
			_syncSettingsActual = _syncSettings;
			_controllerDeckName = controllerDeckName;
		}

		private LibNymaCore _nyma;
		protected T DoInit<T>(GameInfo game, byte[] rom, string wbxFilename, string extension)
			where T : LibNymaCore
		{
			var t = PreInit<T>(new WaterboxOptions
			{
				// TODO fix these up
				Filename = wbxFilename,
				SbrkHeapSizeKB = 1024 * 16,
				SealedHeapSizeKB = 1024 * 16,
				InvisibleHeapSizeKB = 1024 * 16,
				PlainHeapSizeKB = 1024 * 16,
				MmapHeapSizeKB = 1024 * 16,
				SkipCoreConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			});
			_nyma = t;
			_settingsQueryDelegate = new LibNymaCore.FrontendSettingQuery(SettingsQuery);
			var fn = game.FilesystemSafeName();

			using (_exe.EnterExit())
			{
				_nyma.PreInit();
				InitSyncSettingsInfo();
				_exe.AddReadonlyFile(rom, fn);
				_nyma.SetFrontendSettingQuery(_settingsQueryDelegate);

				var didInit = _nyma.Init(new LibNymaCore.InitData
				{
					// TODO: Set these as some cores need them
					FileNameBase = "",
					FileNameExt = extension.Trim('.').ToLowerInvariant(),
					FileNameFull = fn
				});

				if (!didInit)
					throw new InvalidOperationException("Core rejected the rom!");

				_exe.RemoveReadonlyFile(fn);

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

				InitControls();
				_nyma.SetFrontendSettingQuery(null);
				PostInit();
				SettingsInfo.LayerNames = GetLayerData();
				_nyma.SetFrontendSettingQuery(_settingsQueryDelegate);
				PutSettings(_settings);
			}

			return t;
		}

		protected override void LoadStateBinaryInternal(BinaryReader reader)
		{
			_nyma.SetFrontendSettingQuery(_settingsQueryDelegate);
		}

		// todo: bleh
		private GCHandle _frameAdvanceInputLock;

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			_controllerAdapter.SetBits(controller, _inputPortData);
			_frameAdvanceInputLock = GCHandle.Alloc(_inputPortData, GCHandleType.Pinned);
			var ret = new LibNymaCore.FrameInfo
			{
				SkipRendering = (short)(render ? 0 : 1),
				SkipSoundening =(short)(rendersound ? 0 : 1),
				Command = LibNymaCore.CommandType.NONE,
				InputPortData = (byte*)_frameAdvanceInputLock.AddrOfPinnedObject()
			};
			return ret;
		}
		protected override void FrameAdvancePost()
		{
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
	}
}
