namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public partial class LibsnesApi
	{
		public enum eMessage : int
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
			eMessage_SIG_input_notify,
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

		private enum eStatus : int
		{
			eStatus_Idle,
			eStatus_CMD,
			eStatus_BRK
		}

		public enum SNES_INPUT_PORT : int
		{
			None,
			Joypad,
			Multitap,
			Mouse,
			SuperScope,
			Justifier,
			Justifiers,
			USART
		}

		public enum SNES_REG : int
		{
			//$2105
			BG_MODE = 0,
			BG3_PRIORITY = 1,
			BG1_TILESIZE = 2,
			BG2_TILESIZE = 3,
			BG3_TILESIZE = 4,
			BG4_TILESIZE = 5,
			//$2107
			BG1_SCADDR = 10,
			BG1_SCSIZE = 11,
			//$2108
			BG2_SCADDR = 12,
			BG2_SCSIZE = 13,
			//$2109
			BG3_SCADDR = 14,
			BG3_SCSIZE = 15,
			//$210A
			BG4_SCADDR = 16,
			BG4_SCSIZE = 17,
			//$210B
			BG1_TDADDR = 20,
			BG2_TDADDR = 21,
			//$210C
			BG3_TDADDR = 22,
			BG4_TDADDR = 23,
			//$2133 SETINI
			SETINI_MODE7_EXTBG = 30,
			SETINI_HIRES = 31,
			SETINI_OVERSCAN = 32,
			SETINI_OBJ_INTERLACE = 33,
			SETINI_SCREEN_INTERLACE = 34,
			//$2130 CGWSEL
			CGWSEL_COLORMASK = 40,
			CGWSEL_COLORSUBMASK = 41,
			CGWSEL_ADDSUBMODE = 42,
			CGWSEL_DIRECTCOLOR = 43,
			//$2101 OBSEL
			OBSEL_NAMEBASE = 50,
			OBSEL_NAMESEL = 51,
			OBSEL_SIZE = 52,
			//$2131 CGADSUB
			CGADSUB_MODE = 60,
			CGADSUB_HALF = 61,
			CGADSUB_BG4 = 62,
			CGADSUB_BG3 = 63,
			CGADSUB_BG2 = 64,
			CGADSUB_BG1 = 65,
			CGADSUB_OBJ = 66,
			CGADSUB_BACKDROP = 67,
			//$212C TM
			TM_BG1 = 70,
			TM_BG2 = 71,
			TM_BG3 = 72,
			TM_BG4 = 73,
			TM_OBJ = 74,
			//$212D TM
			TS_BG1 = 80,
			TS_BG2 = 81,
			TS_BG3 = 82,
			TS_BG4 = 83,
			TS_OBJ = 84,
			//Mode7 regs
			M7SEL_REPEAT = 90,
			M7SEL_HFLIP = 91,
			M7SEL_VFLIP = 92,
			M7A = 93,
			M7B = 94,
			M7C = 95,
			M7D = 96,
			M7X = 97,
			M7Y = 98,
			//BG scroll regs
			BG1HOFS = 100,
			BG1VOFS = 101,
			BG2HOFS = 102,
			BG2VOFS = 103,
			BG3HOFS = 104,
			BG3VOFS = 105,
			BG4HOFS = 106,
			BG4VOFS = 107,
			M7HOFS = 108,
			M7VOFS = 109,
		}

		public enum SNES_MEMORY : uint
		{
			CARTRIDGE_RAM = 0,
			CARTRIDGE_RTC = 1,
			BSX_RAM = 2,
			BSX_PRAM = 3,
			SUFAMI_TURBO_A_RAM = 4,
			SUFAMI_TURBO_B_RAM = 5,
			SGB_CARTRAM = 6,
			SGB_RTC = 7,
			SGB_WRAM = 8,
			SGB_HRAM = 9,
			SA1_IRAM = 10,

			WRAM = 100,
			APURAM = 101,
			VRAM = 102,
			OAM = 103,
			CGRAM = 104,

			CARTRIDGE_ROM = 105,

			SYSBUS = 200,
			LOGICAL_REGS = 201
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
			PAL = 1,
		}

		public enum SNES_DEVICE : uint
		{
			NONE = 0,
			JOYPAD = 1,
			MULTITAP = 2,
			MOUSE = 3,
			SUPER_SCOPE = 4,
			JUSTIFIER = 5,
			JUSTIFIERS = 6,
			SERIAL_CABLE = 7
		}

		public enum SNES_DEVICE_ID : uint
		{
			JOYPAD_B = 0,
			JOYPAD_Y = 1,
			JOYPAD_SELECT = 2,
			JOYPAD_START = 3,
			JOYPAD_UP = 4,
			JOYPAD_DOWN = 5,
			JOYPAD_LEFT = 6,
			JOYPAD_RIGHT = 7,
			JOYPAD_A = 8,
			JOYPAD_X = 9,
			JOYPAD_L = 10,
			JOYPAD_R = 11
		}
	}
}
