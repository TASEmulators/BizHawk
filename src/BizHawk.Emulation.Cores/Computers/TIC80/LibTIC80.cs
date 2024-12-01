using System.Runtime.InteropServices;

using BizHawk.BizInvoke;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Computers.TIC80
{
	public abstract class LibTIC80 : LibWaterboxCore
	{
		[Flags]
		public enum TIC80Gamepad : byte
		{
			Up = 0x01,
			Down = 0x02,
			Left = 0x04,
			Right = 0x08,
			A = 0x10,
			B = 0x20,
			X = 0x40,
			Y = 0x80,
		}

		public enum TIC80Keys : byte
		{
			Unknown,

			A,
			B,
			C,
			D,
			E,
			F,
			G,
			H,
			I,
			J,
			K,
			L,
			M,
			N,
			O,
			P,
			Q,
			R,
			S,
			T,
			U,
			V,
			W,
			X,
			Y,
			Z,

			_0,
			_1,
			_2,
			_3,
			_4,
			_5,
			_6,
			_7,
			_8,
			_9,

			Minus,
			Equals,
			Left_Bracket,
			Right_Bracket,
			Backslash,
			Semicolon,
			Apostrophe,
			Grave,
			Comma,
			Period,
			Slash,

			Space,
			Tab,

			Return,
			Backspace,
			Delete,
			Insert,

			Page_Up,
			Page_Down,
			Home,
			End,
			Up,
			Down,
			Left,
			Right,

			Caps_Lock,
			Control,
			Shift,
			Alt,

			Escape,
			F1,
			F2,
			F3,
			F4,
			F5,
			F6,
			F7,
			F8,
			F9,
			F10,
			F11,
			F12,
		}

		[StructLayout(LayoutKind.Sequential)]
		public new class FrameInfo : LibWaterboxCore.FrameInfo
		{
			public long Time;
			public bool Crop;
			public bool Reset;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct TIC80Inputs
		{
			public TIC80Gamepad P1Gamepad;
			public TIC80Gamepad P2Gamepad;
			public TIC80Gamepad P3Gamepad;
			public TIC80Gamepad P4Gamepad;

			public byte MouseX;
			public byte MouseY;
			public ushort MouseButtons;

			public TIC80Keys Key1;
			public TIC80Keys Key2;
			public TIC80Keys Key3;
			public TIC80Keys Key4;
		}

		[BizImport(CC)]
		public abstract bool Init(byte[] rom, int sz, bool[] inputsActive);

		[BizImport(CC)]
		public abstract void SetInputs(ref TIC80Inputs inputs);

		[BizImport(CC)]
		public abstract bool IsMouseRelative();
	}
}
