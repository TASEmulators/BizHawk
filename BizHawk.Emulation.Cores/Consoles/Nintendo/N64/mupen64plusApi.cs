using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.N64;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	public class mupen64plusApi : IDisposable
	{
		// Only left in because api needs to know the number of frames passed
		// because of a bug
		private readonly N64 bizhawkCore;
		static mupen64plusApi AttachedCore = null;

		bool disposed = false;

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

		/// <summary>
		/// Type of the read/write memory callbacks
		/// </summary>
		/// <param name="address">The address which the cpu is read/writing</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void MemoryCallback(uint address);

		/// <summary>
		/// Sets the memory read callback
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SetReadCallback(MemoryCallback callback);
		SetReadCallback m64pSetReadCallback;

		/// <summary>
		/// Sets the memory write callback
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SetWriteCallback(MemoryCallback callback);
		SetWriteCallback m64pSetWriteCallback;

		/// <summary>
		/// Gets the CPU registers
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void GetRegisters(byte[] dest);
		GetRegisters m64pGetRegisters;

		// DLL handles
		IntPtr CoreDll;
		IntPtr GfxDll;
		IntPtr RspDll;
		IntPtr AudDll;
		IntPtr InpDll;

		public mupen64plusApi(N64 bizhawkCore, byte[] rom, VideoPluginSettings video_settings, int SaveType)
		{
			// There can only be one core (otherwise breaks mupen64plus)
			if (AttachedCore != null)
			{
				AttachedCore.Dispose();
				AttachedCore = null;
			}
			this.bizhawkCore = bizhawkCore;

			string VidDllName;
			if (video_settings.Plugin == PLUGINTYPE.RICE)
			{
				VidDllName = "mupen64plus-video-rice.dll";
			}
			else if (video_settings.Plugin == PLUGINTYPE.GLIDE)
			{
				VidDllName = "mupen64plus-video-glide64.dll";
			}
			else if (video_settings.Plugin == PLUGINTYPE.GLIDE64MK2)
			{
				VidDllName = "mupen64plus-video-glide64mk2.dll";
			}
			else
			{
				throw new InvalidOperationException(string.Format("Unknown plugin {0}", video_settings.Plugin));
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
			m64p_error result = m64pCoreStartup(0x20001, "", "", "Core",
				null, "", IntPtr.Zero);

			// Set the savetype if needed
			if (SaveType != 0)
			{
				IntPtr core_section = IntPtr.Zero;
				m64pConfigOpenSection("Core", ref core_section);
				m64pConfigSetParameter(core_section, "SaveType", m64p_type.M64TYPE_INT, ref SaveType);
			}

			// Pass the rom to the core
			result = m64pCoreDoCommandByteArray(m64p_command.M64CMD_ROM_OPEN, rom.Length, rom);

			// Open the general video settings section in the config system
			IntPtr video_section = IntPtr.Zero;
			m64pConfigOpenSection("Video-General", ref video_section);

			// Set the desired width and height for mupen64plus
			result = m64pConfigSetParameter(video_section, "ScreenWidth", m64p_type.M64TYPE_INT, ref video_settings.Width);
			result = m64pConfigSetParameter(video_section, "ScreenHeight", m64p_type.M64TYPE_INT, ref video_settings.Height);

			set_video_parameters(video_settings);

			// Order of plugin loading is important, do not change!
			// Set up and connect the graphics plugin
			result = GfxPluginStartup(CoreDll, "Video", null);
			result = m64pCoreAttachPlugin(m64p_plugin_type.M64PLUGIN_GFX, GfxDll);

			// Set up our audio plugin
			result = AudPluginStartup(CoreDll, "Audio", null);
			result = m64pCoreAttachPlugin(m64p_plugin_type.M64PLUGIN_AUDIO, AudDll);

			// Set up our input plugin
			result = InpPluginStartup(CoreDll, "Input", null);
			result = m64pCoreAttachPlugin(m64p_plugin_type.M64PLUGIN_INPUT, InpDll);

			// Set up and connect the RSP plugin
			result = RspPluginStartup(CoreDll, "RSP", null);
			result = m64pCoreAttachPlugin(m64p_plugin_type.M64PLUGIN_RSP, RspDll);

			InitSaveram();

			// Initialize event invoker
			m64pFrameCallback = new FrameCallback(FireFrameFinishedEvent);
			result = m64pCoreDoCommandFrameCallback(m64p_command.M64CMD_SET_FRAME_CALLBACK, 0, m64pFrameCallback);
			m64pVICallback = new VICallback(FireVIEvent);
			result = m64pCoreDoCommandVICallback(m64p_command.M64CMD_SET_VI_CALLBACK, 0, m64pVICallback);

			// Start the emulator in another thread
			m64pEmulator = new Thread(ExecuteEmulator);
			m64pEmulator.Start();

			// Wait for the core to boot up
			m64pStartupComplete.WaitOne();

			AttachedCore = this;
		}

		volatile bool emulator_running = false;
		/// <summary>
		/// Starts execution of mupen64plus
		/// Does not return until the emulator stops
		/// </summary>
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

			m64pSetReadCallback = (SetReadCallback)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "SetReadCallback"), typeof(SetReadCallback));
			m64pSetWriteCallback = (SetWriteCallback)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "SetWriteCallback"), typeof(SetWriteCallback));

			m64pGetRegisters = (GetRegisters)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "GetRegisters"), typeof(GetRegisters));

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

		/// <summary>
		/// Puts plugin settings of EmuHawk into mupen64plus
		/// </summary>
		/// <param name="video_settings">Settings to put into mupen64plus</param>
		public void set_video_parameters(VideoPluginSettings video_settings)
		{
			IntPtr video_plugin_section = IntPtr.Zero;
			if (video_settings.Plugin == PLUGINTYPE.RICE)
			{
				m64pConfigOpenSection("Video-Rice", ref video_plugin_section);
			}
			else if (video_settings.Plugin == PLUGINTYPE.GLIDE)
			{
				m64pConfigOpenSection("Video-Glide64", ref video_plugin_section);
			}
			else if (video_settings.Plugin == PLUGINTYPE.GLIDE64MK2)
			{
				m64pConfigOpenSection("Video-Glide64mk2", ref video_plugin_section);
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

		private int[] m64pBuffer = new int[0];
		/// <summary>
		/// This function copies the frame buffer from mupen64plus
		/// </summary>
		public void Getm64pFrameBuffer(int[] buffer, ref int width, ref int height)
		{
			if(m64pBuffer.Length != width * height)
				m64pBuffer = new int[width * height];
			// Actually get the frame buffer
			GFXReadScreen2(m64pBuffer, ref width, ref height, 0);

			// vflip
			int fromindex = width * (height - 1) * 4;
			int toindex = 0;

			for (int j = 0; j < height; j++)
			{
				Buffer.BlockCopy(m64pBuffer, fromindex, buffer, toindex, width * 4);
				fromindex -= width * 4;
				toindex += width * 4;
			}
			
			// opaque
			unsafe
			{
				fixed (int* ptr = &buffer[0])
				{
					int l = buffer.Length;
					for (int i = 0; i < l; i++)
					{
						ptr[i] |= unchecked((int)0xff000000);
					}
				}
			}
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
			// This is a dirty hack...
			// When using the dynamic recompiler if a state is loaded too early some pointers are not set up yet, so mupen
			// tries to access null pointers and the emulator crashes. It seems like it takes 2 frames to fully set up the recompiler,
			// so if two frames haven't been run yet, run them, then load the state.

			if (bizhawkCore.Frame < 2)
			{
				frame_advance();
				frame_advance();
			}
			
			m64pCoreLoadState(buffer);
		}

		byte[] saveram_backup;

		public void InitSaveram()
		{
			m64pinit_saveram();
		}

		public const int kSaveramSize = 0x800 + 4 * 0x8000 + 0x20000 + 0x8000;

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
					return new byte[kSaveramSize];
				}
			}
			else
			{
				byte[] dest = new byte[kSaveramSize];
				m64psave_saveram(dest);
				return dest;
			}
		}

		public void LoadSaveram(byte[] src)
		{
			m64pload_saveram(src);
		}

		public void setReadCallback(MemoryCallback callback)
		{
			m64pSetReadCallback(callback);
		}
		
		public void setWriteCallback(MemoryCallback callback)
		{
			m64pSetWriteCallback(callback);
		}

		public void getRegisters(byte[] dest)
		{
			m64pGetRegisters(dest);
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

		public void GetScreenDimensions(ref int width, ref int height)
		{
			GFXReadScreen2Res(IntPtr.Zero, ref width, ref height, 0);
		}

		public uint GetSamplingRate()
		{
			return (uint)AudGetAudioRate();
		}

		public int GetAudioBufferSize()
		{
			return AudGetBufferSize();
		}

		public void GetAudioBuffer(short[] buffer)
		{
			AudReadAudioBuffer(buffer);
		}

		public event Action FrameFinished;
		public event Action VInterrupt;

		private void FireFrameFinishedEvent()
		{
			// Execute Frame Callback functions
			if (FrameFinished != null)
				FrameFinished();
		}

		private void FireVIEvent()
		{
			// Execute VI Callback functions
			if (VInterrupt != null)
				VInterrupt();
			m64pFrameComplete.Set();
		}

		private void CompletedFrameCallback()
		{
			m64pFrameComplete.Set();
		}
	}

	public class VideoPluginSettings
	{
		public PLUGINTYPE Plugin;
		//public Dictionary<string, int> IntParameters = new Dictionary<string,int>();
		//public Dictionary<string, string> StringParameters = new Dictionary<string,string>();

		public Dictionary<string, object> Parameters = new Dictionary<string, object>();
		public int Height;
		public int Width;

		public VideoPluginSettings (PLUGINTYPE Plugin, int Width, int Height)
		{
			this.Plugin = Plugin;
			this.Width = Width;
			this.Height = Height;
		}
	}
}
