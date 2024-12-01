using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Jellyfish.Virtu
{
	[Flags]
	internal enum Keys : ulong
	{
		// https://archive.org/stream/Apple_IIe_Technical_Reference_Manual

		// 56 basic keys as described in the reference manual
		[Description("Delete")]
		Delete = 1UL,
		[Description("Left")]
		Left = 2UL,
		[Description("Tab")]
		Tab = 4UL,
		[Description("Down")]
		Down = 8UL,
		[Description("Up")]
		Up = 16UL,
		[Description("Return")]
		Return = 32UL,
		[Description("Right")]
		Right = 64UL,
		[Description("Escape")]
		Escape = 128UL,
		[Description("Space")]
		Space = 256UL,
		[Description("'")]
		Apostrophe = 512UL,
		[Description(",")]
		Comma = 1024UL,
		[Description("-")]
		Dash = 2048UL,
		[Description(".")]
		Period = 4096UL,
		[Description("/")]
		Slash = 8192UL,
		[Description("0")]
		Key0 = 16384UL,
		[Description("1")]
		Key1 = 32768UL,
		[Description("2")]
		Key2 = 65536UL,
		[Description("3")]
		Key3 = 131072UL,
		[Description("4")]
		Key4 = 262144UL,
		[Description("5")]
		Key5 = 524288UL,
		[Description("6")]
		Key6 = 1048576UL,
		[Description("7")]
		Key7 = 2097152UL,
		[Description("8")]
		Key8 = 4194304UL,
		[Description("9")]
		Key9 = 8388608UL,
		[Description(";")]
		Semicolon = 16777216UL,
		[Description("=")]
		Equals = 33554432UL,
		[Description("[")]
		LeftBracket = 67108864UL,
		[Description("\\")]
		Backslash = 134217728UL,
		[Description("]")]
		RightBracket = 268435456UL,
		[Description("`")]
		Backtick = 536870912UL,
		[Description("A")]
		A = 1073741824UL,
		[Description("B")]
		B = 2147483648UL,
		[Description("C")]
		C = 4294967296UL,
		[Description("D")]
		D = 8589934592UL,
		[Description("E")]
		E = 17179869184UL,
		[Description("F")]
		F = 34359738368UL,
		[Description("G")]
		G = 68719476736UL,
		[Description("H")]
		H = 137438953472UL,
		[Description("I")]
		I = 274877906944UL,
		[Description("J")]
		J = 549755813888UL,
		[Description("K")]
		K = 1099511627776UL,
		[Description("L")]
		L = 2199023255552UL,
		[Description("M")]
		M = 4398046511104UL,
		[Description("N")]
		N = 8796093022208UL,
		[Description("O")]
		O = 17592186044416UL,
		[Description("P")]
		P = 35184372088832UL,
		[Description("Q")]
		Q = 70368744177664UL,
		[Description("R")]
		R = 140737488355328UL,
		[Description("S")]
		S = 281474976710656UL,
		[Description("T")]
		T = 562949953421312UL,
		[Description("U")]
		U = 1125899906842624UL,
		[Description("V")]
		V = 2251799813685248UL,
		[Description("W")]
		W = 4503599627370496UL,
		[Description("X")]
		X = 9007199254740992UL,
		[Description("Y")]
		Y = 18014398509481984UL,
		[Description("Z")]
		Z = 36028797018963968UL,

		// three modifier keys, cannot be read directly
		[Description("Control")]
		Control = 72057594037927936UL,
		[Description("Shift")]
		Shift = 144115188075855872UL,
		[Description("Caps Lock")]
		CapsLock = 288230376151711744UL,

		// three special keys
		[Description("White Apple")]
		WhiteApple = 576460752303423488UL, // connected to GAME1
		[Description("Black Apple")]
		BlackApple = 1152921504606846976UL, // connected to GAME2
		[Description("Reset")]
		Reset = 2305843009213693952UL,
	}

#pragma warning disable MA0104 // unlikely to conflict with System.Windows.Input.Keyboard
	public sealed class Keyboard
#pragma warning restore MA0104
	{
		static Keyboard()
		{
			for (int i = 0; i < 62; i++)
			{
				// http://stackoverflow.com/questions/2650080/how-to-get-c-sharp-enum-description-from-value
				Keys value = (Keys)(1UL << i);
				var fi = typeof(Keys).GetField(value.ToString());
				var attr = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
				string name = attr[0].Description;
				DescriptionsToKeys[name] = value;
			}
		}

		// ReSharper disable once UnusedMember.Global
		public static IEnumerable<string> GetKeyNames() => DescriptionsToKeys.Keys.ToList();

		private static readonly uint[] KeyAsciiData =
		{
			// https://archive.org/stream/Apple_IIe_Technical_Reference_Manual#page/n47/mode/2up
			// 0xNNCCSSBB    normal, control, shift both
			// keys in same order as above
			0x7f7f7f7f,
			0x08080808,
			0x09090909,
			0x0a0a0a0a,
			0x0b0b0b0b,
			0x0d0d0d0d,
			0x15151515,
			0x1b1b1b1b,
			0x20202020,
			0x27272222,
			0x2c2c3c3c,
			0x2d1f5f1f,
			0x2e2e3e3e,
			0x2f2f3f3f,
			0x30302929, // 0
			0x31312121,
			0x32004000,
			0x33332323,
			0x34342424,
			0x35352525,
			0x361e5e1e,
			0x37372626,
			0x38382a2a,
			0x39392828, // 9
			0x3b3b3a3a,
			0x3d3d2b2b,
			0x5b1b7b1b,
			0x5c1c7c1c,
			0x5d1d7d1d,
			0x60607e7e,

			0x61014101, // a
			0x62024202,
			0x63034303,
			0x64044404,
			0x65054505,
			0x66064606,
			0x67074707,
			0x68084808,
			0x69094909,
			0x6a0a4a0a,
			0x6b0b4b0b,
			0x6c0c4c0c,
			0x6d0d4d0d,
			0x6e0e4e0e,
			0x6f0f4f0f,
			0x70105010,
			0x71115111,
			0x72125212,
			0x73135313,
			0x74145414,
			0x75155515,
			0x76165616,
			0x77175717,
			0x78185818,
			0x79195919,
			0x7a1a5a1a, // z
		};

		// key: 0 - 55
		private static int KeyToAscii(int key, bool control, bool shift)
		{
			int s = control ? shift ? 0 : 16 : shift ? 8 : 24;
			return (int)(KeyAsciiData[key] >> s & 0x7f);
		}

		// ReSharper disable once InconsistentNaming
		private static readonly Dictionary<string, Keys> DescriptionsToKeys = new Dictionary<string, Keys>();

		private static Keys FromStrings(IEnumerable<string> keynames)
		{
			Keys ret = 0;
			foreach (string s in keynames)
			{
				ret |= DescriptionsToKeys[s];
			}

			return ret;
		}

#pragma warning disable CA2211 // public field
		public static bool WhiteAppleDown;
		public static bool BlackAppleDown;
#pragma warning restore CA2211

		/// <summary>
		/// Call this at 60hz with all of the currently pressed keys
		/// </summary>
		// ReSharper disable once UnusedMember.Global
		public void SetKeys(IEnumerable<string> keynames)
		{
			Keys keys = FromStrings(keynames);

			WhiteAppleDown = keys.HasFlag(Keys.WhiteApple);
			BlackAppleDown = keys.HasFlag(Keys.BlackApple);

			if (keys.HasFlag(Keys.Reset) && keys.HasFlag(Keys.Control)) { } // TODO: reset console

			bool control = keys.HasFlag(Keys.Control);
			bool shift = keys.HasFlag(Keys.Shift);

			bool caps = keys.HasFlag(Keys.CapsLock);
			if (caps && !_currentCapsLockState) // leading edge: toggle CapsLock
			{
				CapsActive = !CapsActive;
			}
			_currentCapsLockState = caps;
			shift ^= CapsActive;

			// work with only the first 56 real keys
			long k = (long)keys & 0xffffffffffffffL;

			IsAnyKeyDown = k != 0;

			if (!IsAnyKeyDown)
			{
				_currentKeyPressed = -1;
				return;
			}

			// TODO: on real hardware, multiple keys pressed in physical would cause a conflict
			// that would be somehow resolved by the scan pattern.  we don't emulate that.

			// instead, just arbitrarily choose the lowest key in our list

			// BSF
			int newKeyPressed = 0;
			while ((k & 1) == 0)
			{
				k >>= 1;
				newKeyPressed++;
			}

			if (newKeyPressed != _currentKeyPressed)
			{
				// strobe, start new repeat cycle
				Strobe = true;
				Latch = KeyToAscii(newKeyPressed, control, shift);
				_framesToRepeat = KeyRepeatStart;
			}
			else
			{
				// check for repeat
				_framesToRepeat--;
				if (_framesToRepeat == 0)
				{
					Strobe = true;
					Latch = KeyToAscii(newKeyPressed, control, shift);
					_framesToRepeat = KeyRepeatRate;
				}
			}

			_currentKeyPressed = newKeyPressed;
		}

		public void ResetStrobe()
		{
			Strobe = false;
		}

		public void Sync(IComponentSerializer ser)
		{
			ser.Sync("Latch", ref _latch);
			ser.Sync("Strobe", ref _strobe);
			ser.Sync("CapsActive", ref _capsActive);
			ser.Sync(nameof(_currentCapsLockState), ref _currentCapsLockState);
			ser.Sync(nameof(_framesToRepeat), ref _framesToRepeat);
		}

		/// <summary>
		/// true if any of the 56 basic keys are pressed
		/// </summary>
		public bool IsAnyKeyDown { get; private set; }

		/// <summary>
		/// the currently latched key; 7 bits.
		/// </summary>
		public int Latch
		{
			get => _latch;
			private set => _latch = value;
		}

		public bool Strobe
		{
			get => _strobe;
			private set => _strobe = value;
		}

		private int _latch;
		private bool _strobe;

		/// <summary>
		/// true if caps lock is active
		/// </summary>
		private bool CapsActive
		{
			get => _capsActive;
			set => _capsActive = value;
		}

		private bool _capsActive;
		private bool _currentCapsLockState;

		/// <summary>
		/// 0-55, -1 = none
		/// </summary>
		private int _currentKeyPressed;

		private int _framesToRepeat;

		private const int KeyRepeatRate = 6; // 10hz
		private const int KeyRepeatStart = 40; // ~666ms?
	}
}
