using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Libretro
{
	[PortedCore(CoreNames.Libretro, "CasualPokePlayer", singleInstance: true, isReleased: false)]
	[ServiceNotApplicable(new[] { typeof(IDriveLight) })]
	public partial class LibretroEmulator : IEmulator
	{
		private static readonly LibretroBridge bridge;
		private static readonly LibretroBridge.retro_procs cb_procs;

		static LibretroEmulator()
		{
			var resolver = new DynamicLibraryImportResolver(
				OSTailoredCode.IsUnixHost ? "libLibretroBridge.so" : "libLibretroBridge.dll", hasLimitedLifetime: false);

			bridge = BizInvoker.GetInvoker<LibretroBridge>(resolver, CallingConventionAdapters.Native);

			cb_procs = new();
			bridge.LibretroBridge_GetRetroProcs(ref cb_procs);
		}

		private readonly LibretroApi api;

		private readonly BasicServiceProvider _serviceProvider;
		public IEmulatorServiceProvider ServiceProvider => _serviceProvider;

		private readonly IntPtr cbHandler;

		// please call this before calling any retro functions
		private void UpdateCallbackHandler()
		{
			bridge.LibretroBridge_SetGlobalCallbackHandler(cbHandler);
		}

		public LibretroEmulator(CoreComm comm, IGameInfo game, string corePath, bool analysis = false)
		{
			try
			{
				cbHandler = bridge.LibretroBridge_CreateCallbackHandler();

				if (cbHandler == IntPtr.Zero)
				{
					throw new Exception("Failed to create callback handler!");
				}

				UpdateCallbackHandler();

				api = BizInvoker.GetInvoker<LibretroApi>(
					new DynamicLibraryImportResolver(corePath, hasLimitedLifetime: false), CallingConventionAdapters.Native);

				_serviceProvider = new(this);
				Comm = comm;

				if (api.retro_api_version() != 1)
				{
					throw new InvalidOperationException("Unsupported Libretro API version (or major error in interop)");
				}

				var SystemDirectory = RetroString(Comm.CoreFileProvider.GetRetroSystemPath(game));
				var SaveDirectory = RetroString(Comm.CoreFileProvider.GetRetroSaveRAMDirectory(game));
				var CoreDirectory = RetroString(Path.GetDirectoryName(corePath));
				var CoreAssetsDirectory = RetroString(Path.GetDirectoryName(corePath));

				bridge.LibretroBridge_SetDirectories(cbHandler, SystemDirectory, SaveDirectory, CoreDirectory, CoreAssetsDirectory);

				ControllerDefinition = CreateControllerDefinition();

				// check if we're just analysing the core and the core path matches the loaded core path anyways
				if (analysis && corePath == LoadedCorePath)
				{
					Description = CalculateDescription();
					Description.SupportsNoGame = LoadedCoreSupportsNoGame;
					// don't set init, we don't want the core deinit later
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

		private class RetroData
		{
			private readonly GCHandle _handle;

			public IntPtr PinnedData => _handle.AddrOfPinnedObject();
			public long Length { get; }

			public RetroData(object o, long len = 0)
			{
				_handle = GCHandle.Alloc(o, GCHandleType.Pinned);
				Length = len;
			}

			~RetroData() => _handle.Free();
		}

		private byte[] RetroString(string managedString)
		{
			var s = Encoding.UTF8.GetBytes(managedString);
			var ret = new byte[s.Length + 1];
			Array.Copy(s, ret, s.Length);
			ret[s.Length] = 0;
			return ret;
		}

		private LibretroApi.retro_system_av_info av_info;

		private bool inited = false;

		public void Dispose()
		{
			UpdateCallbackHandler();

			if (inited)
			{
				api.retro_deinit();
				api.retro_unload_game();
				inited = false;
			}

			bridge.LibretroBridge_DestroyCallbackHandler(cbHandler);

			_blipL?.Dispose();
			_blipR?.Dispose();
		}

		public RetroDescription Description { get; }

		// single instance hacks
		private static string LoadedCorePath { get; set; }
		private static bool LoadedCoreSupportsNoGame { get; set; }

		public enum RETRO_LOAD
		{
			DATA,
			PATH,
			NO_GAME,
		}

		public bool LoadData(byte[] data, string id) => LoadHandler(RETRO_LOAD.DATA, new(RetroString(id)), new(data, data.LongLength));

		public bool LoadPath(string path) => LoadHandler(RETRO_LOAD.PATH, new(RetroString(path)));

		public bool LoadNoGame() => LoadHandler(RETRO_LOAD.NO_GAME);

		private unsafe bool LoadHandler(RETRO_LOAD which, RetroData path = null, RetroData data = null)
		{
			UpdateCallbackHandler();

			var game = new LibretroApi.retro_game_info();
			var gameptr = (IntPtr)(&game);

			if (which == RETRO_LOAD.NO_GAME)
			{
				gameptr = IntPtr.Zero;
			}
			else
			{
				game.path = path.PinnedData;
				if (which == RETRO_LOAD.DATA)
				{
					game.data = data.PinnedData;
					game.size = data.Length;
				}
			}

			api.retro_init();
			bool success = api.retro_load_game(gameptr);
			if (!success)
			{
				api.retro_deinit();
				return false;
			}

			var av = new LibretroApi.retro_system_av_info();
			api.retro_get_system_av_info((IntPtr)(&av));
			av_info = av;

			api.retro_set_video_refresh(cb_procs.retro_video_refresh_proc);
			api.retro_set_audio_sample(cb_procs.retro_audio_sample_proc);
			api.retro_set_audio_sample_batch(cb_procs.retro_audio_sample_batch_proc);
			api.retro_set_input_poll(cb_procs.retro_input_poll_proc);
			api.retro_set_input_state(cb_procs.retro_input_state_proc);

			_stateBuf = new byte[_stateLen = api.retro_serialize_size()];

			_region = api.retro_get_region();

			//this stuff can only happen after the game is loaded

			//allocate a video buffer which will definitely be large enough
			InitVideoBuffer((int)av.base_width, (int)av.base_height, (int)(av.max_width * av.max_height));

			// TODO: more precise
			VsyncNumerator = (int)(10000000 * av.fps);
			VsyncDenominator = 10000000;

			SetupResampler(av.fps, av.sample_rate);

			InitMemoryDomains(); // im going to assume this should happen when a game is loaded

			inited = true;

			return true;
		}

		private LibretroApi.retro_message retro_msg = new();

		private CoreComm Comm { get; }

		private void FrameAdvancePrep(IController controller)
		{
			UpdateInput(controller);
		}

		private void FrameAdvancePost(bool render, bool renderSound)
		{
			if (render)
			{
				UpdateVideoBuffer();
			}

			ProcessSound();
			if (!renderSound)
			{
				DiscardSamples();
			}

			bridge.LibretroBridge_GetRetroMessage(cbHandler, ref retro_msg);
			if (retro_msg.frames > 0)
			{
				Comm.Notify(Mershul.PtrToStringUtf8(retro_msg.msg));
			}
		}

		public bool FrameAdvance(IController controller, bool render, bool renderSound = true)
		{
			UpdateCallbackHandler();

			FrameAdvancePrep(controller);
			api.retro_run();
			FrameAdvancePost(render, renderSound);

			Frame++;

			return true;
		}

		public static ControllerDefinition CreateControllerDefinition()
		{
			ControllerDefinition definition = new("LibRetro Controls"/*for compatibility*/);

			foreach (var item in new[] {
					"P1 {0} Up", "P1 {0} Down", "P1 {0} Left", "P1 {0} Right", "P1 {0} Select", "P1 {0} Start", "P1 {0} Y", "P1 {0} B", "P1 {0} X", "P1 {0} A", "P1 {0} L", "P1 {0} R",
					"P2 {0} Up", "P2 {0} Down", "P2 {0} Left", "P2 {0} Right", "P2 {0} Select", "P2 {0} Start", "P2 {0} Y", "P2 {0} B", "P2 {0} X", "P2 {0} A", "P2 {0} L", "P2 {0} R",
			})
				definition.BoolButtons.Add(string.Format(item, "RetroPad"));

			definition.BoolButtons.Add("Pointer Pressed"); //TODO: this isnt showing up in the binding panel. I don't want to find out why.
			definition.AddXYPair("Pointer {0}", AxisPairOrientation.RightAndUp, (-32767).RangeTo(32767), 0);

			foreach (var key in new[]{
				"Key Backspace", "Key Tab", "Key Clear", "Key Return", "Key Pause", "Key Escape",
				"Key Space", "Key Exclaim", "Key QuoteDbl", "Key Hash", "Key Dollar", "Key Ampersand", "Key Quote", "Key LeftParen", "Key RightParen", "Key Asterisk", "Key Plus", "Key Comma", "Key Minus", "Key Period", "Key Slash",
				"Key 0", "Key 1", "Key 2", "Key 3", "Key 4", "Key 5", "Key 6", "Key 7", "Key 8", "Key 9",
				"Key Colon", "Key Semicolon", "Key Less", "Key Equals", "Key Greater", "Key Question", "Key At", "Key LeftBracket", "Key Backslash", "Key RightBracket", "Key Caret", "Key Underscore", "Key Backquote",
				"Key A", "Key B", "Key C", "Key D", "Key E", "Key F", "Key G", "Key H", "Key I", "Key J", "Key K", "Key L", "Key M", "Key N", "Key O", "Key P", "Key Q", "Key R", "Key S", "Key T", "Key U", "Key V", "Key W", "Key X", "Key Y", "Key Z",
				"Key Delete",
				"Key KP0", "Key KP1", "Key KP2", "Key KP3", "Key KP4", "Key KP5", "Key KP6", "Key KP7", "Key KP8", "Key KP9",
				"Key KP_Period", "Key KP_Divide", "Key KP_Multiply", "Key KP_Minus", "Key KP_Plus", "Key KP_Enter", "Key KP_Equals",
				"Key Up", "Key Down", "Key Right", "Key Left", "Key Insert", "Key Home", "Key End", "Key PageUp", "Key PageDown",
				"Key F1", "Key F2", "Key F3", "Key F4", "Key F5", "Key F6", "Key F7", "Key F8", "Key F9", "Key F10", "Key F11", "Key F12", "Key F13", "Key F14", "Key F15",
				"Key NumLock", "Key CapsLock", "Key ScrollLock", "Key RShift", "Key LShift", "Key RCtrl", "Key LCtrl", "Key RAlt", "Key LAlt", "Key RMeta", "Key LMeta", "Key LSuper", "Key RSuper", "Key Mode", "Key Compose",
				"Key Help", "Key Print", "Key SysReq", "Key Break", "Key Menu", "Key Power", "Key Euro", "Key Undo"
			})
			{
				definition.BoolButtons.Add(key);
				definition.CategoryLabels[key] = "RetroKeyboard";
			}

			return definition.MakeImmutable();
		}

		public ControllerDefinition ControllerDefinition { get; }
		public int Frame { get; set; }
		public string SystemId => VSystemID.Raw.Libretro;
		public bool DeterministicEmulation => false;

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		public unsafe RetroDescription CalculateDescription()
		{
			UpdateCallbackHandler();

			var descr = new RetroDescription();
			var sys_info = new LibretroApi.retro_system_info();
			api.retro_get_system_info((IntPtr)(&sys_info));
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
