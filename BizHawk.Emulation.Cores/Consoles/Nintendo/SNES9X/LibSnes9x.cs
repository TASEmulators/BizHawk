using BizHawk.BizInvoke;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Nintendo.SNES9X
{
	public abstract class LibSnes9x : LibWaterboxCore
	{
		public enum LeftPortDevice : uint
		{
			//None = 0, // something in the libretro spaghetti input goes wonky with None
			Joypad = 1
		}

		public enum RightPortDevice : uint
		{
			//None = 0, // something in the libretro spaghetti input goes wonky with None
			Joypad = 1,
			Multitap = 2,
			Mouse = 3,
			SuperScope = 4,
			Justifier = 5
		}

		[BizImport(CC)]
		public abstract void SetButtons(short[] buttons);
		[BizImport(CC)]
		public abstract void biz_set_sound_channels(int channels);
		[BizImport(CC)]
		public abstract void biz_set_layers(int layers);
		[BizImport(CC)]
		public abstract void biz_soft_reset();
		[BizImport(CC)]
		public abstract void biz_hard_reset();
		[BizImport(CC)]
		public abstract void biz_set_port_devices(LeftPortDevice left, RightPortDevice right);
		[BizImport(CC)]
		public abstract bool biz_load_rom(byte[] data, int size);
		[BizImport(CC)]
		public abstract bool biz_init();
		[BizImport(CC)]
		public abstract bool biz_is_ntsc();
		[BizImport(CC)]
		public abstract void biz_post_load_state();
	}
}
