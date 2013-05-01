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
		public string SystemId { get { return "N64"; } }

		public CoreComm CoreComm { get; private set; }
		public byte[] rom;
		public GameInfo game;

		public IVideoProvider VideoProvider { get { return this; } }
		public int[] frameBuffer = new int[640 * 480];
		public int[] GetVideoBuffer() 
		{
			for (int row = 0; row < 480; row++)
			{
				for (int col = 0; col < 640; col++)
				{
					int i = row * 640 + col;
					frameBuffer[(479 - row) * 640 + col] = (m64p_FrameBuffer[(i * 3)] << 16) + (m64p_FrameBuffer[(i * 3) + 1] << 8) + (m64p_FrameBuffer[(i * 3) + 2]);
				}
			}
			return frameBuffer;
		}
		public int VirtualWidth { get { return 640; } }
		public int BufferWidth { get { return 640; } }
		public int BufferHeight { get { return 480; } }
		public int BackgroundColor { get { return 0; } }

		Sound.Utilities.SpeexResampler resampler;

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
				"DPad R", "DPad L", "DPad D", "DPad U", "Start", "Z", "B", "A", "C Right", "C Left", "C Down", "C Up", "R", "L"
			}
		};

		public int Frame { get; set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get { return true; } }
		public void ResetFrameCounter() { }
		public void FrameAdvance(bool render, bool rendersound) 
		{
			m64pFrameComplete = false;
			m64pCoreDoCommandPtr(m64p_command.M64CMD_ADVANCE_FRAME, 0, IntPtr.Zero);
			while (m64pFrameComplete == false) { }
			Frame++; 
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

		public void Dispose()
		{
			resampler.Dispose();
			resampler = null;
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

		//[DllImport(@"..\..\libmupen64plus\mupen64plus-ui-console\projects\msvc11\Release\mupen64plus.dll", CallingConvention = CallingConvention.Cdecl)]
		//static extern m64p_error CoreStartup(int APIVersion, string ConfigPath, string DataPath, string context, DebugCallback DebugCallback, string context2, IntPtr bar);

		// Core Specifc functions
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate m64p_error CoreStartup(int APIVersion, string ConfigPath, string DataPath, string Context, DebugCallback DebugCallback, string context2, IntPtr dummy);
		CoreStartup m64pCoreStartup;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate m64p_error CoreAttachPlugin(m64p_plugin_type PluginType, IntPtr PluginLibHandle);
		CoreAttachPlugin m64pCoreAttachPlugin;

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
		private delegate void ReadScreen2(byte[] framebuffer, ref int width, ref int height, int buffer);
		ReadScreen2 GFXReadScreen2;

		// Audio plugin specific
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int GetBufferSize();
		GetBufferSize AudGetBufferSize;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void ReadAudioBuffer(short[] dest);
		ReadAudioBuffer AudReadAudioBuffer;

		// This has the same calling pattern for all the plugins
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate m64p_error PluginStartup(IntPtr CoreHandle, string Context, DebugCallback DebugCallback);

		PluginStartup GfxPluginStartup;
		PluginStartup RspPluginStartup;
		PluginStartup AudPluginStartup;

		public delegate void DebugCallback(IntPtr Context, int level, string Message);

		public delegate void FrameCallback();
		FrameCallback m64pFrameCallback;
		
		byte[] m64p_FrameBuffer = new byte[640 * 480 * 3];
		public void Getm64pFrameBuffer()
		{
			int width = 0;
			int height = 0;
			GFXReadScreen2(m64p_FrameBuffer, ref width, ref height, 0);
			//m64pFrameComplete = true;
		}

		public delegate void VICallback();
		VICallback m64pVICallback;
		public void FrameComplete()
		{
			int m64pAudioBufferSize = AudGetBufferSize();

			if (m64pAudioBuffer.Length < m64pAudioBufferSize)
				m64pAudioBuffer = new short[m64pAudioBufferSize];
			if (m64pAudioBufferSize > 0)
			{
				AudReadAudioBuffer(m64pAudioBuffer);
				resampler.EnqueueSamples(m64pAudioBuffer, m64pAudioBufferSize / 2);
			}
			m64pFrameComplete = true;
		}

		IntPtr CoreDll;
		IntPtr GfxDll;
		IntPtr RspDll;
		IntPtr AudDll;

		Thread m64pEmulator;

		volatile bool m64pFrameComplete = false;

		public N64(CoreComm comm, GameInfo game, byte[] rom)
		{
			CoreComm = comm;
			this.rom = rom;
			this.game = game;

			// Load the core DLL and retrieve some function pointers
			CoreDll = LoadLibrary("mupen64plus.dll");
			GfxDll = LoadLibrary("mupen64plus-video-rice.dll");
			RspDll = LoadLibrary("mupen64plus-rsp-hle.dll");
			AudDll = LoadLibrary("mupen64plus-audio-bkm.dll");

			m64pCoreStartup = (CoreStartup)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "CoreStartup"), typeof(CoreStartup));
			m64pCoreDoCommandByteArray = (CoreDoCommandByteArray)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "CoreDoCommand"), typeof(CoreDoCommandByteArray));
			m64pCoreDoCommandPtr = (CoreDoCommandPtr)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "CoreDoCommand"), typeof(CoreDoCommandPtr));
			m64pCoreDoCommandRefInt = (CoreDoCommandRefInt)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "CoreDoCommand"), typeof(CoreDoCommandRefInt));
			m64pCoreDoCommandFrameCallback = (CoreDoCommandFrameCallback)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "CoreDoCommand"), typeof(CoreDoCommandFrameCallback));
			m64pCoreDoCommandVICallback = (CoreDoCommandVICallback)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "CoreDoCommand"), typeof(CoreDoCommandVICallback));
			m64pCoreAttachPlugin = (CoreAttachPlugin)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "CoreAttachPlugin"), typeof(CoreAttachPlugin));

			GfxPluginStartup = (PluginStartup)Marshal.GetDelegateForFunctionPointer(GetProcAddress(GfxDll, "PluginStartup"), typeof(PluginStartup));
			GFXReadScreen2 = (ReadScreen2)Marshal.GetDelegateForFunctionPointer(GetProcAddress(GfxDll, "ReadScreen2"), typeof(ReadScreen2));

			AudPluginStartup = (PluginStartup)Marshal.GetDelegateForFunctionPointer(GetProcAddress(AudDll, "PluginStartup"), typeof(PluginStartup));
			AudGetBufferSize = (GetBufferSize)Marshal.GetDelegateForFunctionPointer(GetProcAddress(AudDll, "GetBufferSize"), typeof(GetBufferSize));
			AudReadAudioBuffer = (ReadAudioBuffer)Marshal.GetDelegateForFunctionPointer(GetProcAddress(AudDll, "ReadAudioBuffer"), typeof(ReadAudioBuffer));

			RspPluginStartup = (PluginStartup)Marshal.GetDelegateForFunctionPointer(GetProcAddress(RspDll, "PluginStartup"), typeof(PluginStartup));

			// Set up the core
			m64p_error result = m64pCoreStartup(0x20001, "", "", "Core", (IntPtr foo, int level, string Message) => { Console.WriteLine(Message); }, "", IntPtr.Zero);
			result = m64pCoreDoCommandByteArray(m64p_command.M64CMD_ROM_OPEN, rom.Length, rom);

			// Set up and connect the graphics plugin
			result = GfxPluginStartup(CoreDll, "Video", (IntPtr foo, int level, string Message) => { Console.WriteLine(Message); });
			result = m64pCoreAttachPlugin(m64p_plugin_type.M64PLUGIN_GFX, GfxDll);

			// Set up a null audio plugin
			result = AudPluginStartup(CoreDll, "Audio", (IntPtr foo, int level, string Message) => { Console.WriteLine(Message); });
			result = m64pCoreAttachPlugin(m64p_plugin_type.M64PLUGIN_AUDIO, AudDll);

			// Set up a null input plugin
			result = m64pCoreAttachPlugin(m64p_plugin_type.M64PLUGIN_INPUT, IntPtr.Zero);

			// Set up and connect the graphics plugin
			result = RspPluginStartup(CoreDll, "RSP", (IntPtr foo, int level, string Message) => { Console.WriteLine(Message); });
			result = m64pCoreAttachPlugin(m64p_plugin_type.M64PLUGIN_RSP, RspDll);

			// Set up the frame callback function
			m64pFrameCallback = new FrameCallback(Getm64pFrameBuffer);
			result = m64pCoreDoCommandFrameCallback(m64p_command.M64CMD_SET_FRAME_CALLBACK, 0, m64pFrameCallback);

			// Set up the vi callback function
			m64pVICallback = new VICallback(FrameComplete);
			result = m64pCoreDoCommandVICallback(m64p_command.M64CMD_SET_VI_CALLBACK, 0, m64pVICallback);

			m64pEmulator = new Thread(ExecuteEmulator);
			m64pEmulator.Start();

			int state = -1;
			do
			{
				m64pCoreDoCommandRefInt(m64p_command.M64CMD_CORE_STATE_QUERY, 1, ref state);
			} while (state != (int)m64p_emu_state.M64EMU_PAUSED);

			resampler = new Sound.Utilities.SpeexResampler(6, 32000, 44100, 32000, 44100, null, null);
		}

		public void ExecuteEmulator()
		{
			m64pCoreDoCommandPtr(m64p_command.M64CMD_EXECUTE, 0, IntPtr.Zero);
		}


	}
}
