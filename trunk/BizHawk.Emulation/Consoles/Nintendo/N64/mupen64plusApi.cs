using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace BizHawk.Emulation.Consoles.Nintendo.N64
{
	public class mupen64plusApi : IDisposable
	{
		private readonly N64 bizhawkCore;
		static mupen64plusApi AttachedCore = null;

		bool disposed = false;

		uint m64pSamplingRate;
		short[] m64pAudioBuffer = new short[2];

		Thread m64pEmulator;

		AutoResetEvent m64pFrameComplete = new AutoResetEvent(false);
		ManualResetEvent m64pStartupComplete = new ManualResetEvent(false);

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

		public enum N64_MEMORY : uint
		{
			RDRAM = 1,
			PI_REG,
			SI_REG,
			VI_REG,
			RI_REG,
			AI_REG,

			EEPROM = 100,
			MEMPAK1,
			MEMPAK2,
			MEMPAK3,
			MEMPAK4
		}

		// Core Specifc functions

		/// <summary>
		/// Initializes the the core DLL
		/// </summary>
		/// <param name="APIVersion">Specifies what API version our app is using. Just set this to 0x20001</param>
		/// <param name="ConfigPath">Directory to have the DLL look for config data. "" seems to disable this</param>
		/// <param name="DataPath">Directory to have the DLL look for user data. "" seems to disable this</param>
		/// <param name="Context">Use "Core"</param>
		/// <param name="DebugCallback">A function to use when the core wants to output debug messages</param>
		/// <param name="context2">Use ""</param>
		/// <param name="dummy">Use IntPtr.Zero</param>
		/// <returns></returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate m64p_error CoreStartup(int APIVersion, string ConfigPath, string DataPath, string Context, DebugCallback DebugCallback, string context2, IntPtr dummy);
		CoreStartup m64pCoreStartup;

		/// <summary>
		/// Cleans up the core
		/// </summary>
		/// <returns></returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate m64p_error CoreShutdown();
		CoreShutdown m64pCoreShutdown;

		/// <summary>
		/// Connects a plugin DLL to the core DLL
		/// </summary>
		/// <param name="PluginType">The type of plugin that is being connected</param>
		/// <param name="PluginLibHandle">The DLL handle for the plugin</param>
		/// <returns></returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate m64p_error CoreAttachPlugin(m64p_plugin_type PluginType, IntPtr PluginLibHandle);
		CoreAttachPlugin m64pCoreAttachPlugin;

		/// <summary>
		/// Disconnects a plugin DLL from the core DLL
		/// </summary>
		/// <param name="PluginType">The type of plugin to be disconnected</param>
		/// <returns></returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate m64p_error CoreDetachPlugin(m64p_plugin_type PluginType);
		CoreDetachPlugin m64pCoreDetachPlugin;

		/// <summary>
		/// Opens a section in the global config system
		/// </summary>
		/// <param name="SectionName">The name of the section to open</param>
		/// <param name="ConfigSectionHandle">A pointer to the pointer to use as the section handle</param>
		/// <returns></returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate m64p_error ConfigOpenSection(string SectionName, ref IntPtr ConfigSectionHandle);
		ConfigOpenSection m64pConfigOpenSection;

		/// <summary>
		/// Sets a parameter in the global config system
		/// </summary>
		/// <param name="ConfigSectionHandle">The handle of the section to access</param>
		/// <param name="ParamName">The name of the parameter to set</param>
		/// <param name="ParamType">The type of the parameter</param>
		/// <param name="ParamValue">A pointer to the value to use for the parameter (must be an int right now)</param>
		/// <returns></returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate m64p_error ConfigSetParameter(IntPtr ConfigSectionHandle, string ParamName, m64p_type ParamType, ref int ParamValue);
		ConfigSetParameter m64pConfigSetParameter;

		/// <summary>
		/// Saves the mupen64plus state to the provided buffer
		/// </summary>
		/// <param name="buffer">A byte array to use to save the state. Must be at least 16788288 + 1024 bytes</param>
		/// <returns></returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate int savestates_save_bkm(byte[] buffer);
		savestates_save_bkm m64pCoreSaveState;

		/// <summary>
		/// Loads the mupen64plus state from the provided buffer
		/// </summary>
		/// <param name="buffer">A byte array filled with the state to load. Must be at least 16788288 + 1024 bytes</param>
		/// <returns></returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate int savestates_load_bkm(byte[] buffer);
		savestates_load_bkm m64pCoreLoadState;

		/// <summary>
		/// Gets a pointer to a section of the mupen64plus core
		/// </summary>
		/// <param name="mem_ptr_type">The section to get a pointer for</param>
		/// <returns>A pointer to the section requested</returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate IntPtr DebugMemGetPointer(N64_MEMORY mem_ptr_type);
		DebugMemGetPointer m64pDebugMemGetPointer;

		/// <summary>
		/// Gets the size of the given memory area
		/// </summary>
		/// <param name="mem_ptr_type">The section to get the size of</param>
		/// <returns>The size of the section requested</returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate int MemGetSize(N64_MEMORY mem_ptr_type);
		MemGetSize m64pMemGetSize;

		/// <summary>
		/// Initializes the saveram (eeprom and 4 mempacks)
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate IntPtr init_saveram();
		init_saveram m64pinit_saveram;

		/// <summary>
		/// Pulls out the saveram for bizhawk to save
		/// </summary>
		/// <param name="dest">A byte array to save the saveram into</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate IntPtr save_saveram(byte[] dest);
		save_saveram m64psave_saveram;

		/// <summary>
		/// Restores the saveram from bizhawk
		/// </summary>
		/// <param name="src">A byte array containing the saveram to restore</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate IntPtr load_saveram(byte[] src);
		load_saveram m64pload_saveram;

		// The last parameter of CoreDoCommand is actually a void pointer, so instead we'll make a few delegates for the versions we want to use
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

		/// <summary>
		/// Fills a provided buffer with the mupen64plus framebuffer
		/// </summary>
		/// <param name="framebuffer">The buffer to fill</param>
		/// <param name="width">A pointer to a variable to fill with the width of the framebuffer</param>
		/// <param name="height">A pointer to a variable to fill with the height of the framebuffer</param>
		/// <param name="buffer">Which buffer to read: 0 = front, 1 = back</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void ReadScreen2(int[] framebuffer, ref int width, ref int height, int buffer);
		ReadScreen2 GFXReadScreen2;

		/// <summary>
		/// Gets the width and height of the mupen64plus framebuffer
		/// </summary>
		/// <param name="dummy">Use IntPtr.Zero</param>
		/// <param name="width">A pointer to a variable to fill with the width of the framebuffer</param>
		/// <param name="height">A pointer to a variable to fill with the height of the framebuffer</param>
		/// <param name="buffer">Which buffer to read: 0 = front, 1 = back</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void ReadScreen2Res(IntPtr dummy, ref int width, ref int height, int buffer);
		ReadScreen2Res GFXReadScreen2Res;


		// Audio plugin specific

		/// <summary>
		/// Gets the size of the mupen64plus audio buffer
		/// </summary>
		/// <returns>The size of the mupen64plus audio buffer</returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int GetBufferSize();
		GetBufferSize AudGetBufferSize;

		/// <summary>
		/// Gets the audio buffer from mupen64plus, and then clears it
		/// </summary>
		/// <param name="dest">The buffer to fill with samples</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void ReadAudioBuffer(short[] dest);
		ReadAudioBuffer AudReadAudioBuffer;

		/// <summary>
		/// Gets the current audio rate from mupen64plus
		/// </summary>
		/// <returns>The current audio rate</returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int GetAudioRate();
		GetAudioRate AudGetAudioRate;


		// Input plugin specific

		/// <summary>
		/// Sets the buttons for a controller
		/// </summary>
		/// <param name="num">The controller number to set buttons for (0-3)</param>
		/// <param name="keys">The button data</param>
		/// <param name="X">The value for the X axis</param>
		/// <param name="Y">The value for the Y axis</param>
		/// <returns></returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int SetKeys(int num, int keys, sbyte X, sbyte Y);
		SetKeys InpSetKeys;

		/// <summary>
		/// Sets a callback to use when the mupen core wants controller buttons
		/// </summary>
		/// <param name="inputCallback">The delegate to use</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void SetInputCallback(InputCallback inputCallback);
		SetInputCallback InpSetInputCallback;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void InputCallback();
		InputCallback InpInputCallback;


		// These are common for all four plugins

		/// <summary>
		/// Initializes the plugin
		/// </summary>
		/// <param name="CoreHandle">The DLL handle for the core DLL</param>
		/// <param name="Context">Use "Video", "Audio", "Input", or "RSP" depending on the plugin</param>
		/// <param name="DebugCallback">A function to use when the pluging wants to output debug messages</param>
		/// <returns></returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate m64p_error PluginStartup(IntPtr CoreHandle, string Context, DebugCallback DebugCallback);

		/// <summary>
		/// Cleans up the plugin
		/// </summary>
		/// <returns></returns>
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

		// Callback functions

		/// <summary>
		/// Handles a debug message from mupen64plus
		/// </summary>
		/// <param name="Context"></param>
		/// <param name="level"></param>
		/// <param name="Message">The message to display</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void DebugCallback(IntPtr Context, int level, string Message);

		/// <summary>
		/// This will be called every time a new frame is finished
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void FrameCallback();
		FrameCallback m64pFrameCallback;

		/// <summary>
		/// This will be called every time a VI occurs
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void VICallback();
		VICallback m64pVICallback;

		/// <summary>
		/// This will be called after the emulator is setup and is ready to be used
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void StartupCallback();

		// DLL handles
		IntPtr CoreDll;
		IntPtr GfxDll;
		IntPtr RspDll;
		IntPtr AudDll;
		IntPtr InpDll;

		public mupen64plusApi(N64 bizhawkCore, byte[] rom, VideoPluginSettings video_settings)
		{
			if (AttachedCore != null)
			{
				AttachedCore.Dispose();
				AttachedCore = null;
			}

			this.bizhawkCore = bizhawkCore;

			string VidDllName;
			if (video_settings.PluginName == "Rice")
			{
				VidDllName = "mupen64plus-video-rice.dll";
			}
			else if (video_settings.PluginName == "Glide64")
			{
				VidDllName = "mupen64plus-video-glide64.dll";
			}
			else
			{
				throw new InvalidOperationException(string.Format("Unknown plugin \"" + video_settings.PluginName));
			}

			// Load each of the DLLs
			CoreDll = LoadLibrary("mupen64plus.dll");
			if (CoreDll == IntPtr.Zero)
				throw new InvalidOperationException(string.Format("Failed to load mupen64plus.dll"));
			GfxDll = LoadLibrary(VidDllName);
			if (GfxDll == IntPtr.Zero)
				throw new InvalidOperationException(string.Format("Failed to load " + VidDllName));
			RspDll = LoadLibrary("mupen64plus-rsp-hle.dll");
			if (RspDll == IntPtr.Zero)
				throw new InvalidOperationException(string.Format("Failed to load mupen64plus-rsp-hle.dll"));
			AudDll = LoadLibrary("mupen64plus-audio-bkm.dll");
			if (AudDll == IntPtr.Zero)
				throw new InvalidOperationException(string.Format("Failed to load mupen64plus-audio-bkm.dll"));
			InpDll = LoadLibrary("mupen64plus-input-bkm.dll");
			if (InpDll == IntPtr.Zero)
				throw new InvalidOperationException(string.Format("Failed to load mupen64plus-input-bkm.dll"));

			connectFunctionPointers();

			// Start up the core
			m64p_error result = m64pCoreStartup(0x20001, "", "", "Core", (IntPtr foo, int level, string Message) => { }, "", IntPtr.Zero);

			// Pass the rom to the core
			result = m64pCoreDoCommandByteArray(m64p_command.M64CMD_ROM_OPEN, rom.Length, rom);

			// Resize the video to the size in bizhawk's settings
			SetVideoSize(video_settings.Width, video_settings.Height);

			// Open the general video settings section in the config system
			IntPtr video_section = IntPtr.Zero;
			m64pConfigOpenSection("Video-General", ref video_section);

			// Set the desired width and height for mupen64plus
			result = m64pConfigSetParameter(video_section, "ScreenWidth", m64p_type.M64TYPE_INT, ref video_settings.Width);
			result = m64pConfigSetParameter(video_section, "ScreenHeight", m64p_type.M64TYPE_INT, ref video_settings.Height);

			set_video_parameters(video_settings);

			// Set up and connect the graphics plugin
			result = GfxPluginStartup(CoreDll, "Video", (IntPtr foo, int level, string Message) => { });
			result = m64pCoreAttachPlugin(m64p_plugin_type.M64PLUGIN_GFX, GfxDll);

			// Set up our audio plugin
			result = AudPluginStartup(CoreDll, "Audio", (IntPtr foo, int level, string Message) => { });
			result = m64pCoreAttachPlugin(m64p_plugin_type.M64PLUGIN_AUDIO, AudDll);

			// Set up our input plugin
			result = AudPluginStartup(CoreDll, "Input", (IntPtr foo, int level, string Message) => { });
			result = m64pCoreAttachPlugin(m64p_plugin_type.M64PLUGIN_INPUT, InpDll);

			// Set up and connect the RSP plugin
			result = RspPluginStartup(CoreDll, "RSP", (IntPtr foo, int level, string Message) => { });
			result = m64pCoreAttachPlugin(m64p_plugin_type.M64PLUGIN_RSP, RspDll);

			// Set up the frame callback function
			m64pFrameCallback = new FrameCallback(Getm64pFrameBuffer);
			result = m64pCoreDoCommandFrameCallback(m64p_command.M64CMD_SET_FRAME_CALLBACK, 0, m64pFrameCallback);

			// Set up the VI callback function
			m64pVICallback = new VICallback(VI);
			result = m64pCoreDoCommandVICallback(m64p_command.M64CMD_SET_VI_CALLBACK, 0, m64pVICallback);

			InitSaveram();

			// Start the emulator in another thread
			m64pEmulator = new Thread(ExecuteEmulator);
			m64pEmulator.Start();

			// Wait for the core to boot up
			m64pStartupComplete.WaitOne();

			// Set up the resampler
			m64pSamplingRate = (uint)AudGetAudioRate();
			bizhawkCore.resampler = new Sound.Utilities.SpeexResampler(6, m64pSamplingRate, 44100, m64pSamplingRate, 44100, null, null);

			AttachedCore = this;
		}

		volatile bool emulator_running = false;
		public void ExecuteEmulator()
		{
			emulator_running = true;
			var cb = new StartupCallback(() => m64pStartupComplete.Set());
			m64pCoreDoCommandPtr(m64p_command.M64CMD_EXECUTE, 0,
				Marshal.GetFunctionPointerForDelegate(cb));
			emulator_running = false;
			cb.GetType();
		}

		/// <summary>
		/// Look up function pointers in the dlls
		/// </summary>
		void connectFunctionPointers()
		{
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
			m64pCoreSaveState = (savestates_save_bkm)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "savestates_save_bkm"), typeof(savestates_save_bkm));
			m64pCoreLoadState = (savestates_load_bkm)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "savestates_load_bkm"), typeof(savestates_load_bkm));
			m64pDebugMemGetPointer = (DebugMemGetPointer)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "DebugMemGetPointer"), typeof(DebugMemGetPointer));
			m64pMemGetSize = (MemGetSize)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "MemGetSize"), typeof(MemGetSize));
			m64pinit_saveram = (init_saveram)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "init_saveram"), typeof(init_saveram));
			m64psave_saveram = (save_saveram)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "save_saveram"), typeof(save_saveram));
			m64pload_saveram = (load_saveram)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "load_saveram"), typeof(load_saveram));

			GfxPluginStartup = (PluginStartup)Marshal.GetDelegateForFunctionPointer(GetProcAddress(GfxDll, "PluginStartup"), typeof(PluginStartup));
			GfxPluginShutdown = (PluginShutdown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(GfxDll, "PluginShutdown"), typeof(PluginShutdown));
			GFXReadScreen2 = (ReadScreen2)Marshal.GetDelegateForFunctionPointer(GetProcAddress(GfxDll, "ReadScreen2"), typeof(ReadScreen2));
			GFXReadScreen2Res = (ReadScreen2Res)Marshal.GetDelegateForFunctionPointer(GetProcAddress(GfxDll, "ReadScreen2"), typeof(ReadScreen2Res));

			AudPluginStartup = (PluginStartup)Marshal.GetDelegateForFunctionPointer(GetProcAddress(AudDll, "PluginStartup"), typeof(PluginStartup));
			AudPluginShutdown = (PluginShutdown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(AudDll, "PluginShutdown"), typeof(PluginShutdown));
			AudGetBufferSize = (GetBufferSize)Marshal.GetDelegateForFunctionPointer(GetProcAddress(AudDll, "GetBufferSize"), typeof(GetBufferSize));
			AudReadAudioBuffer = (ReadAudioBuffer)Marshal.GetDelegateForFunctionPointer(GetProcAddress(AudDll, "ReadAudioBuffer"), typeof(ReadAudioBuffer));
			AudGetAudioRate = (GetAudioRate)Marshal.GetDelegateForFunctionPointer(GetProcAddress(AudDll, "GetAudioRate"), typeof(GetAudioRate));

			InpPluginStartup = (PluginStartup)Marshal.GetDelegateForFunctionPointer(GetProcAddress(InpDll, "PluginStartup"), typeof(PluginStartup));
			InpPluginShutdown = (PluginShutdown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(InpDll, "PluginShutdown"), typeof(PluginShutdown));
			InpSetKeys = (SetKeys)Marshal.GetDelegateForFunctionPointer(GetProcAddress(InpDll, "SetKeys"), typeof(SetKeys));
			InpSetInputCallback = (SetInputCallback)Marshal.GetDelegateForFunctionPointer(GetProcAddress(InpDll, "SetInputCallback"), typeof(SetInputCallback));

			RspPluginStartup = (PluginStartup)Marshal.GetDelegateForFunctionPointer(GetProcAddress(RspDll, "PluginStartup"), typeof(PluginStartup));
			RspPluginShutdown = (PluginShutdown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(RspDll, "PluginShutdown"), typeof(PluginShutdown));
		}

		public void SetVideoSize(int vidX, int vidY)
		{
			bizhawkCore.VirtualWidth = vidX;
			bizhawkCore.BufferWidth = vidX;
			bizhawkCore.BufferHeight = vidY;

			bizhawkCore.frameBuffer = new int[vidX * vidY];
			m64p_FrameBuffer = new int[vidX * vidY];
		}

		public void set_video_parameters(VideoPluginSettings video_settings)
		{
			IntPtr video_plugin_section = IntPtr.Zero;
			if (video_settings.PluginName == "Rice")
			{
				m64pConfigOpenSection("Video-Rice", ref video_plugin_section);
			}
			else if (video_settings.PluginName == "Glide64")
			{
				m64pConfigOpenSection("Video-Glide64", ref video_plugin_section);
			}
			else
			{
				return;
			}

			foreach (string Parameter in video_settings.Parameters.Keys)
			{
				int value = 0;
				if (video_settings.Parameters[Parameter].GetType() == typeof(int))
				{
					value = (int)video_settings.Parameters[Parameter];
				}
				else if (video_settings.Parameters[Parameter].GetType() == typeof(bool))
				{
					value = (bool)video_settings.Parameters[Parameter] ? 1 : 0;
				}
				m64pConfigSetParameter(video_plugin_section, Parameter, m64p_type.M64TYPE_INT, ref value);
			}
		}


		int[] m64p_FrameBuffer;

		/// <summary>
		/// This function will be used as the frame callback. It pulls the framebuffer from mupen64plus
		/// </summary>
		public void Getm64pFrameBuffer()
		{
			int width = 0;
			int height = 0;

			// Get the size of the frame buffer
			GFXReadScreen2Res(IntPtr.Zero, ref width, ref height, 0);

			// If it's not the same size as the current one, change the sizes
			if (width != bizhawkCore.BufferWidth || height != bizhawkCore.BufferHeight)
			{
				SetVideoSize(width, height);
			}

			// Actually get the frame buffer
			GFXReadScreen2(m64p_FrameBuffer, ref width, ref height, 0);

			// vflip
			int fromindex = bizhawkCore.BufferWidth * (bizhawkCore.BufferHeight - 1) * 4;
			int toindex = 0;
			for (int j = 0; j < bizhawkCore.BufferHeight; j++)
			{
				Buffer.BlockCopy(m64p_FrameBuffer, fromindex, bizhawkCore.frameBuffer, toindex, bizhawkCore.BufferWidth * 4);
				fromindex -= bizhawkCore.BufferWidth * 4;
				toindex += bizhawkCore.BufferWidth * 4;
			}
		}

		/// <summary>
		/// This function will be used as the VI callback. It checks the audio rate and updates the resampler if necessary.
		/// It then polls the audio buffer for samples. It then marks the frame as complete.
		/// </summary>
		public void VI()
		{
			uint s = (uint)AudGetAudioRate();
			if (s != m64pSamplingRate)
			{
				m64pSamplingRate = s;
				bizhawkCore.resampler.ChangeRate(s, 44100, s, 44100);
				//Console.WriteLine("N64 ARate Change {0}", s);
			}

			int m64pAudioBufferSize = AudGetBufferSize();
			if (m64pAudioBuffer.Length < m64pAudioBufferSize)
				m64pAudioBuffer = new short[m64pAudioBufferSize];

			if (m64pAudioBufferSize > 0)
			{
				AudReadAudioBuffer(m64pAudioBuffer);
				bizhawkCore.resampler.EnqueueSamples(m64pAudioBuffer, m64pAudioBufferSize / 2);
			}
			m64pFrameComplete.Set();
		}

		public int get_memory_size(N64_MEMORY id)
		{
			return m64pMemGetSize(id);
		}

		public IntPtr get_memory_ptr(N64_MEMORY id)
		{
			return m64pDebugMemGetPointer(id);
		}

		public void set_buttons(int num, int keys, sbyte X, sbyte Y)
		{
			InpSetKeys(num, keys, X, Y);
		}

		public void soft_reset()
		{
			m64pCoreDoCommandPtr(m64p_command.M64CMD_RESET, 0, IntPtr.Zero);
		}

		public void hard_reset()
		{
			m64pCoreDoCommandPtr(m64p_command.M64CMD_RESET, 1, IntPtr.Zero);
		}

		public void frame_advance()
		{
			m64pCoreDoCommandPtr(m64p_command.M64CMD_ADVANCE_FRAME, 0, IntPtr.Zero);
			m64pFrameComplete.WaitOne();
		}

		public void SetM64PInputCallback(InputCallback inputCallback)
		{
			InpInputCallback = inputCallback;
			InpSetInputCallback(InpInputCallback);
		}

		public int SaveState(byte[] buffer)
		{
			return m64pCoreSaveState(buffer);
		}

		public void LoadState(byte[] buffer)
		{
			m64pCoreLoadState(buffer);
		}

		byte[] saveram_backup;

		public void InitSaveram()
		{
			m64pinit_saveram();
		}

		public byte[] SaveSaveram()
		{
			if (disposed)
			{
				if (saveram_backup != null)
				{
					return saveram_backup;
				}
				else
				{
					// This shouldn't happen!!
					return new byte[0x800 + 4 * 0x8000];
				}
			}
			else
			{
				byte[] dest = new byte[0x800 + 4 * 0x8000];
				m64psave_saveram(dest);
				return dest;
			}
		}

		public void LoadSaveram(byte[] src)
		{
			m64pload_saveram(src);
		}

		public void Dispose()
		{
			if (!disposed)
			{
				// Stop the core, and wait for it to end
				while (emulator_running)
				{
					// Repeatedly send the stop command, because sometimes sending it just once doesn't work
					m64pCoreDoCommandPtr(m64p_command.M64CMD_STOP, 0, IntPtr.Zero);
				}

				// Backup the saveram in case bizhawk wants to get at is after we've freed the libraries
				saveram_backup = SaveSaveram();

				bizhawkCore.resampler.Dispose();
				bizhawkCore.resampler = null;

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
	}

	public class VideoPluginSettings
	{
		public string PluginName;
		//public Dictionary<string, int> IntParameters = new Dictionary<string,int>();
		//public Dictionary<string, string> StringParameters = new Dictionary<string,string>();

		public Dictionary<string, object> Parameters = new Dictionary<string, object>();
		public int Height;
		public int Width;

		public VideoPluginSettings (string Name, int Width, int Height)
		{
			this.PluginName = Name;
			this.Width = Width;
			this.Height = Height;
		}
	}
}
