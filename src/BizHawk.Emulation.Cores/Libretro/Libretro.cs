using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Libretro
{
	// nb: multiple libretro cores could theoretically be ran at once
	// but all of them would need to be different cores, a core itself is single instance
	[PortedCore(CoreNames.Libretro, "CasualPokePlayer", singleInstance: true, isReleased: false)]
	public partial class LibretroHost
	{
		private static readonly LibretroBridge bridge;
		private static readonly LibretroBridge.retro_procs cb_procs;

		static LibretroHost()
		{
			var resolver = new DynamicLibraryImportResolver(
				OSTailoredCode.IsUnixHost ? "libLibretroBridge.so" : "libLibretroBridge.dll", hasLimitedLifetime: false);

			bridge = BizInvoker.GetInvoker<LibretroBridge>(resolver, CallingConventionAdapters.Native);
			bridge.LibretroBridge_GetRetroProcs(out cb_procs);
		}

		private readonly LibretroApi api;
		private readonly IntPtr cbHandler;

		private class BridgeGuard(IntPtr parentHandler) : IMonitor
		{
			private static readonly object _sync = new();
			private static IntPtr _activeHandler;
			private static int _refCount;

			public void Enter()
			{
				lock (_sync)
				{
					if (_activeHandler == IntPtr.Zero)
					{
						_activeHandler = parentHandler;
						bridge.LibretroBridge_SetGlobalCallbackHandler(parentHandler);
					}
					else if (_activeHandler != parentHandler)
					{
						throw new InvalidOperationException("Multiple callback handlers cannot be active at once!");
					}

					_refCount++;
				}
			}

			public void Exit()
			{
				lock (_sync)
				{
					if (_refCount <= 0)
					{
						throw new InvalidOperationException($"Invalid {nameof(_refCount)}");
					}
					else
					{
						_refCount--;
						if (_refCount == 0)
						{
							_activeHandler = IntPtr.Zero;
							bridge.LibretroBridge_SetGlobalCallbackHandler(IntPtr.Zero);
						}
					}
				}
			}
		}

		public LibretroHost(CoreComm comm, IGameInfo game, string corePath, bool analysis = false)
		{
			try
			{
				cbHandler = bridge.LibretroBridge_CreateCallbackHandler();

				if (cbHandler == IntPtr.Zero)
				{
					throw new Exception("Failed to create callback handler!");
				}

				var guard = new BridgeGuard(cbHandler);
				api = BizInvoker.GetInvoker<LibretroApi>(
					new DynamicLibraryImportResolver(corePath, hasLimitedLifetime: false), guard, CallingConventionAdapters.Native);

				_serviceProvider = new(this);

				if (api.retro_api_version() != 1)
				{
					throw new InvalidOperationException("Unsupported Libretro API version (or major error in interop)");
				}

				bridge.LibretroBridge_SetDirectories(cbHandler,
					comm.CoreFileProvider.GetRetroSystemPath(game),
					comm.CoreFileProvider.GetRetroSaveRAMDirectory(game),
					Path.GetDirectoryName(corePath),
					Path.GetDirectoryName(corePath));

				ControllerDefinition = ControllerDef;
				_notify = comm.Notify;

				// check if we're just analysing the core and the core path matches the loaded core path anyways
				if (analysis && corePath == LoadedCorePath)
				{
					Description = CalculateDescription();
					Description.SupportsNoGame = LoadedCoreSupportsNoGame;
				}
				else
				{
					api.retro_set_environment(cb_procs.retro_environment_proc);
					Description = CalculateDescription();
				}

				if (!analysis)
				{
					LoadedCorePath = corePath;
					LoadedCoreSupportsNoGame = Description.SupportsNoGame;
				}
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		private class RetroData(object o, long len = 0) : IDisposable
		{
			private GCHandle _handle = GCHandle.Alloc(o, GCHandleType.Pinned);

			public IntPtr PinnedData => _handle.AddrOfPinnedObject();
			public long Length { get; } = len;

			public void Dispose() => _handle.Free();
		}

		private static byte[] RetroString(string managedString)
		{
			var ret = Encoding.UTF8.GetBytes(managedString);
			Array.Resize(ref ret, ret.Length + 1);
			return ret;
		}

		private LibretroApi.retro_system_av_info av_info;

		public RetroDescription Description { get; }

		// single instance hacks
		private static string LoadedCorePath { get; set; }
		private static bool LoadedCoreSupportsNoGame { get; set; }

		private enum RETRO_LOAD
		{
			DATA,
			PATH,
			NO_GAME,
		}

		public bool LoadData(byte[] data, string id)
		{
			using RetroData retroPath = new(RetroString(id));
			using RetroData retroData = new(data, data.Length);
			return LoadHandler(RETRO_LOAD.DATA, retroPath, retroData);
		}

		public bool LoadPath(string path)
		{
			using RetroData retroPath = new(RetroString(path));
			return LoadHandler(RETRO_LOAD.PATH, retroPath);
		}

		public bool LoadNoGame() => LoadHandler(RETRO_LOAD.NO_GAME);

		private bool LoadHandler(RETRO_LOAD which, RetroData path = null, RetroData data = null)
		{
			api.retro_init();
			bool success;
			LibretroApi.retro_game_info game;

			switch (which)
			{
				case RETRO_LOAD.NO_GAME:
					success = api.retro_load_no_game();
					break;
				case RETRO_LOAD.PATH:
					game = new() { path = path!.PinnedData };
					success = api.retro_load_game(ref game);
					break;
				case RETRO_LOAD.DATA:
					game = new() { path = path!.PinnedData, data = data!.PinnedData, size = data.Length };
					success = api.retro_load_game(ref game);
					break;
				default:
					api.retro_deinit();
					throw new InvalidOperationException($"Invalid {nameof(RETRO_LOAD)} sent?");
			}

			if (!success)
			{
				api.retro_deinit();
				return false;
			}

			inited = true;

			av_info = default;
			api.retro_get_system_av_info(ref av_info);

			api.retro_set_video_refresh(cb_procs.retro_video_refresh_proc);
			api.retro_set_audio_sample(cb_procs.retro_audio_sample_proc);
			api.retro_set_audio_sample_batch(cb_procs.retro_audio_sample_batch_proc);
			api.retro_set_input_poll(cb_procs.retro_input_poll_proc);
			api.retro_set_input_state(cb_procs.retro_input_state_proc);

			var len = checked((int)api.retro_serialize_size());
			if (len > 0)
			{
				_stateBuf = new byte[len];
			}
			else
			{
				_serviceProvider.Unregister<IStatable>();
			}

			_region = api.retro_get_region();

			// this stuff can only happen after the game is loaded

			// allocate a video buffer which will definitely be large enough (if it isn't, that's the core's fault)
			InitVideoBuffer((int)av_info.geometry.base_width, (int)av_info.geometry.base_height,
				(int)(av_info.geometry.max_width * av_info.geometry.max_height));

			// this is no good if fps is >= 215
			VsyncNumerator = checked((int)(10000000 * av_info.timing.fps));
			VsyncDenominator = 10000000;

			SetupResampler(av_info.timing.sample_rate);

			Console.WriteLine("FPS {0} SPS {1}", av_info.timing.fps, av_info.timing.sample_rate);

			InitMemoryDomains(); // im going to assume this should happen when a game is loaded

			return true;
		}

		public RetroDescription CalculateDescription()
		{
			var descr = new RetroDescription();
			api.retro_get_system_info(out var sys_info);
			descr.LibraryName = Mershul.PtrToStringUtf8(sys_info.library_name);
			descr.LibraryVersion = Mershul.PtrToStringUtf8(sys_info.library_version);
			descr.ValidExtensions = Mershul.PtrToStringUtf8(sys_info.valid_extensions);
			descr.NeedsRomAsPath = sys_info.need_fullpath;
			descr.NeedsArchives = sys_info.block_extract;
			descr.SupportsNoGame = bridge.LibretroBridge_GetSupportsNoGame(cbHandler);
			return descr;
		}
	}
}
