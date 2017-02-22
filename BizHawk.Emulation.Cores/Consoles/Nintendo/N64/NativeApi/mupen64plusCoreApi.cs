using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

using BizHawk.Emulation.Common;
using System.Text;

namespace BizHawk.Emulation.Cores.Nintendo.N64.NativeApi
{
	public class mupen64plusApi : IDisposable
	{
		// Only left in because api needs to know the number of frames passed
		// because of a bug
		private readonly N64 bizhawkCore;
		static mupen64plusApi AttachedCore = null;

		bool disposed = false;

		Thread m64pEmulator;

		AutoResetEvent m64pEvent = new AutoResetEvent(false);
		AutoResetEvent m64pContinueEvent = new AutoResetEvent(false);
		ManualResetEvent m64pStartupComplete = new ManualResetEvent(false);

		bool event_frameend = false;
		bool event_breakpoint = false;

		[DllImport("kernel32.dll")]
		public static extern UInt32 GetLastError();

		[DllImport("kernel32.dll")]
		public static extern IntPtr LoadLibrary(string dllToLoad);

		[DllImport("kernel32.dll")]
		public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

		[DllImport("kernel32.dll")]
		public static extern bool FreeLibrary(IntPtr hModule);

		public enum m64p_error
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

		public enum m64p_plugin_type
		{
			M64PLUGIN_NULL = 0,
			M64PLUGIN_RSP = 1,
			M64PLUGIN_GFX,
			M64PLUGIN_AUDIO,
			M64PLUGIN_INPUT,
			M64PLUGIN_CORE
		};

		private enum m64p_command
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
			M64CMD_SET_VI_CALLBACK,
			M64CMD_SET_RENDER_CALLBACK
		};

		public enum m64p_emu_state
		{
			M64EMU_STOPPED = 1,
			M64EMU_RUNNING,
			M64EMU_PAUSED
		};

		public enum m64p_type
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
			MEMPAK4,

			THE_ROM
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
		/// Sets a parameter in the global config system
		/// </summary>
		/// <param name="ConfigSectionHandle">The handle of the section to access</param>
		/// <param name="ParamName">The name of the parameter to set</param>
		/// <param name="ParamType">The type of the parameter</param>
		/// <param name="ParamValue">A pointer to the value to use for the parameter (must be a string)</param>
		/// <returns></returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate m64p_error ConfigSetParameterStr(IntPtr ConfigSectionHandle, string ParamName, m64p_type ParamType, StringBuilder ParamValue);
		ConfigSetParameterStr m64pConfigSetParameterStr;

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
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate m64p_error CoreDoCommandRenderCallback(m64p_command Command, int ParamInt, RenderCallback ParamPtr);
		CoreDoCommandRenderCallback m64pCoreDoCommandRenderCallback;

		//WARNING - RETURNS A STATIC BUFFER
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr biz_r4300_decode_op(uint instr, uint counter);
		public biz_r4300_decode_op m64p_decode_op; 

		/// <summary>
		/// Reads from the "system bus"
		/// </summary>
		/// <returns></returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate byte biz_read_memory(uint addr);
		public biz_read_memory m64p_read_memory_8;

		/// <summary>
		/// Writes to the "system bus"
		/// </summary>
		/// <returns></returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void biz_write_memory(uint addr, byte value);
		public biz_write_memory m64p_write_memory_8;

		// These are common for all four plugins

		/// <summary>
		/// Initializes the plugin
		/// </summary>
		/// <param name="CoreHandle">The DLL handle for the core DLL</param>
		/// <param name="Context">Giving a context to the DebugCallback</param>
		/// <param name="DebugCallback">A function to use when the pluging wants to output debug messages</param>
		/// <returns></returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate m64p_error PluginStartup(IntPtr CoreHandle, string Context, DebugCallback DebugCallback);

		/// <summary>
		/// Cleans up the plugin
		/// </summary>
		/// <returns></returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate m64p_error PluginShutdown();

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
		/// This will be called every time before the screen is drawn
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void RenderCallback();
		RenderCallback m64pRenderCallback;

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
		/// Sets the memory execute callback
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SetExecuteCallback(MemoryCallback callback);
		SetExecuteCallback m64pSetExecuteCallback;

		/// <summary>
		/// Type of the trace callback
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void TraceCallback();

		/// <summary>
		/// Sets the trace callback
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SetTraceCallback(TraceCallback callback);
		SetTraceCallback m64pSetTraceCallback;

		/// <summary>
		/// Gets the CPU registers
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void GetRegisters(byte[] dest);
		GetRegisters m64pGetRegisters;

		// DLL handles
		public IntPtr CoreDll { get; private set; }

		public mupen64plusApi(N64 bizhawkCore, byte[] rom, VideoPluginSettings video_settings, int SaveType, int CoreType, bool DisableExpansionSlot)
		{
			// There can only be one core (otherwise breaks mupen64plus)
			if (AttachedCore != null)
			{
				AttachedCore.Dispose();
				AttachedCore = null;
			}
			this.bizhawkCore = bizhawkCore;

			CoreDll = LoadLibrary("mupen64plus.dll");
			if (CoreDll == IntPtr.Zero)
				throw new InvalidOperationException(string.Format("Failed to load mupen64plus.dll"));

			connectFunctionPointers();

			// Start up the core
			m64p_error result = m64pCoreStartup(0x20001, "", "", "Core",
				null, "", IntPtr.Zero);

			// Open the core settings section in the config system
			IntPtr core_section = IntPtr.Zero;
			m64pConfigOpenSection("Core", ref core_section);

			// Set the savetype if needed
			if (DisableExpansionSlot)
			{
				int disable = 1;
				m64pConfigSetParameter(core_section, "DisableExtraMem", m64p_type.M64TYPE_INT, ref disable);
			}

			// Set the savetype if needed
			if (SaveType != 0)
			{
				m64pConfigSetParameter(core_section, "SaveType", m64p_type.M64TYPE_INT, ref SaveType);
			}

			m64pConfigSetParameter(core_section, "R4300Emulator", m64p_type.M64TYPE_INT, ref CoreType);

			// Pass the rom to the core
			result = m64pCoreDoCommandByteArray(m64p_command.M64CMD_ROM_OPEN, rom.Length, rom);

			// Open the general video settings section in the config system
			IntPtr video_section = IntPtr.Zero;
			m64pConfigOpenSection("Video-General", ref video_section);

			// Set the desired width and height for mupen64plus
			result = m64pConfigSetParameter(video_section, "ScreenWidth", m64p_type.M64TYPE_INT, ref video_settings.Width);
			result = m64pConfigSetParameter(video_section, "ScreenHeight", m64p_type.M64TYPE_INT, ref video_settings.Height);

			set_video_parameters(video_settings);

			InitSaveram();

			// Initialize event invoker
			m64pFrameCallback = new FrameCallback(FireFrameFinishedEvent);
			result = m64pCoreDoCommandFrameCallback(m64p_command.M64CMD_SET_FRAME_CALLBACK, 0, m64pFrameCallback);
			m64pVICallback = new VICallback(FireVIEvent);
			result = m64pCoreDoCommandVICallback(m64p_command.M64CMD_SET_VI_CALLBACK, 0, m64pVICallback);
			m64pRenderCallback = new RenderCallback(FireRenderEvent);
			result = m64pCoreDoCommandRenderCallback(m64p_command.M64CMD_SET_RENDER_CALLBACK, 0, m64pRenderCallback);

			// Prepare to start the emulator in a different thread
			m64pEmulator = new Thread(ExecuteEmulator);

			AttachedCore = this;
		}

		volatile bool emulator_running = false;

		/// <summary>
		/// Starts executing the emulator asynchronously
		/// Waits until the emulator booted up and than returns
		/// </summary>
		public void AsyncExecuteEmulator()
		{
			m64pEmulator.Start();

			// Wait for the core to boot up
			m64pStartupComplete.WaitOne();
		}

		/// <summary>
		/// Starts execution of mupen64plus
		/// Does not return until the emulator stops
		/// </summary>
		private void ExecuteEmulator()
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
			m64pCoreDoCommandRenderCallback = (CoreDoCommandRenderCallback)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "CoreDoCommand"), typeof(CoreDoCommandRenderCallback));
			m64pCoreAttachPlugin = (CoreAttachPlugin)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "CoreAttachPlugin"), typeof(CoreAttachPlugin));
			m64pCoreDetachPlugin = (CoreDetachPlugin)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "CoreDetachPlugin"), typeof(CoreDetachPlugin));
			m64pConfigOpenSection = (ConfigOpenSection)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "ConfigOpenSection"), typeof(ConfigOpenSection));
			m64pConfigSetParameter = (ConfigSetParameter)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "ConfigSetParameter"), typeof(ConfigSetParameter));
			m64pConfigSetParameterStr = (ConfigSetParameterStr)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "ConfigSetParameter"), typeof(ConfigSetParameterStr));
			m64pCoreSaveState = (savestates_save_bkm)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "savestates_save_bkm"), typeof(savestates_save_bkm));
			m64pCoreLoadState = (savestates_load_bkm)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "savestates_load_bkm"), typeof(savestates_load_bkm));
			m64pDebugMemGetPointer = (DebugMemGetPointer)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "DebugMemGetPointer"), typeof(DebugMemGetPointer));
			m64pMemGetSize = (MemGetSize)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "MemGetSize"), typeof(MemGetSize));
			m64pinit_saveram = (init_saveram)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "init_saveram"), typeof(init_saveram));
			m64psave_saveram = (save_saveram)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "save_saveram"), typeof(save_saveram));
			m64pload_saveram = (load_saveram)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "load_saveram"), typeof(load_saveram));

			m64pSetReadCallback = (SetReadCallback)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "SetReadCallback"), typeof(SetReadCallback));
			m64pSetWriteCallback = (SetWriteCallback)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "SetWriteCallback"), typeof(SetWriteCallback));
			m64pSetExecuteCallback = (SetExecuteCallback)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "SetExecuteCallback"), typeof(SetExecuteCallback));
			m64pSetTraceCallback = (SetTraceCallback)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "SetTraceCallback"), typeof(SetTraceCallback));

			m64pGetRegisters = (GetRegisters)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "GetRegisters"), typeof(GetRegisters));

			m64p_read_memory_8 = (biz_read_memory)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "biz_read_memory"), typeof(biz_read_memory));
			m64p_write_memory_8 = (biz_write_memory)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "biz_write_memory"), typeof(biz_write_memory));

			m64p_decode_op = (biz_r4300_decode_op)Marshal.GetDelegateForFunctionPointer(GetProcAddress(CoreDll, "biz_r4300_decode_op"), typeof(biz_r4300_decode_op));
		}

		/// <summary>
		/// Puts plugin settings of EmuHawk into mupen64plus
		/// </summary>
		/// <param name="video_settings">Settings to put into mupen64plus</param>
		public void set_video_parameters(VideoPluginSettings video_settings)
		{
			IntPtr video_plugin_section = IntPtr.Zero;
			if (video_settings.Plugin == PluginType.Rice)
			{
				m64pConfigOpenSection("Video-Rice", ref video_plugin_section);
			}
			else if (video_settings.Plugin == PluginType.Glide)
			{
				m64pConfigOpenSection("Video-Glide64", ref video_plugin_section);
			}
			else if (video_settings.Plugin == PluginType.GlideMk2)
			{
				m64pConfigOpenSection("Video-Glide64mk2", ref video_plugin_section);
			}
			else if (video_settings.Plugin == PluginType.Jabo)
			{
				m64pConfigOpenSection("Video-Jabo", ref video_plugin_section);
			}
			else if (video_settings.Plugin == PluginType.GLideN64)
			{
				m64pConfigOpenSection("Video-GLideN64", ref video_plugin_section);
			}
			else
			{
				return;
			}

			foreach (string Parameter in video_settings.Parameters.Keys)
			{
				if (video_settings.Parameters[Parameter].GetType() == typeof(string))
				{
					string value = ((string)video_settings.Parameters[Parameter]);
					StringBuilder sb = new StringBuilder(value);
					m64pConfigSetParameterStr(video_plugin_section, Parameter, m64p_type.M64TYPE_STRING, sb);
				}
				else
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
					else if (video_settings.Parameters[Parameter] is Enum)
					{
						value = (int)video_settings.Parameters[Parameter];
					}
					m64pConfigSetParameter(video_plugin_section, Parameter, m64p_type.M64TYPE_INT, ref value);
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

		public void soft_reset()
		{
			m64pCoreDoCommandPtr(m64p_command.M64CMD_RESET, 0, IntPtr.Zero);
		}

		public void hard_reset()
		{
			m64pCoreDoCommandPtr(m64p_command.M64CMD_RESET, 1, IntPtr.Zero);
		}

		public enum BreakType
		{
			Read, Write, Execute
		}

		public struct BreakParams
		{
			public BreakType _type;
			public uint _addr;
			public IMemoryCallbackSystem _mcs;
		}

		private BreakParams _breakparams;

		public void frame_advance()
		{
			event_frameend = false;
			m64pCoreDoCommandPtr(m64p_command.M64CMD_ADVANCE_FRAME, 0, IntPtr.Zero);

			//the way we should be able to do it:
			//m64pFrameComplete.WaitOne();
			
			//however. since this is probably an STAThread, this call results in message pumps running.
			//those message pumps are only supposed to respond to critical COM stuff, but in fact they interfere with other things.
			//so here are two workaround methods.

			//method 1.
			//BizHawk.Common.Win32ThreadHacks.HackyPinvokeWaitOne(m64pFrameComplete);

			//method 2.
			//BizHawk.Common.Win32ThreadHacks.HackyComWaitOne(m64pFrameComplete);

			for(;;)
			{
				BizHawk.Common.Win32ThreadHacks.HackyPinvokeWaitOne(m64pEvent);
				if (event_frameend)
					break;
				if (event_breakpoint)
				{
					switch (_breakparams._type)
					{
						case BreakType.Read:
							_breakparams._mcs.CallReads(_breakparams._addr);
							break;
						case BreakType.Write:
							_breakparams._mcs.CallWrites(_breakparams._addr);
							break;
						case BreakType.Execute:
							_breakparams._mcs.CallExecutes(_breakparams._addr);
							break;
					}
				}
				event_breakpoint = false;
                m64pContinueEvent.Set();
			}
		}

		public void OnBreakpoint(BreakParams breakparams)
		{
			_breakparams = breakparams;
			event_breakpoint = true; //order important
			m64pEvent.Set(); //order important
            BizHawk.Common.Win32ThreadHacks.HackyPinvokeWaitOne(m64pContinueEvent); //wait for emuhawk to finish event
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

		public const int kSaveramSize = 0x800 + 4 * 0x8000 + 0x20000 + 0x8000;

		public byte[] SaveSaveram()
		{
			if (disposed)
			{
				if (saveram_backup != null)
				{
					return (byte[])saveram_backup.Clone();
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

		public void setExecuteCallback(MemoryCallback callback)
		{
			m64pSetExecuteCallback(callback);
		}

		public void setTraceCallback(TraceCallback callback)
		{
			m64pSetTraceCallback(callback);
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

				DetachPlugin(m64p_plugin_type.M64PLUGIN_GFX);
				DetachPlugin(m64p_plugin_type.M64PLUGIN_AUDIO);
				DetachPlugin(m64p_plugin_type.M64PLUGIN_INPUT);
				DetachPlugin(m64p_plugin_type.M64PLUGIN_RSP);

				m64pCoreDoCommandPtr(m64p_command.M64CMD_ROM_CLOSE, 0, IntPtr.Zero);
				m64pCoreShutdown();
				FreeLibrary(CoreDll);

				disposed = true;
			}
		}

		struct AttachedPlugin
		{
			public PluginStartup dllStartup;
			public PluginShutdown dllShutdown;
			public IntPtr dllHandle;
		}
		Dictionary<m64p_plugin_type, AttachedPlugin> plugins = new Dictionary<m64p_plugin_type, AttachedPlugin>();

		public IntPtr AttachPlugin(m64p_plugin_type type, string PluginName)
		{
			if (plugins.ContainsKey(type))
				DetachPlugin(type);

			AttachedPlugin plugin;
			plugin.dllHandle = LoadLibrary(PluginName);
			if (plugin.dllHandle == IntPtr.Zero)
				throw new InvalidOperationException(string.Format("Failed to load plugin {0}, error code: 0x{1:X}", PluginName, GetLastError()));

			plugin.dllStartup = (PluginStartup)Marshal.GetDelegateForFunctionPointer(GetProcAddress(plugin.dllHandle, "PluginStartup"), typeof(PluginStartup));
			plugin.dllShutdown = (PluginShutdown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(plugin.dllHandle, "PluginShutdown"), typeof(PluginShutdown));
			plugin.dllStartup(CoreDll, null, null);

			m64p_error result = m64pCoreAttachPlugin(type, plugin.dllHandle);
			if (result != m64p_error.M64ERR_SUCCESS)
			{
				FreeLibrary(plugin.dllHandle);
				throw new InvalidOperationException(string.Format("Error during attaching plugin {0}", PluginName));
			}

			plugins.Add(type, plugin);
			return plugin.dllHandle;
		}

		public void DetachPlugin(m64p_plugin_type type)
		{
			AttachedPlugin plugin;
			if (plugins.TryGetValue(type, out plugin))
			{
				plugins.Remove(type);
				m64pCoreDetachPlugin(type);
				plugin.dllShutdown();
				FreeLibrary(plugin.dllHandle);
			}
		}

		public event Action FrameFinished;
		public event Action VInterrupt;
		public event Action BeforeRender;

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
			event_frameend = true; //order important
			m64pEvent.Set(); //order important
		}

		private void FireRenderEvent()
		{
			if (BeforeRender != null)
				BeforeRender();
		}

		private void CompletedFrameCallback()
		{
			m64pEvent.Set();
		}
	}
}
