using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Waterbox
{
	public unsafe abstract partial class NymaCore : WaterboxCore
	{
		protected NymaCore(GameInfo game, byte[] rom, CoreComm comm, Configuration c)
			: base(comm, c)
		{
		}

		private LibNymaCore _nyma;
		protected T DoInit<T>(GameInfo game, byte[] rom, string filename, string extension)
			where T : LibNymaCore
		{
			var t = PreInit<T>(new WaterboxOptions
			{
				// TODO cfg and stuff
				Filename = filename,
				SbrkHeapSizeKB = 1024 * 16,
				SealedHeapSizeKB = 1024 * 16,
				InvisibleHeapSizeKB = 1024 * 16,
				PlainHeapSizeKB = 1024 * 16,
				MmapHeapSizeKB = 1024 * 16,
				StartAddress = WaterboxHost.CanonicalStart,
				SkipCoreConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			});
			_nyma = t;

			using (_exe.EnterExit())
			{
				var fn = game.FilesystemSafeName();

				_exe.AddReadonlyFile(rom, fn);

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

				PostInit();
			}

			return t;
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
		/// Gets a string array of valid layers to pass to SetLayers, or null if that method should not be called
		/// </summary>
		private string[] GetLayerData()
		{
			using (_exe.EnterExit())
			{
				var p = _nyma.GetLayerData();
				if (p == null)
					return null;
				var ret = new List<string>();
				var q = p;
				while (true)
				{
					if (*q == 0)
					{
						if (q > p)
							ret.Add(Mershul.PtrToStringUtf8((IntPtr)p));
						else
							break;
						p = ++q;
					}
					q++;
				}
				return ret.ToArray();
			}
		}
	}
}
