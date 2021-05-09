namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public partial class BsnesApi
	{
		public enum eMessage
		{
			eMessage_NotSet,

			eMessage_Resume,

			eMessage_QUERY_FIRST,
			eMessage_QUERY_get_memory_size,
			eMessage_QUERY_peek,
			eMessage_QUERY_poke,
			eMessage_QUERY_serialize_size,
			eMessage_QUERY_set_color_lut,
			eMessage_QUERY_GetMemoryIdName,
			eMessage_QUERY_state_hook_exec,
			eMessage_QUERY_state_hook_read,
			eMessage_QUERY_state_hook_write,
			eMessage_QUERY_state_hook_nmi,
			eMessage_QUERY_state_hook_irq,
			eMessage_QUERY_state_hook_exec_smp,
			eMessage_QUERY_state_hook_read_smp,
			eMessage_QUERY_state_hook_write_smp,
			eMessage_QUERY_enable_trace,
			eMessage_QUERY_enable_scanline,
			eMessage_QUERY_enable_audio,
			eMessage_QUERY_set_layer_enable,
			eMessage_QUERY_set_backdropColor,
			eMessage_QUERY_peek_logical_register,
			eMessage_QUERY_peek_cpu_regs,
			eMessage_QUERY_set_cdl,
			eMessage_QUERY_LAST,

			eMessage_CMD_FIRST,
			eMessage_CMD_init,
			eMessage_CMD_power,
			eMessage_CMD_reset,
			eMessage_CMD_run,
			eMessage_CMD_serialize,
			eMessage_CMD_unserialize,
			eMessage_CMD_load_cartridge_normal,
			eMessage_CMD_load_cartridge_sgb,
			eMessage_CMD_term,
			eMessage_CMD_unload_cartridge,
			eMessage_CMD_LAST,

			eMessage_SIG_video_refresh,
			eMessage_SIG_input_poll,
			eMessage_SIG_input_state,
			eMessage_SIG_no_lag,
			eMessage_SIG_audio_flush,
			eMessage_SIG_path_request,
			eMessage_SIG_trace_callback,
			eMessage_SIG_allocSharedMemory, //?
			eMessage_SIG_freeSharedMemory, //?

			eMessage_BRK_Complete,
			eMessage_BRK_hook_exec,
			eMessage_BRK_hook_read,
			eMessage_BRK_hook_write,
			eMessage_BRK_hook_nmi,
			eMessage_BRK_hook_irq,
			eMessage_BRK_hook_exec_smp,
			eMessage_BRK_hook_read_smp,
			eMessage_BRK_hook_write_smp,
			eMessage_BRK_scanlineStart,
		}

		public enum SNES_MEMORY
		{
			CARTRIDGE_RAM,
			BSX_RAM,
			BSX_PRAM,
			SUFAMI_TURBO_A_RAM,
			SUFAMI_TURBO_B_RAM,

			WRAM,
			APURAM,
			VRAM,
			// OAM, // needs some work in the core probably? or we return an objects pointer
			CGRAM,

			CARTRIDGE_ROM
		}

		private enum eStatus
		{
			eStatus_Idle,
			eStatus_CMD,
			eStatus_BRK
		}

		public enum BSNES_INPUT_DEVICE
		{
			None,
			Gamepad,
			Mouse,
			SuperMultitap,
			SuperScope,
			Justifier,
			Justifiers,

			Satellaview,
			S21FX
		}

		public enum ENTROPY
		{
			None,
			Low,
			High
		}

		public enum SNES_MAPPER : byte
		{
			LOROM = 0,
			HIROM = 1,
			EXLOROM = 2,
			EXHIROM = 3,
			SUPERFXROM = 4,
			SA1ROM = 5,
			SPC7110ROM = 6,
			BSCLOROM = 7,
			BSCHIROM = 8,
			BSXROM = 9,
			STROM = 10
		}

		public enum SNES_REGION : uint
		{
			NTSC = 0,
			PAL = 1
		}
	}
}
