using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

				ControllerDefinition = ControllerDef;

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
			InitVideoBuffer((int)av.geometry.base_width, (int)av.geometry.base_height, (int)(av.geometry.max_width * av.geometry.max_height));

			// TODO: more precise
			VsyncNumerator = (int)(10000000 * av.timing.fps);
			VsyncDenominator = 10000000;

			SetupResampler(av.timing.fps, av.timing.sample_rate);

			InitMemoryDomains(); // im going to assume this should happen when a game is loaded

			inited = true;

			return true;
		}

		private LibretroApi.retro_message retro_msg = new();

		private CoreComm Comm { get; }

		private void FrameAdvancePrep(IController controller)
		{
			UpdateInput(controller);

			if (controller.IsPressed("Reset"))
			{
				api.retro_reset();
			}
		}

		private void FrameAdvancePost(bool render, bool renderSound)
		{
			if (bridge.LibretroBridge_GetRetroGeometryInfo(cbHandler, ref av_info.geometry))
			{
				vidBuffer = new int[av_info.geometry.max_width * av_info.geometry.max_height];
			}

			if (bridge.LibretroBridge_GetRetroTimingInfo(cbHandler, ref av_info.timing))
			{
				VsyncNumerator = (int)(10000000 * av_info.timing.fps);
				_blipL.SetRates(av_info.timing.sample_rate, 44100);
				_blipR.SetRates(av_info.timing.sample_rate, 44100);
			}

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

		private static readonly LibretroControllerDef ControllerDef = new();

		public class LibretroControllerDef : ControllerDefinition
		{
			private const string CAT_KEYBOARD = "RetroKeyboard";

			public const string PFX_RETROPAD = "RetroPad ";

			public LibretroControllerDef()
				: base(name: "LibRetro Controls"/*for compatibility*/)
			{
				for (var player = 1; player <= 2; player++) foreach (var button in new[] { "Up", "Down", "Left", "Right", "Select", "Start", "Y", "B", "X", "A", "L", "R" })
				{
					BoolButtons.Add($"P{player} {PFX_RETROPAD}{button}");
				}

				BoolButtons.Add("Pointer Pressed");
				this.AddXYPair("Pointer {0}", AxisPairOrientation.RightAndUp, (-32767).RangeTo(32767), 0);

				foreach (var s in new[] {
					"Backspace", "Tab", "Clear", "Return", "Pause", "Escape",
					"Space", "Exclaim", "QuoteDbl", "Hash", "Dollar", "Ampersand", "Quote", "LeftParen", "RightParen", "Asterisk", "Plus", "Comma", "Minus", "Period", "Slash",
					"0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
					"Colon", "Semicolon", "Less", "Equals", "Greater", "Question", "At", "LeftBracket", "Backslash", "RightBracket", "Caret", "Underscore", "Backquote",
					"A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
					"Delete",
					"KP0", "KP1", "KP2", "KP3", "KP4", "KP5", "KP6", "KP7", "KP8", "KP9",
					"KP_Period", "KP_Divide", "KP_Multiply", "KP_Minus", "KP_Plus", "KP_Enter", "KP_Equals",
					"Up", "Down", "Right", "Left", "Insert", "Home", "End", "PageUp", "PageDown",
					"F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12", "F13", "F14", "F15",
					"NumLock", "CapsLock", "ScrollLock", "RShift", "LShift", "RCtrl", "LCtrl", "RAlt", "LAlt", "RMeta", "LMeta", "LSuper", "RSuper", "Mode", "Compose",
					"Help", "Print", "SysReq", "Break", "Menu", "Power", "Euro", "Undo"
				})
				{
					var buttonName = $"Key {s}";
					BoolButtons.Add(buttonName);
					CategoryLabels[buttonName] = CAT_KEYBOARD;
				}

				BoolButtons.Add("Reset");

				MakeImmutable();
			}

			protected override IReadOnlyList<IReadOnlyList<string>> GenOrderedControls()
			{
				// all this is to remove the keyboard buttons from P0 and put them in P3 so they appear at the end of the input display
				var players = base.GenOrderedControls().ToList();
				List<string> retroKeyboard = new();
				var p0 = (List<string>) players[0];
				for (var i = 0; i < p0.Count; /* incremented in body */)
				{
					var buttonName = p0[i];
					if (CategoryLabels.TryGetValue(buttonName, out var v) && v is CAT_KEYBOARD)
					{
						retroKeyboard.Add(buttonName);
						p0.RemoveAt(i);
					}
					else
					{
						i++;
					}
				}
				players.Add(retroKeyboard);
				return players;
			}
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
