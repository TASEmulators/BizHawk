using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using System.Text;

namespace BizHawk.Emulation.Cores.Nintendo.N64.NativeApi
{
	public class mupen64plusApi : IDisposable
	{
		// Only left in because api needs to know the number of frames passed
		// because of a bug
		private readonly N64 bizhawkCore;
		private static mupen64plusApi AttachedCore = null;

		private bool disposed = false;

		private readonly Thread m64pEmulator;

		private readonly AutoResetEvent m64pEvent = new AutoResetEvent(false);
		private AutoResetEvent m64pContinueEvent = new AutoResetEvent(false);
		private readonly ManualResetEvent m64pStartupComplete = new ManualResetEvent(false);

		private bool event_frameend = false;
		private bool event_breakpoint = false;

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
		}

		public enum m64p_plugin_type
		{
			M64PLUGIN_NULL = 0,
			M64PLUGIN_RSP = 1,
			M64PLUGIN_GFX,
			M64PLUGIN_AUDIO,
			M64PLUGIN_INPUT,
			M64PLUGIN_CORE
		}

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
		}

		private enum m64p_dbg_bkp_command
		{
			M64P_BKP_CMD_ADD_ADDR = 1,
			M64P_BKP_CMD_ADD_STRUCT,
			M64P_BKP_CMD_GET_STRUCT,
			M64P_BKP_CMD_REPLACE,
			M64P_BKP_CMD_REMOVE_ADDR,
			M64P_BKP_CMD_REMOVE_IDX,
			M64P_BKP_CMD_ENABLE,
			M64P_BKP_CMD_DISABLE,
			M64P_BKP_CMD_CHECK
		}

		[Flags]
		private enum m64p_dbg_bkp_flags
		{
			M64P_BPT_FLAG_ENABLED = 0x01,
			M64P_BPT_FLAG_CONDITIONAL = 0x02,
			M64P_BPT_FLAG_COUNTER = 0x04,
			M64P_BPT_FLAG_READ = 0x08,
			M64P_BPT_FLAG_WRITE = 0x10,
			M64P_BPT_FLAG_EXEC = 0x20,
			M64P_BPT_FLAG_LOG = 0x40
		}

		private enum m64p_dbg_state
		{
			M64P_DBG_RUN_STATE = 1,
			M64P_DBG_PREVIOUS_PC,
			M64P_DBG_NUM_BREAKPOINTS,
			M64P_DBG_CPU_DYNACORE,
			M64P_DBG_CPU_NEXT_INTERRUPT
		}

		private enum m64p_dbg_runstate
		{
			M64P_DBG_RUNSTATE_PAUSED = 0,
			M64P_DBG_RUNSTATE_STEPPING,
			M64P_DBG_RUNSTATE_RUNNING
		}

		public enum m64p_emu_state
		{
			M64EMU_STOPPED = 1,
			M64EMU_RUNNING,
			M64EMU_PAUSED
		}

		public enum m64p_type
		{
			M64TYPE_INT = 1,
			M64TYPE_FLOAT,
			M64TYPE_BOOL,
			M64TYPE_STRING
		}

		public enum N64_MEMORY : uint
		{
			RDRAM = 1,
			PI_REG,
			SI_REG,
			VI_REG,
			RI_REG,
			AI_REG,

			EEPROM = 100,
			SRAM,
			FLASHRAM,
			MEMPAK1,
			MEMPAK2,
			MEMPAK3,
			MEMPAK4,

			THE_ROM
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct m64p_breakpoint
		{
			public uint address;
			public uint endaddr;
			public uint flags;
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
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate m64p_error CoreStartup(int APIVersion, string ConfigPath, string DataPath, string Context, DebugCallback DebugCallback, string context2, IntPtr dummy);

		private CoreStartup m64pCoreStartup;

		/// <summary>
		/// Cleans up the core
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate m64p_error CoreShutdown();

		private CoreShutdown m64pCoreShutdown;

		/// <summary>
		/// Connects a plugin DLL to the core DLL
		/// </summary>
		/// <param name="PluginType">The type of plugin that is being connected</param>
		/// <param name="PluginLibHandle">The DLL handle for the plugin</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate m64p_error CoreAttachPlugin(m64p_plugin_type PluginType, IntPtr PluginLibHandle);

		private CoreAttachPlugin m64pCoreAttachPlugin;

		/// <summary>
		/// Disconnects a plugin DLL from the core DLL
		/// </summary>
		/// <param name="PluginType">The type of plugin to be disconnected</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate m64p_error CoreDetachPlugin(m64p_plugin_type PluginType);

		private CoreDetachPlugin m64pCoreDetachPlugin;

		/// <summary>
		/// Opens a section in the global config system
		/// </summary>
		/// <param name="SectionName">The name of the section to open</param>
		/// <param name="ConfigSectionHandle">A pointer to the pointer to use as the section handle</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate m64p_error ConfigOpenSection(string SectionName, ref IntPtr ConfigSectionHandle);

		private ConfigOpenSection m64pConfigOpenSection;

		/// <summary>
		/// Sets a parameter in the global config system
		/// </summary>
		/// <param name="ConfigSectionHandle">The handle of the section to access</param>
		/// <param name="ParamName">The name of the parameter to set</param>
		/// <param name="ParamType">The type of the parameter</param>
		/// <param name="ParamValue">A pointer to the value to use for the parameter (must be an int right now)</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate m64p_error ConfigSetParameter(IntPtr ConfigSectionHandle, string ParamName, m64p_type ParamType, ref int ParamValue);

		private ConfigSetParameter m64pConfigSetParameter;

		/// <summary>
		/// Sets a parameter in the global config system
		/// </summary>
		/// <param name="ConfigSectionHandle">The handle of the section to access</param>
		/// <param name="ParamName">The name of the parameter to set</param>
		/// <param name="ParamType">The type of the parameter</param>
		/// <param name="ParamValue">A pointer to the value to use for the parameter (must be a string)</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate m64p_error ConfigSetParameterStr(IntPtr ConfigSectionHandle, string ParamName, m64p_type ParamType, StringBuilder ParamValue);

		private ConfigSetParameterStr m64pConfigSetParameterStr;

		/// <summary>
		/// Saves the mupen64plus state to the provided buffer
		/// </summary>
		/// <param name="buffer">A byte array to use to save the state. Must be at least 16788288 + 1024 bytes</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int savestates_save_bkm(byte[] buffer);

		private savestates_save_bkm m64pCoreSaveState;

		/// <summary>
		/// Loads the mupen64plus state from the provided buffer
		/// </summary>
		/// <param name="buffer">A byte array filled with the state to load. Must be at least 16788288 + 1024 bytes</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int savestates_load_bkm(byte[] buffer);

		private savestates_load_bkm m64pCoreLoadState;

		/// <summary>
		/// Gets a pointer to a section of the mupen64plus core
		/// </summary>
		/// <param name="mem_ptr_type">The section to get a pointer for</param>
		/// <returns>A pointer to the section requested</returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate IntPtr DebugMemGetPointer(N64_MEMORY mem_ptr_type);

		private DebugMemGetPointer m64pDebugMemGetPointer;

		/// <summary>
		/// Gets the size of the given memory area
		/// </summary>
		/// <param name="mem_ptr_type">The section to get the size of</param>
		/// <returns>The size of the section requested</returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int MemGetSize(N64_MEMORY mem_ptr_type);

		private MemGetSize m64pMemGetSize;

		/// <summary>
		/// Initializes the saveram (eeprom and 4 mempacks)
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate IntPtr init_saveram();

		private init_saveram m64pinit_saveram;

		/// <summary>
		/// Pulls out the saveram for bizhawk to save
		/// </summary>
		/// <param name="dest">A byte array to save the saveram into</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate IntPtr save_saveram(byte[] dest);

		private save_saveram m64psave_saveram;

		/// <summary>
		/// Restores the saveram from bizhawk
		/// </summary>
		/// <param name="src">A byte array containing the saveram to restore</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate IntPtr load_saveram(byte[] src);

		private load_saveram m64pload_saveram;

		// The last parameter of CoreDoCommand is actually a void pointer, so instead we'll make a few delegates for the versions we want to use
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate m64p_error CoreDoCommandByteArray(m64p_command Command, int ParamInt, byte[] ParamPtr);

		private CoreDoCommandByteArray m64pCoreDoCommandByteArray;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate m64p_error CoreDoCommandPtr(m64p_command Command, int ParamInt, IntPtr ParamPtr);

		private CoreDoCommandPtr m64pCoreDoCommandPtr;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate m64p_error CoreDoCommandRefInt(m64p_command Command, int ParamInt, ref int ParamPtr);

		private CoreDoCommandRefInt m64pCoreDoCommandRefInt;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate m64p_error CoreDoCommandFrameCallback(m64p_command Command, int ParamInt, FrameCallback ParamPtr);

		private CoreDoCommandFrameCallback m64pCoreDoCommandFrameCallback;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate m64p_error CoreDoCommandVICallback(m64p_command Command, int ParamInt, VICallback ParamPtr);

		private CoreDoCommandVICallback m64pCoreDoCommandVICallback;
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate m64p_error CoreDoCommandRenderCallback(m64p_command Command, int ParamInt, RenderCallback ParamPtr);

		private CoreDoCommandRenderCallback m64pCoreDoCommandRenderCallback;

		//WARNING - RETURNS A STATIC BUFFER
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr biz_r4300_decode_op(uint instr, uint counter);
		public biz_r4300_decode_op m64p_decode_op; 

		/// <summary>
		/// Reads from the "system bus"
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate byte biz_read_memory(uint addr);
		public biz_read_memory m64p_read_memory_8;

		/// <summary>
		/// Writes to the "system bus"
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void biz_write_memory(uint addr, byte value);
		public biz_write_memory m64p_write_memory_8;

		// These are common for all four plugins

		/// <summary>
		/// Initializes the plugin
		/// </summary>
		/// <param name="CoreHandle">The DLL handle for the core DLL</param>
		/// <param name="Context">Giving a context to the DebugCallback</param>
		/// <param name="DebugCallback">A function to use when the plugin wants to output debug messages</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate m64p_error PluginStartup(IntPtr CoreHandle, string Context, DebugCallback DebugCallback);

		/// <summary>
		/// Cleans up the plugin
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate m64p_error PluginShutdown();

		// Callback functions

		/// <summary>
		/// Handles a debug message from mupen64plus
		/// </summary>
		/// <param name="Message">The message to display</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void DebugCallback(IntPtr Context, int level, string Message);

		/// <summary>
		/// This will be called every time a new frame is finished
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void FrameCallback();

		private FrameCallback m64pFrameCallback;

		/// <summary>
		/// This will be called every time a VI occurs
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void VICallback();

		private VICallback m64pVICallback;

		/// <summary>
		/// This will be called every time before the screen is drawn
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void RenderCallback();

		private RenderCallback m64pRenderCallback;

		/// <summary>
		/// This will be called after the emulator is setup and is ready to be used
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void StartupCallback();

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

		private SetTraceCallback m64pSetTraceCallback;

		/// <summary>
		/// Gets the CPU registers
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void GetRegisters(byte[] dest);

		private GetRegisters m64pGetRegisters;

		/// <summary>
		/// This will be called when the debugger is initialized
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void DebugInitCallback();

		private DebugInitCallback m64pDebugInitCallback;

		/// <summary>
		/// This will be called when the debugger hits a breakpoint or executes one instruction in stepping mode
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void DebugUpdateCallback(int bpt);

		private DebugUpdateCallback m64pDebugUpdateCallback;

		/// <summary>
		/// This will be called during each vertical interrupt
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void DebugVICallback();

		/// <summary>
		///  Sets the debug callbacks
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate m64p_error DebugSetCallbacks(DebugInitCallback initCallback, DebugUpdateCallback updateCallback, DebugVICallback viCallback);

		private DebugSetCallbacks m64pDebugSetCallbacks;

		/// <summary>
		/// This function searches through all current breakpoints in the debugger to find one that matches the given input parameters.
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int DebugBreakpointLookup(uint address, uint size, uint flags);

		private DebugBreakpointLookup m64pDebugBreakpointLookup;

		/// <summary>
		/// This function is used to process common breakpoint commands, such as adding, removing, or searching the breakpoints
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int DebugBreakpointCommand(m64p_dbg_bkp_command command, uint index, ref m64p_breakpoint bkp);

		private DebugBreakpointCommand m64pDebugBreakpointCommand;

		/// <summary>
		/// Gets a debugger state variable
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int DebugGetState(m64p_dbg_state statenum);

		private DebugGetState m64pDebugGetState;

		/// <summary>
		/// Sets the runstate of the emulator
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate m64p_error DebugSetRunState(m64p_dbg_runstate runstate);

		private DebugSetRunState m64pDebugSetRunState;

		/// <summary>
		/// Continues execution
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate m64p_error DebugStep();

		private DebugStep m64pDebugStep;

		private readonly DynamicLibraryImportResolver Library = new DynamicLibraryImportResolver(OSTailoredCode.IsUnixHost ? "libmupen64plus.so.2" : "mupen64plus.dll");

		public mupen64plusApi(N64 bizhawkCore, byte[] rom, VideoPluginSettings video_settings, int SaveType, int CoreType, bool DisableExpansionSlot)
		{
			// There can only be one core (otherwise breaks mupen64plus)
			if (AttachedCore != null)
			{
				AttachedCore.Dispose();
				AttachedCore = null;
			}
			this.bizhawkCore = bizhawkCore;

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

			int enableDebugger = 1;

			m64pConfigSetParameter(core_section, "R4300Emulator", m64p_type.M64TYPE_INT, ref CoreType);
			m64pConfigSetParameter(core_section, "EnableDebugger", m64p_type.M64TYPE_INT, ref enableDebugger);

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
			m64pDebugInitCallback = new DebugInitCallback(OnDebuggerInitialized);
			m64pDebugUpdateCallback = new DebugUpdateCallback(FireBreakpointEvent);
			result = m64pDebugSetCallbacks(m64pDebugInitCallback, m64pDebugUpdateCallback, null);

			// Prepare to start the emulator in a different thread
			m64pEmulator = new Thread(ExecuteEmulatorThread);

			AttachedCore = this;
		}

		private volatile bool emulator_running = false;

		public bool IsCrashed => !emulator_running;

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
		private void ExecuteEmulatorThread()
		{
			emulator_running = true;
			var cb = new StartupCallback(() => m64pStartupComplete.Set());
			m64pCoreDoCommandPtr(m64p_command.M64CMD_EXECUTE, 0,
				Marshal.GetFunctionPointerForDelegate(cb));
			emulator_running = false;
			cb.GetType();
		}

		internal static T GetTypedDelegate<T>(IntPtr lib, string proc) where T : Delegate => (T) Marshal.GetDelegateForFunctionPointer(OSTailoredCode.LinkedLibManager.GetProcAddrOrThrow(lib, proc), typeof(T));

		/// <summary>
		/// Look up function pointers in the dlls
		/// </summary>
		private void connectFunctionPointers()
		{
			T GetCoreDelegate<T>(string proc) where T : Delegate => (T) Marshal.GetDelegateForFunctionPointer(Library.GetProcAddrOrThrow(proc), typeof(T));

			m64pCoreStartup = GetCoreDelegate<CoreStartup>("CoreStartup");
			m64pCoreShutdown = GetCoreDelegate<CoreShutdown>("CoreShutdown");
			m64pCoreDoCommandByteArray = GetCoreDelegate<CoreDoCommandByteArray>("CoreDoCommand");
			m64pCoreDoCommandPtr = GetCoreDelegate<CoreDoCommandPtr>("CoreDoCommand");
			m64pCoreDoCommandRefInt = GetCoreDelegate<CoreDoCommandRefInt>("CoreDoCommand");
			m64pCoreDoCommandFrameCallback = GetCoreDelegate<CoreDoCommandFrameCallback>("CoreDoCommand");
			m64pCoreDoCommandVICallback = GetCoreDelegate<CoreDoCommandVICallback>("CoreDoCommand");
			m64pCoreDoCommandRenderCallback = GetCoreDelegate<CoreDoCommandRenderCallback>("CoreDoCommand");
			m64pCoreAttachPlugin = GetCoreDelegate<CoreAttachPlugin>("CoreAttachPlugin");
			m64pCoreDetachPlugin = GetCoreDelegate<CoreDetachPlugin>("CoreDetachPlugin");
			m64pConfigOpenSection = GetCoreDelegate<ConfigOpenSection>("ConfigOpenSection");
			m64pConfigSetParameter = GetCoreDelegate<ConfigSetParameter>("ConfigSetParameter");
			m64pConfigSetParameterStr = GetCoreDelegate<ConfigSetParameterStr>("ConfigSetParameter");
			m64pCoreSaveState = GetCoreDelegate<savestates_save_bkm>("savestates_save_bkm");
			m64pCoreLoadState = GetCoreDelegate<savestates_load_bkm>("savestates_load_bkm");
			m64pDebugMemGetPointer = GetCoreDelegate<DebugMemGetPointer>("DebugMemGetPointer");
			m64pDebugSetCallbacks = GetCoreDelegate<DebugSetCallbacks>("DebugSetCallbacks");
			m64pDebugBreakpointLookup = GetCoreDelegate<DebugBreakpointLookup>("DebugBreakpointLookup");
			m64pDebugBreakpointCommand = GetCoreDelegate<DebugBreakpointCommand>("DebugBreakpointCommand");
			m64pDebugGetState = GetCoreDelegate<DebugGetState>("DebugGetState");
			m64pDebugSetRunState = GetCoreDelegate<DebugSetRunState>("DebugSetRunState");
			m64pDebugStep = GetCoreDelegate<DebugStep>("DebugStep");
			m64pMemGetSize = GetCoreDelegate<MemGetSize>("MemGetSize");
			m64pinit_saveram = GetCoreDelegate<init_saveram>("init_saveram");
			m64psave_saveram = GetCoreDelegate<save_saveram>("save_saveram");
			m64pload_saveram = GetCoreDelegate<load_saveram>("load_saveram");

			m64pSetTraceCallback = GetCoreDelegate<SetTraceCallback>("SetTraceCallback");

			m64pGetRegisters = GetCoreDelegate<GetRegisters>("GetRegisters");

			m64p_read_memory_8 = GetCoreDelegate<biz_read_memory>("biz_read_memory");
			m64p_write_memory_8 = GetCoreDelegate<biz_write_memory>("biz_write_memory");

			m64p_decode_op = GetCoreDelegate<biz_r4300_decode_op>("biz_r4300_decode_op");
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
			else if (video_settings.Plugin == PluginType.GLideN64)
			{
				m64pConfigOpenSection("Video-GLideN64", ref video_plugin_section);
			}
			else if (video_settings.Plugin == PluginType.Angrylion)
			{
				m64pConfigOpenSection("Video-Angrylion", ref video_plugin_section);
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
#if false // for alt. method #2 below
		static bool IsNativeWaitSuccessful(uint count, uint nativeResult, out int managedResult)
		{
			const uint WAIT_OBJECT_0 = 0x00000000U;
			const uint WAIT_ABANDONED_0 = 0x00000080U;
			const uint WAIT_TIMEOUT = 0x00000102U;
			if (/* WAIT_OBJECT_0 <= nativeResult && */ nativeResult < WAIT_OBJECT_0 + count)
			{
				managedResult = unchecked((int) (nativeResult - WAIT_OBJECT_0));
				return true;
			}
			else if (nativeResult == WAIT_OBJECT_0 + count)
			{
				// a is message pending, only valid for MsgWaitForMultipleObjectsEx
				managedResult = unchecked((int) nativeResult);
				return false;
			}
			else if (WAIT_ABANDONED_0 <= nativeResult && nativeResult < WAIT_ABANDONED_0 + count)
			{
				managedResult = unchecked((int) (nativeResult - WAIT_ABANDONED_0));
				throw new AbandonedMutexException();
			}
			else if (nativeResult == WAIT_TIMEOUT)
			{
				managedResult = WaitHandle.WaitTimeout;
				return false;
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		static void HackyComWaitOne(WaitHandle handle)
		{
			IntPtr[] waitHandles = { handle.SafeWaitHandle.DangerousGetHandle() };
			const uint count = 1;
			var QS_MASK = WmImports.QS_ALLINPUT; // message queue status
			QS_MASK = 0; //bizhawk edit?? did we need any messages here?? apparently not???
			uint nativeResult;
			WmImports.MSG msg;
			while (true)
			{
				// MsgWaitForMultipleObjectsEx with MWMO_INPUTAVAILABLE returns,
				// even if there's a message already seen but not removed in the message queue
				nativeResult = ThreadHacks.MsgWaitForMultipleObjectsEx(count, waitHandles, 0xFFFFFFFF, QS_MASK, ThreadHacks.MWMO_INPUTAVAILABLE);
				if (IsNativeWaitSuccessful(count, nativeResult, out int managedResult) || WaitHandle.WaitTimeout == managedResult) break;
				// there is a message, pump and dispatch it
				if (WmImports.PeekMessageW(out msg, IntPtr.Zero, 0, 0, WmImports.PM_REMOVE))
				{
					WmImports.TranslateMessage(ref msg);
					WmImports.DispatchMessageW(ref msg);
				}
			}
//			handle.WaitOne();
		}
#endif

			if (!emulator_running) return;

			event_frameend = false;
			m64pCoreDoCommandPtr(m64p_command.M64CMD_ADVANCE_FRAME, 0, IntPtr.Zero);

			for(;;)
			{
#if false // the way we should be able to do it
				m64pEvent.WaitOne();
				// however. since this is probably an STAThread, this call results in message pumps running.
				// those message pumps are only supposed to respond to critical COM stuff, but in fact they interfere with other things.
				// so here are two workaround methods:
#elif true // alt. method #1 - functionally the same as WaitOne, but does not message pump
				ThreadHacks.WaitForSingleObject(m64pEvent.SafeWaitHandle, 200);
#else // alt. method #2 - functionally the same as WaitOne(), but pumps com messages
				HackyComWaitOne(m64pEvent);
#endif
				if (event_frameend)
					break;
				if (event_breakpoint)
				{
					MemoryCallbackFlags flags = 0;
					switch (_breakparams._type)
					{
						case BreakType.Read:
							flags |= MemoryCallbackFlags.AccessRead;
							break;
						case BreakType.Write:
							flags |= MemoryCallbackFlags.AccessWrite;
							break;
						case BreakType.Execute:
							flags |= MemoryCallbackFlags.AccessExecute;
							break;
					}

					_breakparams._mcs.CallMemoryCallbacks(_breakparams._addr, 0, (uint)flags, "System Bus");

					event_breakpoint = false;
					Resume();
					continue;
				}
				//no event.. must be a timeout
				//check if the core crashed and bail if it did
				//otherwise wait longer (could be inside slow emulation or lua logic)
				if (!emulator_running)
					break;
			}
		}

		public void OnBreakpoint(BreakParams breakparams)
		{
			_breakparams = breakparams;
			event_breakpoint = true; //order important
			m64pEvent.Set(); //order important
		}

		public int SaveState(byte[] buffer)
		{
			return m64pCoreSaveState(buffer);
		}

		public void LoadState(byte[] buffer)
		{
			m64pCoreLoadState(buffer);
		}

		private byte[] saveram_backup;

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

		/* TODO: Support address masks */
		public void SetBreakpoint(BreakType type, uint? address)
		{
			m64p_breakpoint breakpoint = address is null
				? new m64p_breakpoint()
				{
					// For null address, specify max address range to match any address
					address = 0x0,
					endaddr = uint.MaxValue,
					flags = (uint)m64p_dbg_bkp_flags.M64P_BPT_FLAG_ENABLED
				}
				: new m64p_breakpoint()
				{
					address = address.Value,
					endaddr = address.Value + 0x03,
					flags = (uint)m64p_dbg_bkp_flags.M64P_BPT_FLAG_ENABLED
				};

			switch(type)
			{
				case BreakType.Read:
					breakpoint.flags |= (uint)m64p_dbg_bkp_flags.M64P_BPT_FLAG_READ;
					break;

				case BreakType.Write:
					breakpoint.flags |= (uint)m64p_dbg_bkp_flags.M64P_BPT_FLAG_WRITE;
					break;

				case BreakType.Execute:
					breakpoint.flags |= (uint)m64p_dbg_bkp_flags.M64P_BPT_FLAG_EXEC;
					break;
			}

			m64pDebugBreakpointCommand(m64p_dbg_bkp_command.M64P_BKP_CMD_ADD_STRUCT, 0, ref breakpoint);
		}

		public void RemoveBreakpoint(BreakType type, uint? address)
		{
			int index = 0;

			// Convert null (any) address to breakpoint with max range
			uint size = address != null ? 4 : uint.MaxValue;
			address ??= 0x0;

			switch(type)
			{
				case BreakType.Read:
					index = m64pDebugBreakpointLookup(address.Value, size, (uint)m64p_dbg_bkp_flags.M64P_BPT_FLAG_READ);
					break;

				case BreakType.Write:
					index = m64pDebugBreakpointLookup(address.Value, size, (uint)m64p_dbg_bkp_flags.M64P_BPT_FLAG_WRITE);
					break;

				case BreakType.Execute:
					index = m64pDebugBreakpointLookup(address.Value, size, (uint)m64p_dbg_bkp_flags.M64P_BPT_FLAG_EXEC);
					break;
			}

			m64p_breakpoint unused = new m64p_breakpoint();

			m64pDebugBreakpointCommand(m64p_dbg_bkp_command.M64P_BKP_CMD_REMOVE_IDX, (uint)index, ref unused);
		}

		public void setTraceCallback(TraceCallback callback)
		{
			m64pSetTraceCallback(callback);
		}

		public void getRegisters(byte[] dest)
		{
			m64pGetRegisters(dest);
		}

		public void Step()
		{
			m64pDebugStep();
		}

		public void Resume()
		{
			m64pDebugSetRunState(m64p_dbg_runstate.M64P_DBG_RUNSTATE_RUNNING);
			m64pDebugStep();
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
				Library.Dispose();

				disposed = true;
			}
		}

		private static readonly FieldInfo fiDLIRInternalPtr = typeof(DynamicLibraryImportResolver).GetField("_p", BindingFlags.Instance | BindingFlags.NonPublic);

		private struct AttachedPlugin
		{
			public DynamicLibraryImportResolver dllThinWrapper;
			public PluginShutdown dllShutdown;
		}

		private readonly Dictionary<m64p_plugin_type, AttachedPlugin> plugins = new Dictionary<m64p_plugin_type, AttachedPlugin>();

		public IntPtr AttachPlugin(m64p_plugin_type type, string PluginName)
		{
			static IntPtr GetDLIRPtrByRefl(DynamicLibraryImportResolver dlir) => (IntPtr) fiDLIRInternalPtr.GetValue(dlir);
			if (plugins.ContainsKey(type)) DetachPlugin(type);
			var lib = new DynamicLibraryImportResolver(PluginName);
			var libPtr = GetDLIRPtrByRefl(lib);
			GetTypedDelegate<PluginStartup>(libPtr, "PluginStartup")(GetDLIRPtrByRefl(Library), null, null);
			if (m64pCoreAttachPlugin(type, libPtr) != m64p_error.M64ERR_SUCCESS)
			{
				lib.Dispose();
				throw new InvalidOperationException($"Error during attaching plugin {PluginName}");
			}
			plugins.Add(type, new AttachedPlugin { dllThinWrapper = lib, dllShutdown = GetTypedDelegate<PluginShutdown>(libPtr, "PluginShutdown") });
			return libPtr;
		}

		public void DetachPlugin(m64p_plugin_type type)
		{
			if (plugins.TryGetValue(type, out var plugin))
			{
				plugins.Remove(type);
				m64pCoreDetachPlugin(type);
				plugin.dllShutdown();
				plugin.dllThinWrapper.Dispose();
			}
		}

		public event Action FrameFinished;
		public event Action VInterrupt;
		public event Action BeforeRender;

		public delegate void BreakpointHitCallback(uint address, BreakType type);
		public event BreakpointHitCallback BreakpointHit;

		private void FireFrameFinishedEvent()
		{
			// Execute Frame Callback functions
			FrameFinished?.Invoke();
		}

		private void FireVIEvent()
		{
			// Execute VI Callback functions
			VInterrupt?.Invoke();
			event_frameend = true; //order important
			m64pEvent.Set(); //order important
		}

		private void FireRenderEvent()
		{
			BeforeRender?.Invoke();
		}

		private bool CheckBreakpointFlag(ref m64p_breakpoint bkp, m64p_dbg_bkp_flags flag)
		{
			return ((bkp.flags & (uint)flag) != 0);
		}

		private void FireBreakpointEvent(int bpt)
		{
			// bpt equal to -1 means we're stepping
			if (bpt == -1 || BreakpointHit == null)
			{
				return;
			}

			m64p_breakpoint breakpoint = new m64p_breakpoint();

			m64pDebugBreakpointCommand(m64p_dbg_bkp_command.M64P_BKP_CMD_GET_STRUCT, (uint)bpt, ref breakpoint);

			BreakType type = BreakType.Execute;

			if(CheckBreakpointFlag(ref breakpoint, m64p_dbg_bkp_flags.M64P_BPT_FLAG_READ))
			{
				type = BreakType.Read;
			}
			else if(CheckBreakpointFlag(ref breakpoint, m64p_dbg_bkp_flags.M64P_BPT_FLAG_WRITE))
			{
				type = BreakType.Write;
			}

			BreakpointHit(breakpoint.address, type);
		}

		private void OnDebuggerInitialized()
		{
			// Default value is M64P_DBG_RUNSTATE_PAUSED
			m64pDebugSetRunState(m64p_dbg_runstate.M64P_DBG_RUNSTATE_RUNNING);
		}

		private void CompletedFrameCallback()
		{
			m64pEvent.Set();
		}
	}
}
