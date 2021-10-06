using BizHawk.BizInvoke;
using BizHawk.Emulation.Cores.Waterbox;
using System;

using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	public abstract class LibMelonDS : LibWaterboxCore
	{
		[Flags]
		public enum Buttons : uint
		{
			A = 0x0001,
			B = 0x0002,
			SELECT = 0x0004,
			START = 0x0008,
			RIGHT = 0x0010,
			LEFT = 0x0020,
			UP = 0x0040,
			DOWN = 0x0080,
			R = 0x0100,
			L = 0x0200,
			X = 0x0400,
			Y = 0x0800,
			TOUCH = 0x1000,
			LIDOPEN = 0x2000,
			LIDCLOSE = 0x4000,
		}

		[Flags]
		public enum LoadFlags : uint
		{
			NONE = 0x00,
			USE_DSI = 0x01,
			USE_REAL_DS_BIOS = 0x02,
			SKIP_FIRMWARE = 0x04,
			SD_CARD_ENABLE = 0x08,
			GBA_CART_PRESENT = 0x10,
			ACCURATE_AUDIO_BITRATE = 0x20,
			FIRMWARE_OVERRIDE = 0x40,
		}

		[StructLayout(LayoutKind.Sequential)]
		public new class FrameInfo : LibWaterboxCore.FrameInfo
		{
			public long Time;
			public Buttons Keys;
			public byte TouchX;
			public byte TouchY;
			public short MicInput;
			public byte GBALightSensor;
		}

		public class FirmwareSettings
		{
			byte[] FirmwareUsername = new byte[64];
			int FirmwareLanguage;
			int FirmwareBirthdayMonth;
			int FirmwareBirthdayDay;
			int FirmwareFavouriteColour;
			byte[] FirmwareMessage = new byte[1024];
		};

		[BizImport(CC)]
		public abstract bool Init(LoadFlags flags, FirmwareSettings fwSettings);

		public delegate void FileCallback(byte[] file);

		[BizImport(CC)]
		public abstract void SetFileOpenCallback(FileCallback callback);

		[BizImport(CC)]
		public abstract void SetFileCloseCallback(FileCallback callback);

		[BizImport(CC)]
		public abstract bool PutSaveRam(byte[] data, uint len);

		[BizImport(CC)]
		public abstract void GetSaveRam();

		[BizImport(CC)]
		public abstract bool SaveRamIsDirty();

		[BizImport(CC)]
		public abstract void Reset();
	}
}
