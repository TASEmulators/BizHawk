using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace BizHawk.Emulation.Consoles.Nintendo.N64
{
	public class N64 : IEmulator, IVideoProvider
	{
		static N64 AttachedCore = null;
		public string SystemId { get { return "N64"; } }

		public CoreComm CoreComm { get; private set; }
		public byte[] rom;
		public GameInfo game;

		public IVideoProvider VideoProvider { get { return this; } }
		public int[] frameBuffer = new int[800 * 600];
		public int[] GetVideoBuffer() 
		{
			/*
			for (int row = 0; row < 600; row++)
			{
				for (int col = 0; col < 800; col++)
				{
					int i = row * 800 + col;
					frameBuffer[(599 - row) * 800 + col] = (m64p_FrameBuffer[(i * 3)] << 16) + (m64p_FrameBuffer[(i * 3) + 1] << 8) + (m64p_FrameBuffer[(i * 3) + 2]);
				}
			}
			*/
			return frameBuffer;
		}
		public int VirtualWidth { get { return 800; } }
		public int BufferWidth { get { return 800; } }
		public int BufferHeight { get { return 600; } }
		public int BackgroundColor { get { return 0; } }

		Sound.Utilities.SpeexResampler resampler;

		uint m64pSamplingRate;

		short[] m64pAudioBuffer = new short[2];
		public ISoundProvider SoundProvider { get { return null; } }
		public ISyncSoundProvider SyncSoundProvider { get { return resampler; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }

		public ControllerDefinition ControllerDefinition { get { return N64ControllerDefinition; } }
		public IController Controller { get; set; }
		public static readonly ControllerDefinition N64ControllerDefinition = new ControllerDefinition
		{
			Name = "Nintento 64 Controller",
			BoolButtons =
			{
				"P1 DPad R", "P1 DPad L", "P1 DPad D", "P1 DPad U", "P1 Start", "P1 Z", "P1 B", "P1 A", "P1 C Right", "P1 C Left", "P1 C Down", "P1 C Up", "P1 R", "P1 L",
				"P2 DPad R", "P2 DPad L", "P2 DPad D", "P2 DPad U", "P2 Start", "P2 Z", "P2 B", "P2 A", "P2 C Right", "P2 C Left", "P2 C Down", "P2 C Up", "P2 R", "P2 L",
				"P3 DPad R", "P3 DPad L", "P3 DPad D", "P3 DPad U", "P3 Start", "P3 Z", "P3 B", "P3 A", "P3 C Right", "P3 C Left", "P3 C Down", "P3 C Up", "P3 R", "P3 L",
				"P4 DPad R", "P4 DPad L", "P4 DPad D", "P4 DPad U", "P4 Start", "P4 Z", "P4 B", "P4 A", "P4 C Right", "P4 C Left", "P4 C Down", "P4 C Up", "P4 R", "P4 L",
				"Reset", "Power"
			}
		};

		public int Frame { get; set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get { return true; } }
		public void ResetFrameCounter() { }
		public void FrameAdvance(bool render, bool rendersound) 
		{
			if (Controller["Reset"])
			{
				m64pCoreDoCommandPtr(m64p_command.M64CMD_RESET, 0, IntPtr.Zero);
			}
			if (Controller["Power"])
			{
				m64pCoreDoCommandPtr(m64p_command.M64CMD_RESET, 1, IntPtr.Zero);
			}			
			/*
			sbyte x = 0;
			sbyte y = 0;
			if (Controller["P1 DPad R"]) x = 80;
			if (Controller["P1 DPad L"]) x = -80;
			if (Controller["P1 DPad D"]) y = -80;
			if (Controller["P1 DPad U"]) y = 80;
			InpSetKeys(0, ReadController(1), x, y);
			*/
			InpSetKeys(0, ReadController(1), 0, 0);
			m64pCoreDoCommandPtr(m64p_command.M64CMD_ADVANCE_FRAME, 0, IntPtr.Zero);
			m64pFrameComplete.WaitOne();
			Frame++; 
		}

		public int ReadController(int num)
		{
			int buttons = 0;

			if (Controller["P1 DPad R"]) buttons |= (1 << 0);
			if (Controller["P1 DPad L"]) buttons |= (1 << 1);
			if (Controller["P1 DPad D"]) buttons |= (1 << 2);
			if (Controller["P1 DPad U"]) buttons |= (1 << 3);
			if (Controller["P1 Start"]) buttons |= (1 << 4);
			if (Controller["P1 Z"]) buttons |= (1 << 5);
			if (Controller["P1 B"]) buttons |= (1 << 6);
			if (Controller["P1 A"]) buttons |= (1 << 7);
			if (Controller["P1 C Right"]) buttons |= (1 << 8);
			if (Controller["P1 C Left"]) buttons |= (1 << 9);
			if (Controller["P1 C Down"]) buttons |= (1 << 10);
			if (Controller["P1 C Up"]) buttons |= (1 << 11);
			if (Controller["P1 R"]) buttons |= (1 << 12);
			if (Controller["P1 L"]) buttons |= (1 << 13);

			return buttons;
		}

		public bool DeterministicEmulation { get; set; }

		public byte[] ReadSaveRam() { return null; }
		public void StoreSaveRam(byte[] data) { }
		public void ClearSaveRam() { }
		public bool SaveRamModified { get; set; }

		void SyncState(Serializer ser)
		{
			ser.BeginSection("N64");
			ser.EndSection();
		}

		public void SaveStateText(TextWriter writer) { SyncState(Serializer.CreateTextWriter(writer)); }
		public void LoadStateText(TextReader reader) { SyncState(Serializer.CreateTextReader(reader)); }
		public void SaveStateBinary(BinaryWriter bw) { SyncState(Serializer.CreateBinaryWriter(bw)); }
		public void LoadStateBinary(BinaryReader br) { SyncState(Serializer.CreateBinaryReader(br)); }
		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		public IList<MemoryDomain> MemoryDomains { get { return null; } }
		public MemoryDomain MainMemory { get { return null; } }

		bool disposed = false;
		public void Dispose()
		{
			if (!disposed)
			{
				// Stop the core, and wait for it to end
				m64pCoreDoCommandPtr(m64p_command.M64CMD_STOP, 0, IntPtr.Zero);
				//m64pEmulator.Join();
				while (emulator_running) { }

				resampler.Dispose();
				resampler = null;

				m64pCoreDetachPlugin(m64p_plugin_type.M64PLUGIN_GFX);
				m64pCoreDetachPlugin(m64p_plugin_type.M64PLUGIN_AUDIO);
				m64pCoreDetachPlugin(m64p_plugin_type.M64PLUGIN_INPUT);
				m64pCoreDetachPlugin(m64p_plugin_type.M64PLUGIN_RSP);

				GfxPluginShutdown();
				FreeLibrary(GfxDll);

				AudPluginShutdown();
				FreeLibrary(AudDll);

				InpPluginShutdown();
				FreeLibrary(InpDll);

				RspPluginShutdown();
				FreeLibrary(RspDll);

				m64pCoreDoCommandPtr(m64p_command.M64CMD_ROM_CLOSE, 0, IntPtr.Zero);
				m64pCoreShutdown();
				FreeLibrary(CoreDll);

				disposed = true;
			}
		}

		[DllImport("kernel32.dll")]
		public static extern IntPtr LoadLibrary(string dllToLoad);

		[DllImport("kernel32.dll")]
		public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

		[DllImport("kernel32.dll")]
		public static extern bool FreeLibrary(IntPtr hModule);

		enum m64p_error
		{
			M64ERR_SUCCESS = 0,
			M64ERR_NOT_INIT,        /* Function is disallowed before InitMupen64Plus() is called */
			M64ERR_ALREADY_INIT,    /* InitMupen64Plus() was called twice */
			M64ERR_INCOMPATIBLE,    /* API versions between components are incompatible */
			M64ERR_INPUT_ASSERT,    /* Invalid parameters for function call, such as ParamValue=NULL for GetCoreParameter() */
			M64ERR_INPUT_INVALID,   /* Invalid input data, such as ParamValue="maybe" for SetCoreParameter() to set a BOOL-type value */
			M64ERR_INPUT_NOT_FOUND, /* The input parameter(s) specified a particular item which was not found */
			M64ERR_NO_MEMORY,       /* Memory allocation failed */
			M64ERR_FILES,           /* Error opening, creating, reading, or writing to a file */
			M64ERR_INTERNAL,        /* Internal error (bug) */
			M64ERR_INVALID_STATE,   /* Current program state does not allow operation */
			M64ERR_PLUGIN_FAIL,     /* A plugin function returned a fatal error */
			M64ERR_SYSTEM_FAIL,     /* A system function call, such as an SDL or file operation, failed */
			M64ERR_UNSUPPORTED,     /* Function call is not supported (ie, core not built with debugger) */
			M64ERR_WRONG_TYPE       /* A given input type parameter cannot be used for desired operation */
		};

		enum m64p_plugin_type
		{
			M64PLUGIN_NULL = 0,
			M64PLUGIN_RSP = 1,
			M64PLUGIN_GFX,
			M64PLUGIN_AUDIO,
			M64PLUGIN_INPUT,
			M64PLUGIN_CORE
		};

		enum m64p_command
		{
			M64CMD_NOP = 0,
			M64CMD_ROM_OPEN,
			M64CMD_ROM_CLOSE,
			M64CMD_ROM_GET_HEADER,
			M64CMD_ROM_GET_SETTINGS,
			M64CMD_EXECUTE,
			M64CMD_STOP,
			M64CMD_PAUSE,
			M64CMD_RESUME,
			M64CMD_CORE_STATE_QUERY,
			M64CMD_STATE_LOAD,
			M64CMD_STATE_SAVE,
			M64CMD_STATE_SET_SLOT,
			M64CMD_SEND_SDL_KEYDOWN,
			M64CMD_SEND_SDL_KEYUP,
			M64CMD_SET_FRAME_CALLBACK,
			M64CMD_TAKE_NEXT_SCREENSHOT,
			M64CMD_CORE_STATE_SET,
			M64CMD_READ_SCREEN,
			M64CMD_RESET,
			M64CMD_ADVANCE_FRAME,
			M64CMD_SET_VI_CALLBACK
		};

		enum m64p_emu_state
		{
		  M64EMU_STOPPED = 1,
		  M64EMU_RUNNING,
		  M64EMU_PAUSED
		};

		enum m64p_type
		{
		  M64TYPE_INT = 1,
		  M64TYPE_FLOAT,
		  M64TYPE_BOOL,
		  M64TYPE_STRING
		};

		//[DllImport(@"..\..\libmupen64plus\mupen64plus-ui-console\projects\msvc11\Release\mupen64plus.dll", CallingConvention = CallingConvention.Cdecl)]
		//static extern m64p_error CoreStartup(int APIVersion, string ConfigPath, string DataPath, string context, DebugCallback DebugCallback, string context2, IntPtr bar);

		// Core Specifc functions
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate m64p_error CoreStartup(int APIVersion, string ConfigPath, string DataPath, string Context, DebugCallback DebugCallback, string context2, IntPtr dummy);
		CoreStartup m64pCoreStartup;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate m64p_error CoreShutdown();
		CoreShutdown m64pCoreShutdown;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate m64p_error CoreAttachPlugin(m64p_plugin_type PluginType, IntPtr PluginLibHandle);
		CoreAttachPlugin m64pCoreAttachPlugin;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate m64p_error CoreDetachPlugin(m64p_plugin_type PluginType);
		CoreDetachPlugin m64pCoreDetachPlugin;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate m64p_error ConfigOpenSection(string SectionName, ref IntPtr ConfigSectionHandle);
		ConfigOpenSection m64pConfigOpenSection;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate m64p_error ConfigSetParameter(IntPtr ConfigSectionHandle, string ParamName, m64p_type ParamType, ref int ParamValue);
		ConfigSetParameter m64pConfigSetParameter;

		// The last parameter is a void pointer, so make a few delegates for the versions we want to use
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate m64p_error CoreDoCommandByteArray(m64p_command Command, int ParamInt, byte[] ParamPtr);
		CoreDoCommandByteArray m64pCoreDoCommandByteArray;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate m64p_error CoreDoCommandPtr(m64p_command Command, int ParamInt, IntPtr ParamPtr);
		CoreDoCommandPtr m64pCoreDoCommandPtr;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate m64p_error CoreDoCommandRefInt(m64p_command Command, int ParamInt, ref int ParamPtr);
		CoreDoCommandRefInt m64pCoreDoCommandRefInt;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate m64p_error CoreDoCommandFrameCallback(m64p_command Command, int ParamInt, FrameCallback ParamPtr);
		CoreDoCommandFrameCallback m64pCoreDoCommandFrameCallback;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate m64p_error CoreDoCommandVICallback(m64p_command Command, int ParamInt, VICallback ParamPtr);
		CoreDoCommandVICallback m64pCoreDoCommandVICallback;

		// Graphics plugin specific
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void ReadScreen2(int[] framebuffer, ref int width, ref int height, int buffer);
		ReadScreen2 GFXReadScreen2;

		// Audio plugin specific
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int GetBufferSize();
		GetBufferSize AudGetBufferSize;

		// Input plugin specific
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int SetKeys(int num, int keys, sbyte X, sbyte Y);
		SetKeys InpSetKeys;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void ReadAudioBuffer(short[] dest);
		ReadAudioBuffer AudReadAudioBuffer;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int GetAudioRate();
		GetAudioRate AudGetAudioRate;

		// This has the same calling pattern for all the plugins
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate m64p_error PluginStartup(IntPtr CoreHandle, string Context, DebugCallback DebugCallback);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate m64p_error PluginShutdown();

		PluginStartup GfxPluginStartup;
		PluginStartup RspPluginStartup;
		PluginStartup AudPluginStartup;
		PluginStartup InpPluginStartup;

		PluginShutdown GfxPluginShutdown;
		PluginShutdown RspPluginShutdown;
		PluginShutdown AudPluginShutdown;
		PluginShutdown InpPluginShutdown;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void DebugCallback(IntPtr Context, int level, string Message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void FrameCallback();
		FrameCallback m64pFrameCallback;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void StartupCallback();

		int[] m64p_FrameBuffer = new int[800 * 600];
		public void Getm64pFrameBuffer()
		{
			int width = 0;
			int height = 0;
			GFXReadScreen2(m64p_FrameBuffer, ref width, ref height, 0);
			
			// vflip
			int fromindex = 800 * 599 * 4;
			int toindex = 0;
			for (int j = 0; j < 600; j++)
			{
				Buffer.BlockCopy(m64p_FrameBuffer, fromindex, frameBuffer, toindex, 800 * 4);
				fromindex -= 800 * 4;
				toindex += 800 * 4;
			}
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void VICallback();
		VICallback m64pVICallback;
		public void FrameComplete()
		{
			uint s = (uint)AudGetAudioRate();
			if (s != m64pSamplingRate)
			{
				m64pSamplingRate = s;
				resampler.ChangeRate(s, 44100, s, 44100);
				//Console.WriteLine("N64 ARate Change {0}", s);
			}

			int m64pAudioBufferSize = AudGetBufferSize();
			if (m64pAudioBuffer.Length < m64pAudioBufferSize)
				m64pAudioBuffer = new short[m64pAudioBufferSize];

			if (m64pAudioBufferSize > 0)
			{
				AudReadAudioBuffer(m64pAudioBuffer);
				resampler.EnqueueSamples(m64pAudioBuffer, m64pAudioBufferSize / 2);
			}
			m64pFrameComplete.Set();
		}

		IntPtr CoreDll;
		IntPtr GfxDll;
		IntPtr RspDll;
		IntPtr AudDll;
		IntPtr InpDll;

		Thread m64pEmulator;

		AutoResetEvent m64pFrameComplete = new AutoResetEvent(false);

		ManualResetEvent m64pStartupComplete = new ManualResetEvent(false);

		public N64(CoreComm comm, GameInfo game, byte[] rom)
		{
			if (AttachedCore != null)
			{
				AttachedCore.Dispose();
				AttachedCore = null;
			}
			CoreComm = comm;
			this.rom = rom;
			this.game = game;

			// Load the core DLL and retrieve some function pointers
			CoreDll = LoadLibrary("mupen64plus.dll");
			if (CoreDll == IntPtr.Zero)
				throw new InvalidOperationException(string.Format("Failed to load mupen64plus.dll"));
			GfxDll = LoadLibrary("mupen64plus-video-rice.dll");
			if (GfxDll == IntPtr.Zero)
				throw new InvalidOperationException(string.Format("Failed to load mupen64plus-video-rice.dll"));
			RspDll = LoadLibrary("mupen64plus-rsp-hle.dll");
			if (RspDll == IntPtr.Zero)
				throw new InvalidOperationException(string.Format("Failed to load mupen64plus-rsp-hle.dll"));
			AudDll = LoadLibrary("mupen64plus-audio-bkm.dll");
			if (AudDll == IntPtr.Zero)
				throw new InvalidOperationException(string.Format("Failed to load mupen64plus-audio-bkm.dll"));
			InpDll = LoadLibrary("mupen64plus-input-bkm.dll");
			if (InpDll == IntPtr.Zero)
				throw new InvalidOperationException(string.Format("Failed to load mupen64plus-input-bkm.dll"));
			
			m64pCoreStartup = (CoreStartup)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "CoreStartup"), typeof(CoreStartup));
			m64pCoreShutdown = (CoreShutdown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "CoreShutdown"), typeof(CoreShutdown));
			m64pCoreDoCommandByteArray = (CoreDoCommandByteArray)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "CoreDoCommand"), typeof(CoreDoCommandByteArray));
			m64pCoreDoCommandPtr = (CoreDoCommandPtr)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "CoreDoCommand"), typeof(CoreDoCommandPtr));
			m64pCoreDoCommandRefInt = (CoreDoCommandRefInt)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "CoreDoCommand"), typeof(CoreDoCommandRefInt));
			m64pCoreDoCommandFrameCallback = (CoreDoCommandFrameCallback)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "CoreDoCommand"), typeof(CoreDoCommandFrameCallback));
			m64pCoreDoCommandVICallback = (CoreDoCommandVICallback)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "CoreDoCommand"), typeof(CoreDoCommandVICallback));
			m64pCoreAttachPlugin = (CoreAttachPlugin)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "CoreAttachPlugin"), typeof(CoreAttachPlugin));
			m64pCoreDetachPlugin = (CoreDetachPlugin)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "CoreDetachPlugin"), typeof(CoreDetachPlugin));
			m64pConfigOpenSection = (ConfigOpenSection)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "ConfigOpenSection"), typeof(ConfigOpenSection));
			m64pConfigSetParameter = (ConfigSetParameter)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "ConfigSetParameter"), typeof(ConfigSetParameter));

			GfxPluginStartup = (PluginStartup)Marshal.GetDelegateForFunctionPointer(GetProcAddress(GfxDll, "PluginStartup"), typeof(PluginStartup));
			GfxPluginShutdown = (PluginShutdown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(GfxDll, "PluginShutdown"), typeof(PluginShutdown));
			GFXReadScreen2 = (ReadScreen2)Marshal.GetDelegateForFunctionPointer(GetProcAddress(GfxDll, "ReadScreen2"), typeof(ReadScreen2));

			AudPluginStartup = (PluginStartup)Marshal.GetDelegateForFunctionPointer(GetProcAddress(AudDll, "PluginStartup"), typeof(PluginStartup));
			AudPluginShutdown = (PluginShutdown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(AudDll, "PluginShutdown"), typeof(PluginShutdown));
			AudGetBufferSize = (GetBufferSize)Marshal.GetDelegateForFunctionPointer(GetProcAddress(AudDll, "GetBufferSize"), typeof(GetBufferSize));
			AudReadAudioBuffer = (ReadAudioBuffer)Marshal.GetDelegateForFunctionPointer(GetProcAddress(AudDll, "ReadAudioBuffer"), typeof(ReadAudioBuffer));
			AudGetAudioRate = (GetAudioRate)Marshal.GetDelegateForFunctionPointer(GetProcAddress(AudDll, "GetAudioRate"), typeof(GetAudioRate));

			InpPluginStartup = (PluginStartup)Marshal.GetDelegateForFunctionPointer(GetProcAddress(InpDll, "PluginStartup"), typeof(PluginStartup));
			InpPluginShutdown = (PluginShutdown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(InpDll, "PluginShutdown"), typeof(PluginShutdown));
			InpSetKeys = (SetKeys)Marshal.GetDelegateForFunctionPointer(GetProcAddress(InpDll, "SetKeys"), typeof(SetKeys));

			RspPluginStartup = (PluginStartup)Marshal.GetDelegateForFunctionPointer(GetProcAddress(RspDll, "PluginStartup"), typeof(PluginStartup));
			RspPluginShutdown = (PluginShutdown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(RspDll, "PluginShutdown"), typeof(PluginShutdown));

			// Set up the core
			m64p_error result = m64pCoreStartup(0x20001, "", "", "Core", (IntPtr foo, int level, string Message) => { }, "", IntPtr.Zero);
			result = m64pCoreDoCommandByteArray(m64p_command.M64CMD_ROM_OPEN, rom.Length, rom);

			// Set up and connect the graphics plugin
			result = GfxPluginStartup(CoreDll, "Video", (IntPtr foo, int level, string Message) => { });
			result = m64pCoreAttachPlugin(m64p_plugin_type.M64PLUGIN_GFX, GfxDll);

			// Configure the video plugin
			IntPtr video_section = IntPtr.Zero;
			m64pConfigOpenSection("Video-General", ref video_section);
			int width = 800;
			result = m64pConfigSetParameter(video_section, "ScreenWidth", m64p_type.M64TYPE_INT, ref width);
			int height = 600;
			result = m64pConfigSetParameter(video_section, "ScreenHeight", m64p_type.M64TYPE_INT, ref height);

			// Set up a null audio plugin
			result = AudPluginStartup(CoreDll, "Audio", (IntPtr foo, int level, string Message) => { });
			result = m64pCoreAttachPlugin(m64p_plugin_type.M64PLUGIN_AUDIO, AudDll);

			// Set up a null input plugin
			result = AudPluginStartup(CoreDll, "Input", (IntPtr foo, int level, string Message) => { });
			result = m64pCoreAttachPlugin(m64p_plugin_type.M64PLUGIN_INPUT, InpDll);

			// Set up and connect the graphics plugin
			result = RspPluginStartup(CoreDll, "RSP", (IntPtr foo, int level, string Message) => { });
			result = m64pCoreAttachPlugin(m64p_plugin_type.M64PLUGIN_RSP, RspDll);

			// Set up the frame callback function
			m64pFrameCallback = new FrameCallback(Getm64pFrameBuffer);
			result = m64pCoreDoCommandFrameCallback(m64p_command.M64CMD_SET_FRAME_CALLBACK, 0, m64pFrameCallback);

			// Set up the vi callback function
			m64pVICallback = new VICallback(FrameComplete);
			result = m64pCoreDoCommandVICallback(m64p_command.M64CMD_SET_VI_CALLBACK, 0, m64pVICallback);

			m64pEmulator = new Thread(ExecuteEmulator);
			m64pEmulator.Start();

			//int state = -1;
			/*
			do
			{
				m64pCoreDoCommandRefInt(m64p_command.M64CMD_CORE_STATE_QUERY, 1, ref state);
			} while (state != (int)m64p_emu_state.M64EMU_PAUSED);
			*/
			//m64pFrameComplete.WaitOne();
			m64pStartupComplete.WaitOne();

			m64pSamplingRate = (uint)AudGetAudioRate();
			resampler = new Sound.Utilities.SpeexResampler(6, m64pSamplingRate, 44100, m64pSamplingRate, 44100, null, null);
			//Console.WriteLine("N64 Initial ARate {0}", m64pSamplingRate);
			
			//resampler = new Sound.Utilities.SpeexResampler(6, 32000, 44100, 32000, 44100, null, null);
			AttachedCore = this;
		}

		volatile bool emulator_running = false;
		public void ExecuteEmulator()
		{
			emulator_running = true;
			m64pCoreDoCommandPtr(m64p_command.M64CMD_EXECUTE, 0,
				Marshal.GetFunctionPointerForDelegate(new StartupCallback(() => m64pStartupComplete.Set())));
			emulator_running = false;
		}


	}
}
