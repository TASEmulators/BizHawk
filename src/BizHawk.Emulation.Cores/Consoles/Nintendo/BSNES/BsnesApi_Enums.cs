namespace BizHawk.Emulation.Cores.Nintendo.BSNES
{
	public partial class BsnesApi
	{
		public enum SNES_REGISTER {
			//$2105
			BG_MODE,
			BG3_PRIORITY,
			BG1_TILESIZE,
			BG2_TILESIZE,
			BG3_TILESIZE,
			BG4_TILESIZE,
			//$2107
			BG1_SCADDR,
			BG1_SCSIZE,
			//$2108
			BG2_SCADDR,
			BG2_SCSIZE,
			//$2109,
			BG3_SCADDR,
			BG3_SCSIZE,
			//$210A
			BG4_SCADDR,
			BG4_SCSIZE,
			//$210B
			BG1_TDADDR,
			BG2_TDADDR,
			//$210C
			BG3_TDADDR,
			BG4_TDADDR,
			//$2133 SETINI
			SETINI_MODE7_EXTBG,
			SETINI_HIRES,
			SETINI_OVERSCAN,
			SETINI_OBJ_INTERLACE,
			SETINI_SCREEN_INTERLACE,
			//$2130 CGWSEL
			CGWSEL_COLORMASK,
			CGWSEL_COLORSUBMASK,
			CGWSEL_ADDSUBMODE,
			CGWSEL_DIRECTCOLOR,
			//$2101 OBSEL
			OBSEL_NAMEBASE,
			OBSEL_NAMESEL,
			OBSEL_SIZE,
			//$2131 CGADSUB
			CGADDSUB_MODE,
			CGADDSUB_HALF,
			CGADDSUB_BG4,
			CGADDSUB_BG3,
			CGADDSUB_BG2,
			CGADDSUB_BG1,
			CGADDSUB_OBJ,
			CGADDSUB_BACKDROP,
			//$212C TM
			TM_BG1,
			TM_BG2,
			TM_BG3,
			TM_BG4,
			TM_OBJ,
			//$212D TM
			TS_BG1,
			TS_BG2,
			TS_BG3,
			TS_BG4,
			TS_OBJ,
			//Mode7 regs
			M7SEL_REPEAT,
			M7SEL_HFLIP,
			M7SEL_VFLIP,
			M7A,
			M7B,
			M7C,
			M7D,
			M7X,
			M7Y,
			//BG scroll regs
			BG1HOFS,
			BG1VOFS,
			BG2HOFS,
			BG2VOFS,
			BG3HOFS,
			BG3VOFS,
			BG4HOFS,
			BG4VOFS,
			M7HOFS,
			M7VOFS
		}

		public enum SNES_MEMORY
		{
			CARTRAM,
			CARTROM,

			BSMEMORY_ROM,
			BSMEMORY_PSRAM,
			SUFAMI_TURBO_A_RAM,
			SUFAMI_TURBO_B_RAM,
			SA1_IRAM,
			SA1_BWRAM,

			WRAM,
			APURAM,
			VRAM,
			OAM,
			CGRAM,
		}

		// This is a mirror of SameBoy's GB_direct_access_t
		public enum SGB_MEMORY
		{
			CARTROM,
			WRAM,
			CARTRAM,
			VRAM,
			HRAM,
			MMIO,
			BOOTROM,
			OAM,
			BGP,
			OBP,
			IE,
		}

		public enum BSNES_INPUT_DEVICE
		{
			None = 0,
			Gamepad = 1,
			ExtendedGamepad = 2,
			Mouse = 3,
			SuperMultitap = 4,
			Payload = 5,
			SuperScope = 6,
			Justifier = 7,
			Justifiers = 8
		}

		/// this a subset of the <see cref="BSNES_INPUT_DEVICE"/> enum with all lightgun controllers removed
		public enum BSNES_PORT1_INPUT_DEVICE
		{
			None = 0,
			Gamepad = 1,
			ExtendedGamepad = 2,
			Mouse = 3,
			SuperMultitap = 4,
			Payload = 5
		}

		public enum ENTROPY
		{
			None,
			Low,
			High
		}

		public enum SNES_REGION : uint
		{
			NTSC = 0,
			PAL = 1
		}

		public enum REGION_OVERRIDE : uint
		{
			Auto,
			NTSC,
			PAL
		}

		public enum ASPECT_RATIO_CORRECTION : uint
		{
			None,
			Auto,
			NTSC,
			PAL
		}
	}
}
