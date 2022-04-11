using System;
using System.Collections.Generic;
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
	public partial class LibretroEmulator : IEmulator, ISaveRam, IStatable, IVideoProvider, IInputPollable, IRegionable
	{
		private readonly LibretroApi api;

		private readonly LibretroApi.retro_environment_t retro_environment_cb;
		private LibretroApi.retro_video_refresh_t retro_video_refresh_cb;
		private LibretroApi.retro_audio_sample_t retro_audio_sample_cb;
		private LibretroApi.retro_audio_sample_batch_t retro_audio_sample_batch_cb;
		private LibretroApi.retro_input_poll_t retro_input_poll_cb;
		private LibretroApi.retro_input_state_t retro_input_state_cb;

		private readonly BasicServiceProvider _serviceProvider;
		public IEmulatorServiceProvider ServiceProvider => _serviceProvider;

		public LibretroEmulator(CoreComm comm, IGameInfo game, string corePath, bool analysis = false)
		{
			api = BizInvoker.GetInvoker<LibretroApi>(
				new DynamicLibraryImportResolver(corePath, hasLimitedLifetime: false), CallingConventionAdapters.Native);

			_serviceProvider = new(this);
			Comm = comm;

			if (api.retro_api_version() != 1)
			{
				throw new InvalidOperationException("Unsupported Libretro API version (or major error in interop)");
			}

			SystemDirectory = new(Comm.CoreFileProvider.GetRetroSystemPath(game));
			SaveDirectory = new(Comm.CoreFileProvider.GetRetroSaveRAMDirectory(game));
			CoreDirectory = new(Path.GetDirectoryName(corePath));
			CoreAssetsDirectory = new(Path.GetDirectoryName(corePath));

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
				api.retro_set_environment(retro_environment_cb = retro_environment);
				api.retro_init();
				Description = CalculateDescription();
				inited = true;
			}

			if (!analysis)
			{
				LoadedCorePath = corePath;
				LoadedCoreSupportsNoGame = Description.SupportsNoGame;
			}
		}

		private class RetroData
		{
			private readonly GCHandle _handle;
			private readonly long _length;

			public IntPtr PinnedData => _handle.AddrOfPinnedObject();
			public long Length { get; }

			public RetroData(object o, long len)
			{
				_handle = GCHandle.Alloc(o, GCHandleType.Pinned);
				Length = len;
			}

			~RetroData() => _handle.Free();
		}

		private class RetroString
		{
			private readonly RetroData _data;
			public IntPtr PinnedString => _data.PinnedData;

			public RetroString(string managedString)
			{
				var s = Encoding.UTF8.GetBytes(managedString);
				var b = new byte[s.Length + 1];
				Array.Copy(s, b, s.Length);
				b[s.Length] = 0;
				_data = new(b, b.LongLength);
			}
		}

		// environment vars

		private uint rotation_ccw = 0;
		private LibretroApi.RETRO_PIXEL_FORMAT pixel_format = 0;
		private bool variables_dirty = false;
		private int variable_count = 0;
		private string[] variable_keys = null;
		private string[] variable_comments = null;
		private readonly List<RetroString> variables = new();
		private bool support_no_game = false;
		private LibretroApi.retro_system_av_info av_info;
		private LibretroApi.RETRO_REGION _region = LibretroApi.RETRO_REGION.NTSC;

		private readonly RetroString SystemDirectory;
		private readonly RetroString SaveDirectory;
		private readonly RetroString CoreDirectory;
		private readonly RetroString CoreAssetsDirectory;

		public DisplayType Region
		{
			get
			{
				return _region switch
				{
					LibretroApi.RETRO_REGION.NTSC => DisplayType.NTSC,
					LibretroApi.RETRO_REGION.PAL => DisplayType.PAL,
					_ => DisplayType.NTSC,
				};
			}
		}

		private unsafe bool retro_environment(LibretroApi.RETRO_ENVIRONMENT cmd, IntPtr data)
		{
			switch (cmd)
			{
				case LibretroApi.RETRO_ENVIRONMENT.SET_ROTATION:
					rotation_ccw = (*(uint*)data) * 90;
					return true;
				case LibretroApi.RETRO_ENVIRONMENT.GET_OVERSCAN:
					*(bool*)data = false;
					return true;
				case LibretroApi.RETRO_ENVIRONMENT.GET_CAN_DUPE:
					*(bool*)data = true;
					return true;
				case LibretroApi.RETRO_ENVIRONMENT.SET_MESSAGE:
				{
					var message = (LibretroApi.retro_message*)data;
					retro_message_string = Mershul.PtrToStringUtf8(message->msg);
					retro_message_time = message->frames;
					return true;
				}
				case LibretroApi.RETRO_ENVIRONMENT.SHUTDOWN:
					//TODO low priority
					return false;
				case LibretroApi.RETRO_ENVIRONMENT.SET_PERFORMANCE_LEVEL:
					//unneeded
					return false;
				case LibretroApi.RETRO_ENVIRONMENT.GET_SYSTEM_DIRECTORY:
					*(byte**)data = (byte*)SystemDirectory.PinnedString;
					return true;
				case LibretroApi.RETRO_ENVIRONMENT.SET_PIXEL_FORMAT:
					pixel_format = *(LibretroApi.RETRO_PIXEL_FORMAT*)data;
					return true;
				case LibretroApi.RETRO_ENVIRONMENT.SET_INPUT_DESCRIPTORS:
					//TODO medium priority
					return false;
				case LibretroApi.RETRO_ENVIRONMENT.SET_KEYBOARD_CALLBACK:
					//TODO high priority (to support keyboard consoles, probably high value for us. but that may take a lot of infrastructure work)
					return false;
				case LibretroApi.RETRO_ENVIRONMENT.SET_DISK_CONTROL_INTERFACE:
					//TODO high priority (to support disc systems)
					return false;
				case LibretroApi.RETRO_ENVIRONMENT.SET_HW_RENDER:
					//TODO high priority (to support 3d renderers
					return false;

				case LibretroApi.RETRO_ENVIRONMENT.GET_VARIABLE:
				{
					//according to retroarch's `core_option_manager_get` this is what we should do

					variables_dirty = false;

					var req = (LibretroApi.retro_variable*)data;
					req->value = IntPtr.Zero;

					for (int i = 0; i < variable_count; i++)
					{
						if (variable_keys[i] == Mershul.PtrToStringUtf8(req->key))
						{
							req->value = variables[i].PinnedString;
							return true;
						}
					}

					return true;
				}

				case LibretroApi.RETRO_ENVIRONMENT.SET_VARIABLES:
				{
					var var = (LibretroApi.retro_variable*)data;
					int nVars = 0;
					while (var->key != IntPtr.Zero)
					{
						var++;
						nVars++;
					}

					variables.Clear();
					variable_count = nVars;
					variable_keys = new string[nVars];
					variable_comments = new string[nVars];
					var = (LibretroApi.retro_variable*)data;
					for (int i = 0; i < nVars; i++)
					{
						variable_keys[i] = Mershul.PtrToStringUtf8(var[i].key);
						variable_comments[i] = Mershul.PtrToStringUtf8(var[i].value);

						//analyze to find default and save it
						string comment = variable_comments[i];
						var ofs = comment.IndexOf(';') + 2;
						var pipe = comment.IndexOf('|', ofs);
						if (pipe == -1)
						{
							variables.Add(new(comment.Substring(ofs)));
						}
						else
						{
							variables.Add(new(comment.Substring(ofs, pipe - ofs)));
						}
					}

					return true;
				}

				case LibretroApi.RETRO_ENVIRONMENT.GET_VARIABLE_UPDATE:
					*(bool*)data = variables_dirty;
					break;
				case LibretroApi.RETRO_ENVIRONMENT.SET_SUPPORT_NO_GAME:
					support_no_game = *(bool*)data;
					break;
				case LibretroApi.RETRO_ENVIRONMENT.GET_LIBRETRO_PATH:
					*(byte**)data = (byte*)CoreDirectory.PinnedString;
					return true;
				case LibretroApi.RETRO_ENVIRONMENT.SET_AUDIO_CALLBACK:
					//dont know what to do with this yet
					return false;
				case LibretroApi.RETRO_ENVIRONMENT.SET_FRAME_TIME_CALLBACK:
					//dont know what to do with this yet
					return false;
				case LibretroApi.RETRO_ENVIRONMENT.GET_RUMBLE_INTERFACE:
					//TODO low priority
					return false;
				case LibretroApi.RETRO_ENVIRONMENT.GET_INPUT_DEVICE_CAPABILITIES:
					//TODO medium priority - other input methods
					*(ulong*)data = 1 << (int)LibretroApi.RETRO_DEVICE.JOYPAD;
					return true;
				case LibretroApi.RETRO_ENVIRONMENT.GET_LOG_INTERFACE:
					var cb = (LibretroApi.retro_log_callback*)data;
					cb->log = IntPtr.Zero; // we can't do this from C#, although cores will have a fallback log anyways so not a big deal
					return false;
				case LibretroApi.RETRO_ENVIRONMENT.GET_PERF_INTERFACE:
					// uhhhh what the fuck is this for
					return false;
				case LibretroApi.RETRO_ENVIRONMENT.GET_LOCATION_INTERFACE:
					//TODO low priority
					return false;
				case LibretroApi.RETRO_ENVIRONMENT.GET_CORE_ASSETS_DIRECTORY:
					*(byte**)data = (byte*)CoreAssetsDirectory.PinnedString;
					return true;
				case LibretroApi.RETRO_ENVIRONMENT.GET_SAVE_DIRECTORY:
					*(byte**)data = (byte*)SaveDirectory.PinnedString;
					return true;
				case LibretroApi.RETRO_ENVIRONMENT.SET_SYSTEM_AV_INFO:
					Console.WriteLine("NEED RETRO_ENVIRONMENT.SET_SYSTEM_AV_INFO");
					return false;
				case LibretroApi.RETRO_ENVIRONMENT.SET_PROC_ADDRESS_CALLBACK:
					// uhhhh what the fuck is this for
					return true;
				case LibretroApi.RETRO_ENVIRONMENT.SET_SUBSYSTEM_INFO:
					//needs retro_load_game_special to be useful; not supported yet
					return false;
				case LibretroApi.RETRO_ENVIRONMENT.SET_CONTROLLER_INFO:
					//TODO medium priority probably
					return false;
				case LibretroApi.RETRO_ENVIRONMENT.SET_GEOMETRY:
					// uhhhh what the fuck is this for
					return true;
				case LibretroApi.RETRO_ENVIRONMENT.GET_USERNAME:
					//we definitely want to return false here so the core will do something deterministic
					return false;
				case LibretroApi.RETRO_ENVIRONMENT.GET_LANGUAGE:
					*(uint*)data = (uint)LibretroApi.RETRO_LANGUAGE.ENGLISH;
					return true;
			}

			return false;
		}

		private bool inited = false;
		private bool game_loaded = false;

		public void Dispose()
		{
			if (game_loaded)
			{
				api.retro_unload_game();
				game_loaded = false;
			}

			if (inited)
			{
				api.retro_deinit();
				inited = false;
			}

			_resampler?.Dispose();
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

		public bool LoadData(byte[] data, string id)
		{
			LoadHandler(RETRO_LOAD.DATA, new(id), new(data, data.LongLength));
			return true;
		}

		public bool LoadPath(string path)
		{
			LoadHandler(RETRO_LOAD.PATH, new(path));
			return true;
		}

		public bool LoadNoGame()
		{
			LoadHandler(RETRO_LOAD.NO_GAME);
			return true;
		}

		private unsafe void LoadHandler(RETRO_LOAD which, RetroString path = null, RetroData data = null)
		{
			var game = new LibretroApi.retro_game_info();
			var gameptr = (IntPtr)(&game);

			if (which == RETRO_LOAD.NO_GAME)
			{
				gameptr = IntPtr.Zero;
			}
			else
			{
				game.path = path.PinnedString;
				if (which == RETRO_LOAD.DATA)
				{
					game.data = data.PinnedData;
					game.size = data.Length;
				}
			}

			api.retro_load_game(gameptr);

			var av = new LibretroApi.retro_system_av_info();
			api.retro_get_system_av_info((IntPtr)(&av));
			av_info = av;

			api.retro_set_video_refresh(retro_video_refresh_cb = retro_video_refresh);
			api.retro_set_audio_sample(retro_audio_sample_cb = retro_audio_sample);
			api.retro_set_audio_sample_batch(retro_audio_sample_batch_cb = retro_audio_sample_batch);
			api.retro_set_input_poll(retro_input_poll_cb = retro_input_poll);
			api.retro_set_input_state(retro_input_state_cb = retro_input_state);

			_stateBuf = new byte[_stateLen = api.retro_serialize_size()];

			_region = api.retro_get_region();

			//this stuff can only happen after the game is loaded

			//allocate a video buffer which will definitely be large enough
			SetVideoBuffer((int)av.base_width, (int)av.base_height);
			vidBuffer = new int[av.max_width * av.max_height];

			// TODO: more precise
			VsyncNumerator = (int)(10000000 * av.fps);
			VsyncDenominator = 10000000;

			SetupResampler(av.fps, av.sample_rate);
			_serviceProvider.Register<ISoundProvider>(_resampler);

			InitMemoryDomains(); // im going to assume this should happen when a game is loaded

			game_loaded = true;
		}

		public IInputCallbackSystem InputCallbacks => _inputCallbacks;
		private readonly InputCallbackSystem _inputCallbacks = new();

		private IController _controller;

		private string retro_message_string = "";
		private uint retro_message_time = 0;
		private CoreComm Comm { get; }

		public bool FrameAdvance(IController controller, bool render, bool rendersound)
		{
			_controller = controller;
			api.retro_run();
			Frame++;

			if (retro_message_time > 0)
			{
				Comm.Notify(retro_message_string);
				retro_message_time--;
			}

			return true;
		}

		private int[] vidBuffer;
		private int vidWidth = -1, vidHeight = -1;

		private void SetVideoBuffer(int width, int height)
		{
			//actually, we've already allocated a buffer with the given maximum size
			if (vidWidth == width && vidHeight == height) return;
			vidWidth = width;
			vidHeight = height;
		}

		//video provider
		public int BackgroundColor => 0;
		public int[] GetVideoBuffer() => vidBuffer;

		public int VirtualWidth
		{
			get
			{
				var dar = av_info.aspect_ratio;
				if (dar <= 0)
				{
					return vidWidth;
				}
				if (dar > 1.0f)
				{
					return (int)(vidHeight * dar);
				}
				return vidWidth;
			}
		}

		public int VirtualHeight
		{
			get
			{
				var dar = av_info.aspect_ratio;
				if (dar <= 0)
				{
					return vidHeight;
				}
				if (dar < 1.0f)
				{
					return (int)(vidWidth / dar);
				}
				return vidHeight;
			}
		}

		public int BufferWidth => vidWidth;
		public int BufferHeight => vidHeight;

		public int VsyncNumerator { get; private set; }
		public int VsyncDenominator { get; private set; }

		private static unsafe int* address(uint rot, uint width, uint height, long pitch, int x, int y, int* dstbuf, int* optimize0dst)
		{
			switch (rot)
			{
				case 0:
					return optimize0dst;
				case 90:
					//TODO:
					return optimize0dst;
				case 180:
					//TODO:
					return optimize0dst;
				case 270:
					{
						int dx = (int)width - y - 1;
						int dy = x;
						return dstbuf + dy * width + dx;
					}
				default:
					throw new Exception();
			}
		}

		private static unsafe void Blit555(uint rot, short* srcbuf, int* dstbuf, uint width, uint height, long pitch)
		{
			int* dst = dstbuf;
			for (int y = 0; y < height; y++)
			{
				short* row = srcbuf;
				for (int x = 0; x < width; x++)
				{
					short ci = *row;
					int r = ci & 0x001f;
					int g = ci & 0x03e0;
					int b = ci & 0x7c00;

					r = (r << 3) | (r >> 2);
					g = (g >> 2) | (g >> 7);
					b = (b >> 7) | (b >> 12);
					int co = r | g | b | unchecked((int)0xff000000);

					*address(rot, width, height, pitch, x, y, dstbuf, dst) = co;
					dst++;
					row++;
				}
				srcbuf += pitch / 2;
			}
		}

		private static unsafe void Blit565(uint rot, short* srcbuf, int* dstbuf, uint width, uint height, long pitch)
		{
			int* dst = dstbuf;
			for (int y = 0; y < height; y++)
			{
				short* row = srcbuf;
				for (int x = 0; x < width; x++)
				{
					short ci = *row;
					int r = ci & 0x001f;
					int g = (ci & 0x07e0) >> 5;
					int b = (ci & 0xf800) >> 11;

					r = (r << 3) | (r >> 2);
					g = (g << 2) | (g >> 4);
					b = (b << 3) | (b >> 2);
					int co = (b << 16) | (g << 8) | r | unchecked((int)0xff000000);

					*address(rot, width, height, pitch, x, y, dstbuf, dst) = co;
					dst++;
					row++;
				}
				srcbuf += pitch / 2;
			}
		}

		private static unsafe void Blit888(uint rot, int* srcbuf, int* dstbuf, uint width, uint height, long pitch)
		{
			int* dst = dstbuf;
			for (int y = 0; y < height; y++)
			{
				int* row = srcbuf;
				for (int x = 0; x < width; x++)
				{
					int ci = *row;
					int co = ci | unchecked((int)0xff000000);
					*address(rot, width, height, pitch, x, y, dstbuf, dst) = co;
					dst++;
					row++;
				}
				srcbuf += pitch / 4;
			}
		}


		public unsafe void retro_video_refresh(IntPtr data, uint width, uint height, long pitch)
		{
			if (data == IntPtr.Zero)
			{
				return;
			}

			SetVideoBuffer((int)width, (int)height);

			fixed (int* vb = vidBuffer)
			{
				switch (pixel_format)
				{
					case LibretroApi.RETRO_PIXEL_FORMAT.ZRGB1555:
						Blit555(rotation_ccw, (short*)data, vb, width, height, pitch);
						break;
					case LibretroApi.RETRO_PIXEL_FORMAT.XRGB8888:
						Blit888(rotation_ccw, (int*)data, vb, width, height, pitch);
						break;
					case LibretroApi.RETRO_PIXEL_FORMAT.RGB565:
						Blit565(rotation_ccw, (short*)data, vb, width, height, pitch);
						break;
				}
			}
		}

		private SpeexResampler _resampler;

		private short[] _sampleBuf = new short[0];

		// debug
		private int nsamprecv = 0;

		private void SetupResampler(double fps, double sps)
		{
			Console.WriteLine("FPS {0} SPS {1}", fps, sps);

			// todo: more precise?
			uint spsnum = (uint)sps * 10000;
			uint spsden = 10000U;

			_resampler = new SpeexResampler(SpeexResampler.Quality.QUALITY_DESKTOP, 44100 * spsden, spsnum, (uint)sps, 44100, null, null);
		}

		public void retro_audio_sample(short left, short right)
		{
			_resampler.EnqueueSample(left, right);
			nsamprecv++;
		}

		public void retro_audio_sample_batch(IntPtr data, long frames)
		{
			if (_sampleBuf.Length < frames * 2)
				_sampleBuf = new short[frames * 2];
			Marshal.Copy(data, _sampleBuf, 0, (int)(frames * 2));
			_resampler.EnqueueSamples(_sampleBuf, (int)frames);
			nsamprecv += (int)frames;
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
		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }
		public string SystemId => VSystemID.Raw.Libretro;
		public bool DeterministicEmulation => false;

		public byte[] CloneSaveRam()
		{
			if (_saveramSize > 0)
			{
				var buf = new byte[_saveramSize];
				int index = 0;
				foreach (var m in _saveramAreas)
				{
					Marshal.Copy(m.Data, buf, index, (int)m.Size);
					index += (int)m.Size;
				}
				return buf;
			}

			return null;
		}

		public void StoreSaveRam(byte[] data)
		{
			if (_saveramSize > 0)
			{
				int index = 0;
				foreach (var m in _saveramAreas)
				{
					Marshal.Copy(data, index, m.Data, (int)m.Size);
					index += (int)m.Size;
				}
			}
		}

		public bool SaveRamModified => _saveramSize > 0;

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		private byte[] _stateBuf;
		private long _stateLen;

		public void SaveStateBinary(BinaryWriter writer)
		{
			_stateLen = api.retro_serialize_size();
			if (_stateBuf.LongLength != _stateLen)
			{
				_stateBuf = new byte[_stateLen];
			}

			var d = new RetroData(_stateBuf, _stateLen);
			api.retro_serialize(d.PinnedData, d.Length);
			writer.Write(_stateBuf.Length);
			writer.Write(_stateBuf);
			// other variables
			writer.Write(Frame);
			writer.Write(LagCount);
			writer.Write(IsLagFrame);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			var newlen = reader.ReadInt32();
			if (newlen > _stateBuf.Length)
			{
				throw new Exception("Unexpected buffer size");
			}

			reader.Read(_stateBuf, 0, newlen);
			var d = new RetroData(_stateBuf, _stateLen);
			api.retro_unserialize(d.PinnedData, d.Length);
			// other variables
			Frame = reader.ReadInt32();
			LagCount = reader.ReadInt32();
			IsLagFrame = reader.ReadBoolean();
		}

		private readonly List<MemoryDomain> _memoryDomains = new();
		private IMemoryDomains MemoryDomains { get; set; }

		private readonly List<MemoryDomainIntPtr> _saveramAreas = new();
		private long _saveramSize = 0;

		private void InitMemoryDomains()
		{
			foreach (LibretroApi.RETRO_MEMORY m in Enum.GetValues(typeof(LibretroApi.RETRO_MEMORY)))
			{
				var mem = api.retro_get_memory_data(m);
				var sz = api.retro_get_memory_size(m);
				if (mem != IntPtr.Zero && sz > 0)
				{
					var d = new MemoryDomainIntPtr(Enum.GetName(m.GetType(), m), MemoryDomain.Endian.Little, mem, sz, true, 1);
					_memoryDomains.Add(d);
					if (m is LibretroApi.RETRO_MEMORY.SAVE_RAM or LibretroApi.RETRO_MEMORY.RTC)
					{
						_saveramAreas.Add(d);
						_saveramSize += d.Size;
					}
				}
			}

			MemoryDomains = new MemoryDomainList(_memoryDomains);
			_serviceProvider.Register(MemoryDomains);
		}

		public unsafe RetroDescription CalculateDescription()
		{
			var descr = new RetroDescription();
			var sys_info = new LibretroApi.retro_system_info();
			api.retro_get_system_info((IntPtr)(&sys_info));
			descr.LibraryName = Mershul.PtrToStringUtf8(sys_info.library_name);
			descr.LibraryVersion = Mershul.PtrToStringUtf8(sys_info.library_version);
			descr.ValidExtensions = Mershul.PtrToStringUtf8(sys_info.valid_extensions);
			descr.NeedsRomAsPath = sys_info.need_fullpath;
			descr.NeedsArchives = sys_info.block_extract;
			descr.SupportsNoGame = support_no_game;
			return descr;
		}
	}
}
