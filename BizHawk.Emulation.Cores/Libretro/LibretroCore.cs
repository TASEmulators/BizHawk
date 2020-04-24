//TODO: it's a bit of a misnomer to call this a 'core'
//that's libretro nomenclature for a particular core (nes, genesis, doom, etc.) 
//we should call this LibretroEmulator (yeah, it was originally called that)
//Since it's an IEmulator.. but... I don't know. Yeah, that's probably best

using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Libretro
{
	[Core("Libretro", "zeromus", isPorted: false, isReleased: false)]
	[ServiceNotApplicable(new[] { typeof(IDriveLight) })]
	public unsafe partial class LibretroCore : IEmulator, ISettable<LibretroCore.Settings, LibretroCore.SyncSettings>,
		ISaveRam, IStatable, IVideoProvider, IInputPollable
	{
		private LibretroApi api;

		// TODO: codepath just for introspection (lighter weight; no speex, no controls, etc.)
		public LibretroCore(CoreComm nextComm, GameInfo game, string corePath)
		{
			ServiceProvider = new BasicServiceProvider(this);
			_SyncSettings = new SyncSettings();
			CoreComm = nextComm;

			string dllPath = Path.Combine(CoreComm.CoreFileProvider.DllPath(), "LibretroBridge.dll");
			api = new LibretroApi(dllPath, corePath);

			if (api.comm->env.retro_api_version != 1)
				throw new InvalidOperationException("Unsupported Libretro API version (or major error in interop)");

			// SO: I think I need these paths set before I call retro_set_environment
			// and I need retro_set_environment set so I can find out if the core supports no-game
			// therefore, I need a complete environment (including pathing) before I can complete my introspection of the core.
			// Sucky, but that's life.
			// I don't even know for sure what paths I should use until... (what?)


			// not sure about each of these.. but we may be doing things different than retroarch.
			// I wish I could initialize these with placeholders during a separate introspection codepath..
			string SystemDirectory = CoreComm.CoreFileProvider.GetRetroSystemPath(game);
			string SaveDirectory = CoreComm.CoreFileProvider.GetRetroSaveRAMDirectory(game);
			string CoreDirectory = Path.GetDirectoryName(corePath);
			string CoreAssetsDirectory = Path.GetDirectoryName(corePath);

			api.CopyAscii(LibretroApi.BufId.SystemDirectory, SystemDirectory);
			api.CopyAscii(LibretroApi.BufId.SaveDirectory, SaveDirectory);
			api.CopyAscii(LibretroApi.BufId.CoreDirectory, CoreDirectory);
			api.CopyAscii(LibretroApi.BufId.CoreAssetsDirectory, CoreAssetsDirectory);

			api.CMD_SetEnvironment();

			//TODO: IT'S A BOWL OF SPAGHETTI! I KNOW, IM GOING TO FIX IT
			api.core = this;

			Description = api.CalculateDescription();

			ControllerDefinition = CreateControllerDefinition(_SyncSettings);
		}

		bool disposed = false;
		public void Dispose()
		{
			if (disposed) return;
			disposed = true;

			//TODO
			//api.CMD_unload_cartridge();
			//api.CMD_term();

			resampler?.Dispose();

			api.Dispose();

			if(vidBufferHandle.IsAllocated)
				vidBufferHandle.Free();
		}

		public CoreComm CoreComm { get; }

		public RetroDescription Description { get; }

		public bool LoadData(byte[] data, string id)
		{
			bool ret = api.CMD_LoadData(data, id);
			LoadHandler();
			return ret;
		}

		public bool LoadPath(string path)
		{
			bool ret = api.CMD_LoadPath(path);
			LoadHandler();
			return ret;
		}

		public bool LoadNoGame()
		{
			bool ret = api.CMD_LoadNoGame();
			LoadHandler();
			return ret;
		}

		void LoadHandler()
		{
			//this stuff can only happen after the game is loaded

			//allocate a video buffer which will definitely be large enough
			SetVideoBuffer((int)api.comm->env.retro_system_av_info.geometry.base_width, (int)api.comm->env.retro_system_av_info.geometry.base_height);
			vidBuffer = new int[api.comm->env.retro_system_av_info.geometry.max_width * api.comm->env.retro_system_av_info.geometry.max_height];
			vidBufferHandle = GCHandle.Alloc(vidBuffer, GCHandleType.Pinned);
			api.comm->env.fb_bufptr = (int*)vidBufferHandle.AddrOfPinnedObject().ToPointer();
			//TODO: latch DAR? we may want to change it synchronously, or something

			// TODO: more precise
			VsyncNumerator = (int)(10000000 * api.comm->env.retro_system_av_info.timing.fps);
			VsyncDenominator = 10000000;

			SetupResampler(api.comm->env.retro_system_av_info.timing.fps, api.comm->env.retro_system_av_info.timing.sample_rate);
			(ServiceProvider as BasicServiceProvider).Register<ISoundProvider>(resampler);
		}

		public IEmulatorServiceProvider ServiceProvider { get; }

		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, RegisterValue>();
		}

		public IInputCallbackSystem InputCallbacks => _inputCallbacks;
		private readonly InputCallbackSystem _inputCallbacks = new InputCallbackSystem();

		public ITraceable Tracer { get; private set; }
		public IMemoryCallbackSystem MemoryCallbacks { get; private set; }

		public bool CanStep(StepType type) => false;

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();
		[FeatureNotImplemented]
		public void SetCpuRegister(string register, int value) => throw new NotImplementedException();
		[FeatureNotImplemented]
		public long TotalExecutedCycles => throw new NotImplementedException();

		private IController _controller;

		public bool FrameAdvance(IController controller, bool render, bool rendersound)
		{
			_controller = controller;
			api.CMD_Run();
			timeFrameCounter++;

			return true;
		}

		GCHandle vidBufferHandle;
		int[] vidBuffer;
		int vidWidth = -1, vidHeight = -1;

		private void SetVideoBuffer(int width, int height)
		{
			//actually, we've already allocated a buffer with the given maximum size
			if (vidWidth == width && vidHeight == height) return;
			vidWidth = width;
			vidHeight = height;
		}

		//video provider
		int IVideoProvider.BackgroundColor => 0;
		int[] IVideoProvider.GetVideoBuffer() { return vidBuffer; }

		public int VirtualWidth
		{
			get
			{
				var dar = api.AVInfo.geometry.aspect_ratio;
				if(dar<=0)
					return vidWidth;
				if (dar > 1.0f)
					return (int)(vidHeight * dar);
				return vidWidth;
			}
		}
		public int VirtualHeight
		{
			get
			{
				var dar = api.AVInfo.geometry.aspect_ratio;
				if(dar<=0)
					return vidHeight;
				if (dar < 1.0f)
					return (int)(vidWidth / dar);
				return vidHeight;
			}
		}

		public void SIG_VideoUpdate()
		{
			SetVideoBuffer(api.comm->env.fb_width, api.comm->env.fb_height);
		}

		int IVideoProvider.BufferWidth => vidWidth;
		int IVideoProvider.BufferHeight => vidHeight;

		public int VsyncNumerator { get; private set; }
		public int VsyncDenominator { get; private set; }

		#region ISoundProvider

		SpeexResampler resampler;

		short[] sampbuff = new short[0];

		// debug
		int nsamprecv = 0;

		void SetupResampler(double fps, double sps)
		{
			Console.WriteLine("FPS {0} SPS {1}", fps, sps);

			// todo: more precise?
			uint spsnum = (uint)sps * 1000;
			uint spsden = 1000U;

			resampler = new SpeexResampler(SpeexResampler.Quality.QUALITY_DESKTOP, 44100 * spsden, spsnum, (uint)sps, 44100, null, null);
		}

		//TODO: handle these in c++ (queue there and blast after frameadvance to c#)
		public void retro_audio_sample(short left, short right)
		{
			resampler.EnqueueSample(left, right);
			nsamprecv++;
		}

		public void retro_audio_sample_batch(void* data, int frames)
		{
			if (sampbuff.Length < frames * 2)
				sampbuff = new short[frames * 2];
			Marshal.Copy(new IntPtr(data), sampbuff, 0, (int)(frames * 2));
			resampler.EnqueueSamples(sampbuff, (int)frames);
			nsamprecv += (int)frames;
		}

		#endregion

		public static ControllerDefinition CreateControllerDefinition(SyncSettings syncSettings)
		{
			ControllerDefinition definition = new ControllerDefinition();
			definition.Name = "LibRetro Controls"; // <-- for compatibility

			foreach(var item in new[] {
					"P1 {0} Up", "P1 {0} Down", "P1 {0} Left", "P1 {0} Right", "P1 {0} Select", "P1 {0} Start", "P1 {0} Y", "P1 {0} B", "P1 {0} X", "P1 {0} A", "P1 {0} L", "P1 {0} R",
					"P2 {0} Up", "P2 {0} Down", "P2 {0} Left", "P2 {0} Right", "P2 {0} Select", "P2 {0} Start", "P2 {0} Y", "P2 {0} B", "P2 {0} X", "P2 {0} A", "P2 {0} L", "P2 {0} R",
			})
				definition.BoolButtons.Add(string.Format(item,"RetroPad"));

			definition.BoolButtons.Add("Pointer Pressed"); //TODO: this isnt showing up in the binding panel. I don't want to find out why.
			definition.AxisControls.Add("Pointer X");
			definition.AxisControls.Add("Pointer Y");
			definition.AxisRanges.AddRange(ControllerDefinition.CreateAxisRangePair(-32767, 0, 32767, ControllerDefinition.AxisPairOrientation.RightAndUp));

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

			return definition;
		}

		public ControllerDefinition ControllerDefinition { get; }

		int timeFrameCounter;
		public int Frame
		{
			get => timeFrameCounter;
			set => timeFrameCounter = value;
		}
		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }
		public string SystemId => "Libretro";
		public bool DeterministicEmulation => false;

		#region ISaveRam
		//TODO - terrible things will happen if this changes at runtime

		byte[] saverambuff = new byte[0];

		public byte[] CloneSaveRam()
		{
			var mem = api.QUERY_GetMemory(LibretroApi.RETRO_MEMORY.SAVE_RAM);
			var buf = new byte[mem.Item2];

			Marshal.Copy(mem.Item1, buf, 0, (int)mem.Item2);
			return buf;
		}

		public void StoreSaveRam(byte[] data)
		{
			var mem = api.QUERY_GetMemory(LibretroApi.RETRO_MEMORY.SAVE_RAM);

			//bail if the size is 0
			if (mem.Item2 == 0)
				return;

			Marshal.Copy(data, 0, mem.Item1, (int)mem.Item2);
		}

		public bool SaveRamModified
		{
			[FeatureNotImplemented]
			get
			{
				//if we don't have saveram, it isnt modified. otherwise, assume it is
				var mem = api.QUERY_GetMemory(LibretroApi.RETRO_MEMORY.SAVE_RAM);
				
				//bail if the size is 0
				if (mem.Item2 == 0)
					return false;

				return true;
			}

			[FeatureNotImplemented]
			set => throw new NotImplementedException();
		}

		#endregion

		public void ResetCounters()
		{
			timeFrameCounter = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		#region savestates

		private byte[] savebuff, savebuff2;

		public void SaveStateBinary(BinaryWriter writer)
		{
			api.CMD_UpdateSerializeSize();
			if (savebuff == null || savebuff.Length != (int)api.comm->env.retro_serialize_size)
			{
				savebuff = new byte[api.comm->env.retro_serialize_size];
				savebuff2 = new byte[savebuff.Length + 13];
			}

			api.CMD_Serialize(savebuff);
			writer.Write(savebuff.Length);
			writer.Write(savebuff);
			// other variables
			writer.Write(Frame);
			writer.Write(LagCount);
			writer.Write(IsLagFrame);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			int newlen = reader.ReadInt32();
			if (newlen > savebuff.Length)
				throw new Exception("Unexpected buffer size");
			reader.Read(savebuff, 0, newlen);
			api.CMD_Unserialize(savebuff);
			// other variables
			Frame = reader.ReadInt32();
			LagCount = reader.ReadInt32();
			IsLagFrame = reader.ReadBoolean();
		}

		public byte[] SaveStateBinary()
		{
			api.CMD_UpdateSerializeSize();
			if (savebuff == null || savebuff.Length != (int)api.comm->env.retro_serialize_size)
			{
				savebuff = new byte[api.comm->env.retro_serialize_size];
				savebuff2 = new byte[savebuff.Length + 13];
			}

			var ms = new MemoryStream(savebuff2, true);
			var bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			ms.Close();
			return savebuff2;
		}

		#endregion


	} //class

} //namespace
 