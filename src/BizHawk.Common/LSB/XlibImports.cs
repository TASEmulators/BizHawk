using System.Runtime.InteropServices;

// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace BizHawk.Common
{
	public static class XlibImports
	{
		private const string LIB = "libX11.so.6";

		[DllImport(LIB)]
		public static extern IntPtr XOpenDisplay(string? display_name);

		[DllImport(LIB)]
		public static extern int XCloseDisplay(IntPtr display);

		[DllImport(LIB)]
		public static extern void XLockDisplay(IntPtr display);

		[DllImport(LIB)]
		public static extern void XUnlockDisplay(IntPtr display);

		// helper struct for XLockDisplay/XUnlockDisplay
		// largely taken from OpenTK
		public ref struct XLock
		{
			private IntPtr _display;

			public XLock(IntPtr display)
			{
				if (display == IntPtr.Zero)
				{
					throw new InvalidOperationException("null display");
				}

				_display = display;
				XLockDisplay(display);
			}

			public void Dispose()
			{
				if (_display != IntPtr.Zero)
				{
					XUnlockDisplay(_display);
					_display = IntPtr.Zero;
				}
			}
		}

		[DllImport(LIB)]
		public static extern unsafe int XQueryKeymap(IntPtr display, byte* keys_return);

		// copied from OpenTK
		public enum Keysym : ulong
		{
			/*
			 * TTY function keys, cleverly chosen to map to ASCII, for convenience of
			 * programming, but could have been arbitrary (at the cost of lookup
			 * tables in client code).
			 */

			BackSpace                   = 0xff08,  /* Back space, back char */
			Tab                         = 0xff09,
			Linefeed                    = 0xff0a,  /* Linefeed, LF */
			Clear                       = 0xff0b,
			Return                      = 0xff0d,  /* Return, enter */
			Pause                       = 0xff13,  /* Pause, hold */
			Scroll_Lock                 = 0xff14,
			Sys_Req                     = 0xff15,
			Escape                      = 0xff1b,
			Delete                      = 0xffff,  /* Delete, rubout */



			/* International & multi-key character composition */

			Multi_key                   = 0xff20,  /* Multi-key character compose */
			Codeinput                   = 0xff37,
			SingleCandidate             = 0xff3c,
			MultipleCandidate           = 0xff3d,
			PreviousCandidate           = 0xff3e,

			/* Japanese keyboard support */

			Kanji                       = 0xff21,  /* Kanji, Kanji convert */
			Muhenkan                    = 0xff22,  /* Cancel Conversion */
			Henkan_Mode                 = 0xff23,  /* Start/Stop Conversion */
			Henkan                      = 0xff23,  /* Alias for Henkan_Mode */
			Romaji                      = 0xff24,  /* to Romaji */
			Hiragana                    = 0xff25,  /* to Hiragana */
			Katakana                    = 0xff26,  /* to Katakana */
			Hiragana_Katakana           = 0xff27,  /* Hiragana/Katakana toggle */
			Zenkaku                     = 0xff28,  /* to Zenkaku */
			Hankaku                     = 0xff29,  /* to Hankaku */
			Zenkaku_Hankaku             = 0xff2a,  /* Zenkaku/Hankaku toggle */
			Touroku                     = 0xff2b,  /* Add to Dictionary */
			Massyo                      = 0xff2c,  /* Delete from Dictionary */
			Kana_Lock                   = 0xff2d,  /* Kana Lock */
			Kana_Shift                  = 0xff2e,  /* Kana Shift */
			Eisu_Shift                  = 0xff2f,  /* Alphanumeric Shift */
			Eisu_toggle                 = 0xff30,  /* Alphanumeric toggle */
			Kanji_Bangou                = 0xff37,  /* Codeinput */
			Zen_Koho                    = 0xff3d,  /* Multiple/All Candidate(s) */
			Mae_Koho                    = 0xff3e,  /* Previous Candidate */

			/* 0xff31 thru 0xff3f are under XK_KOREAN */

			/* Cursor control & motion */

			Home                        = 0xff50,
			Left                        = 0xff51,  /* Move left, left arrow */
			Up                          = 0xff52,  /* Move up, up arrow */
			Right                       = 0xff53,  /* Move right, right arrow */
			Down                        = 0xff54,  /* Move down, down arrow */
			Prior                       = 0xff55,  /* Prior, previous */
			Page_Up                     = 0xff55,
			Next                        = 0xff56,  /* Next */
			Page_Down                   = 0xff56,
			End                         = 0xff57,  /* EOL */
			Begin                       = 0xff58,  /* BOL */


			/* Misc functions */

			Select                      = 0xff60,  /* Select, mark */
			Print                       = 0xff61,
			Execute                     = 0xff62,  /* Execute, run, do */
			Insert                      = 0xff63,  /* Insert, insert here */
			Undo                        = 0xff65,
			Redo                        = 0xff66,  /* Redo, again */
			Menu                        = 0xff67,
			Find                        = 0xff68,  /* Find, search */
			Cancel                      = 0xff69,  /* Cancel, stop, abort, exit */
			Help                        = 0xff6a,  /* Help */
			Break                       = 0xff6b,
			Mode_switch                 = 0xff7e,  /* Character set switch */
			script_switch               = 0xff7e,  /* Alias for mode_switch */
			Num_Lock                    = 0xff7f,

			/* Keypad functions, keypad numbers cleverly chosen to map to ASCII */

			KP_Space                    = 0xff80,  /* Space */
			KP_Tab                      = 0xff89,
			KP_Enter                    = 0xff8d,  /* Enter */
			KP_F1                       = 0xff91,  /* PF1, KP_A, ... */
			KP_F2                       = 0xff92,
			KP_F3                       = 0xff93,
			KP_F4                       = 0xff94,
			KP_Home                     = 0xff95,
			KP_Left                     = 0xff96,
			KP_Up                       = 0xff97,
			KP_Right                    = 0xff98,
			KP_Down                     = 0xff99,
			KP_Prior                    = 0xff9a,
			KP_Page_Up                  = 0xff9a,
			KP_Next                     = 0xff9b,
			KP_Page_Down                = 0xff9b,
			KP_End                      = 0xff9c,
			KP_Begin                    = 0xff9d,
			KP_Insert                   = 0xff9e,
			KP_Delete                   = 0xff9f,
			KP_Equal                    = 0xffbd,  /* Equals */
			KP_Multiply                 = 0xffaa,
			KP_Add                      = 0xffab,
			KP_Separator                = 0xffac,  /* Separator, often comma */
			KP_Subtract                 = 0xffad,
			KP_Decimal                  = 0xffae,
			KP_Divide                   = 0xffaf,

			KP_0                        = 0xffb0,
			KP_1                        = 0xffb1,
			KP_2                        = 0xffb2,
			KP_3                        = 0xffb3,
			KP_4                        = 0xffb4,
			KP_5                        = 0xffb5,
			KP_6                        = 0xffb6,
			KP_7                        = 0xffb7,
			KP_8                        = 0xffb8,
			KP_9                        = 0xffb9,

			/*
			 * Auxiliary functions; note the duplicate definitions for left and right
			 * function keys;  Sun keyboards and a few other manufacturers have such
			 * function key groups on the left and/or right sides of the keyboard.
			 * We've not found a keyboard with more than 35 function keys total.
			 */

			F1                          = 0xffbe,
			F2                          = 0xffbf,
			F3                          = 0xffc0,
			F4                          = 0xffc1,
			F5                          = 0xffc2,
			F6                          = 0xffc3,
			F7                          = 0xffc4,
			F8                          = 0xffc5,
			F9                          = 0xffc6,
			F10                         = 0xffc7,
			F11                         = 0xffc8,
			L1                          = 0xffc8,
			F12                         = 0xffc9,
			L2                          = 0xffc9,
			F13                         = 0xffca,
			L3                          = 0xffca,
			F14                         = 0xffcb,
			L4                          = 0xffcb,
			F15                         = 0xffcc,
			L5                          = 0xffcc,
			F16                         = 0xffcd,
			L6                          = 0xffcd,
			F17                         = 0xffce,
			L7                          = 0xffce,
			F18                         = 0xffcf,
			L8                          = 0xffcf,
			F19                         = 0xffd0,
			L9                          = 0xffd0,
			F20                         = 0xffd1,
			L10                         = 0xffd1,
			F21                         = 0xffd2,
			R1                          = 0xffd2,
			F22                         = 0xffd3,
			R2                          = 0xffd3,
			F23                         = 0xffd4,
			R3                          = 0xffd4,
			F24                         = 0xffd5,
			R4                          = 0xffd5,
			F25                         = 0xffd6,
			R5                          = 0xffd6,
			F26                         = 0xffd7,
			R6                          = 0xffd7,
			F27                         = 0xffd8,
			R7                          = 0xffd8,
			F28                         = 0xffd9,
			R8                          = 0xffd9,
			F29                         = 0xffda,
			R9                          = 0xffda,
			F30                         = 0xffdb,
			R10                         = 0xffdb,
			F31                         = 0xffdc,
			R11                         = 0xffdc,
			F32                         = 0xffdd,
			R12                         = 0xffdd,
			F33                         = 0xffde,
			R13                         = 0xffde,
			F34                         = 0xffdf,
			R14                         = 0xffdf,
			F35                         = 0xffe0,
			R15                         = 0xffe0,

			/* Modifiers */

			Shift_L                     = 0xffe1,  /* Left shift */
			Shift_R                     = 0xffe2,  /* Right shift */
			Control_L                   = 0xffe3,  /* Left control */
			Control_R                   = 0xffe4,  /* Right control */
			Caps_Lock                   = 0xffe5,  /* Caps lock */
			Shift_Lock                  = 0xffe6,  /* Shift lock */

			Meta_L                      = 0xffe7,  /* Left meta */
			Meta_R                      = 0xffe8,  /* Right meta */
			Alt_L                       = 0xffe9,  /* Left alt */
			Alt_R                       = 0xffea,  /* Right alt */
			Super_L                     = 0xffeb,  /* Left super */
			Super_R                     = 0xffec,  /* Right super */
			Hyper_L                     = 0xffed,  /* Left hyper */
			Hyper_R                     = 0xffee,  /* Right hyper */

			ISO_Level3_Shift = 0xfe03,

			/*
			 * Latin 1
			 * (ISO/IEC 8859-1 = Unicode U+0020..U+00FF)
			 * Byte 3 = 0
			 */

			space                       = 0x0020,  /* U+0020 SPACE */
			exclam                      = 0x0021,  /* U+0021 EXCLAMATION MARK */
			quotedbl                    = 0x0022,  /* U+0022 QUOTATION MARK */
			numbersign                  = 0x0023,  /* U+0023 NUMBER SIGN */
			dollar                      = 0x0024,  /* U+0024 DOLLAR SIGN */
			percent                     = 0x0025,  /* U+0025 PERCENT SIGN */
			ampersand                   = 0x0026,  /* U+0026 AMPERSAND */
			apostrophe                  = 0x0027,  /* U+0027 APOSTROPHE */
			quoteright                  = 0x0027,  /* deprecated */
			parenleft                   = 0x0028,  /* U+0028 LEFT PARENTHESIS */
			parenright                  = 0x0029,  /* U+0029 RIGHT PARENTHESIS */
			asterisk                    = 0x002a,  /* U+002A ASTERISK */
			plus                        = 0x002b,  /* U+002B PLUS SIGN */
			comma                       = 0x002c,  /* U+002C COMMA */
			minus                       = 0x002d,  /* U+002D HYPHEN-MINUS */
			period                      = 0x002e,  /* U+002E FULL STOP */
			slash                       = 0x002f,  /* U+002F SOLIDUS */
			Number0                     = 0x0030,  /* U+0030 DIGIT ZERO */
			Number1                     = 0x0031,  /* U+0031 DIGIT ONE */
			Number2                     = 0x0032,  /* U+0032 DIGIT TWO */
			Number3                     = 0x0033,  /* U+0033 DIGIT THREE */
			Number4                     = 0x0034,  /* U+0034 DIGIT FOUR */
			Number5                     = 0x0035,  /* U+0035 DIGIT FIVE */
			Number6                     = 0x0036,  /* U+0036 DIGIT SIX */
			Number7                     = 0x0037,  /* U+0037 DIGIT SEVEN */
			Number8                     = 0x0038,  /* U+0038 DIGIT EIGHT */
			Number9                     = 0x0039,  /* U+0039 DIGIT NINE */
			colon                       = 0x003a,  /* U+003A COLON */
			semicolon                   = 0x003b,  /* U+003B SEMICOLON */
			less                        = 0x003c,  /* U+003C LESS-THAN SIGN */
			equal                       = 0x003d,  /* U+003D EQUALS SIGN */
			greater                     = 0x003e,  /* U+003E GREATER-THAN SIGN */
			question                    = 0x003f,  /* U+003F QUESTION MARK */
			at                          = 0x0040,  /* U+0040 COMMERCIAL AT */
			A                           = 0x0041,  /* U+0041 LATIN CAPITAL LETTER A */
			B                           = 0x0042,  /* U+0042 LATIN CAPITAL LETTER B */
			C                           = 0x0043,  /* U+0043 LATIN CAPITAL LETTER C */
			D                           = 0x0044,  /* U+0044 LATIN CAPITAL LETTER D */
			E                           = 0x0045,  /* U+0045 LATIN CAPITAL LETTER E */
			F                           = 0x0046,  /* U+0046 LATIN CAPITAL LETTER F */
			G                           = 0x0047,  /* U+0047 LATIN CAPITAL LETTER G */
			H                           = 0x0048,  /* U+0048 LATIN CAPITAL LETTER H */
			I                           = 0x0049,  /* U+0049 LATIN CAPITAL LETTER I */
			J                           = 0x004a,  /* U+004A LATIN CAPITAL LETTER J */
			K                           = 0x004b,  /* U+004B LATIN CAPITAL LETTER K */
			L                           = 0x004c,  /* U+004C LATIN CAPITAL LETTER L */
			M                           = 0x004d,  /* U+004D LATIN CAPITAL LETTER M */
			N                           = 0x004e,  /* U+004E LATIN CAPITAL LETTER N */
			O                           = 0x004f,  /* U+004F LATIN CAPITAL LETTER O */
			P                           = 0x0050,  /* U+0050 LATIN CAPITAL LETTER P */
			Q                           = 0x0051,  /* U+0051 LATIN CAPITAL LETTER Q */
			R                           = 0x0052,  /* U+0052 LATIN CAPITAL LETTER R */
			S                           = 0x0053,  /* U+0053 LATIN CAPITAL LETTER S */
			T                           = 0x0054,  /* U+0054 LATIN CAPITAL LETTER T */
			U                           = 0x0055,  /* U+0055 LATIN CAPITAL LETTER U */
			V                           = 0x0056,  /* U+0056 LATIN CAPITAL LETTER V */
			W                           = 0x0057,  /* U+0057 LATIN CAPITAL LETTER W */
			X                           = 0x0058,  /* U+0058 LATIN CAPITAL LETTER X */
			Y                           = 0x0059,  /* U+0059 LATIN CAPITAL LETTER Y */
			Z                           = 0x005a,  /* U+005A LATIN CAPITAL LETTER Z */
			bracketleft                 = 0x005b,  /* U+005B LEFT SQUARE BRACKET */
			backslash                   = 0x005c,  /* U+005C REVERSE SOLIDUS */
			bracketright                = 0x005d,  /* U+005D RIGHT SQUARE BRACKET */
			asciicircum                 = 0x005e,  /* U+005E CIRCUMFLEX ACCENT */
			underscore                  = 0x005f,  /* U+005F LOW LINE */
			grave                       = 0x0060,  /* U+0060 GRAVE ACCENT */
			quoteleft                   = 0x0060,  /* deprecated */
			a                           = 0x0061,  /* U+0061 LATIN SMALL LETTER A */
			b                           = 0x0062,  /* U+0062 LATIN SMALL LETTER B */
			c                           = 0x0063,  /* U+0063 LATIN SMALL LETTER C */
			d                           = 0x0064,  /* U+0064 LATIN SMALL LETTER D */
			e                           = 0x0065,  /* U+0065 LATIN SMALL LETTER E */
			f                           = 0x0066,  /* U+0066 LATIN SMALL LETTER F */
			g                           = 0x0067,  /* U+0067 LATIN SMALL LETTER G */
			h                           = 0x0068,  /* U+0068 LATIN SMALL LETTER H */
			i                           = 0x0069,  /* U+0069 LATIN SMALL LETTER I */
			j                           = 0x006a,  /* U+006A LATIN SMALL LETTER J */
			k                           = 0x006b,  /* U+006B LATIN SMALL LETTER K */
			l                           = 0x006c,  /* U+006C LATIN SMALL LETTER L */
			m                           = 0x006d,  /* U+006D LATIN SMALL LETTER M */
			n                           = 0x006e,  /* U+006E LATIN SMALL LETTER N */
			o                           = 0x006f,  /* U+006F LATIN SMALL LETTER O */
			p                           = 0x0070,  /* U+0070 LATIN SMALL LETTER P */
			q                           = 0x0071,  /* U+0071 LATIN SMALL LETTER Q */
			r                           = 0x0072,  /* U+0072 LATIN SMALL LETTER R */
			s                           = 0x0073,  /* U+0073 LATIN SMALL LETTER S */
			t                           = 0x0074,  /* U+0074 LATIN SMALL LETTER T */
			u                           = 0x0075,  /* U+0075 LATIN SMALL LETTER U */
			v                           = 0x0076,  /* U+0076 LATIN SMALL LETTER V */
			w                           = 0x0077,  /* U+0077 LATIN SMALL LETTER W */
			x                           = 0x0078,  /* U+0078 LATIN SMALL LETTER X */
			y                           = 0x0079,  /* U+0079 LATIN SMALL LETTER Y */
			z                           = 0x007a,  /* U+007A LATIN SMALL LETTER Z */
			braceleft                   = 0x007b,  /* U+007B LEFT CURLY BRACKET */
			bar                         = 0x007c,  /* U+007C VERTICAL LINE */
			braceright                  = 0x007d,  /* U+007D RIGHT CURLY BRACKET */
			asciitilde                  = 0x007e,  /* U+007E TILDE */

			// Extra keys

			XF86AudioMute = 0x1008ff12,
			XF86AudioLowerVolume = 0x1008ff11,
			XF86AudioRaiseVolume = 0x1008ff13,
			XF86PowerOff = 0x1008ff2a,
			XF86Suspend = 0x1008ffa7,
			XF86Copy = 0x1008ff57,
			XF86Paste = 0x1008ff6d,
			XF86Cut = 0x1008ff58,
			XF86MenuKB = 0x1008ff65,
			XF86Calculator = 0x1008ff1d,
			XF86Sleep = 0x1008ff2f,
			XF86WakeUp = 0x1008ff2b,
			XF86Explorer = 0x1008ff5d,
			XF86Send = 0x1008ff7b,
			XF86Xfer = 0x1008ff8a,
			XF86Launch1 = 0x1008ff41,
			XF86Launch2 = 0x1008ff42,
			XF86Launch3 = 0x1008ff43,
			XF86Launch4 = 0x1008ff44,
			XF86Launch5 = 0x1008ff45,
			XF86LaunchA = 0x1008ff4a,
			XF86LaunchB = 0x1008ff4b,
			XF86WWW = 0x1008ff2e,
			XF86DOS = 0x1008ff5a,
			XF86ScreenSaver = 0x1008ff2d,
			XF86RotateWindows = 0x1008ff74,
			XF86Mail = 0x1008ff19,
			XF86Favorites = 0x1008ff30,
			XF86MyComputer = 0x1008ff33,
			XF86Back = 0x1008ff26,
			XF86Forward = 0x1008ff27,
			XF86Eject = 0x1008ff2c,
			XF86AudioPlay = 0x1008ff14,
			XF86AudioStop = 0x1008ff15,
			XF86AudioPrev = 0x1008ff16,
			XF86AudioNext = 0x1008ff17,
			XF86AudioRecord = 0x1008ff1c,
			XF86AudioPause = 0x1008ff31,
			XF86AudioRewind = 0x1008ff3e,
			XF86AudioForward = 0x1008ff97,
			XF86Phone = 0x1008ff6e,
			XF86Tools = 0x1008ff81,
			XF86HomePage = 0x1008ff18,
			XF86Close = 0x1008ff56,
			XF86Reload = 0x1008ff73,
			XF86ScrollUp = 0x1008ff78,
			XF86ScrollDown = 0x1008ff79,
			XF86New = 0x1008ff68,
			XF86TouchpadToggle = 0x1008ffa9,
			XF86WebCam = 0x1008ff8f,
			XF86Search = 0x1008ff1b,
			XF86Finance = 0x1008ff3c,
			XF86Shop = 0x1008ff36,
			XF86MonBrightnessDown = 0x1008ff03,
			XF86MonBrightnessUp = 0x1008ff02,
			XF86AudioMedia = 0x1008ff32,
			XF86Display = 0x1008ff59,
			XF86KbdLightOnOff = 0x1008ff04,
			XF86KbdBrightnessDown = 0x1008ff06,
			XF86KbdBrightnessUp = 0x1008ff05,
			XF86Reply = 0x1008ff72,
			XF86MailForward = 0x1008ff90,
			XF86Save = 0x1008ff77,
			XF86Documents = 0x1008ff5b,
			XF86Battery = 0x1008ff93,
			XF86Bluetooth = 0x1008ff94,
			XF86WLAN = 0x1008ff95,

			SunProps = 0x1005ff70,
			SunOpen = 0x1005ff73,
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct XKeyEvent
		{
			public int type;
			public nuint serial;
			[MarshalAs(UnmanagedType.Bool)]
			public bool send_event;
			public IntPtr display;
			public nuint window;
			public nuint root;
			public nuint subwindow;
			public nuint time;
			public int x, y;
			public int x_root, y_root;
			public int state;
			public int keycode;
			[MarshalAs(UnmanagedType.Bool)]
			public bool same_screen;
		}

		[DllImport(LIB)]
		[return: MarshalAs(UnmanagedType.SysUInt)]
		public static extern Keysym XLookupKeysym(ref XKeyEvent key_event, int index);

		[DllImport(LIB)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool XkbQueryExtension(IntPtr display, out int opcode_rtrn, out int event_rtrn, out int error_rtrn, ref int major_in_out, ref int minor_in_out);
		
		[DllImport(LIB)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool XkbSetDetectableAutoRepeat(IntPtr display, [MarshalAs(UnmanagedType.Bool)] bool detectable, [MarshalAs(UnmanagedType.Bool)] out bool supported_rtrn);

		[StructLayout(LayoutKind.Sequential)]
		public unsafe struct XkbKeyNameRec
		{
			public fixed sbyte name[4];
		}

		[StructLayout(LayoutKind.Sequential)]
		public unsafe struct XkbKeyAliasRec
		{
			public fixed sbyte real[4];
			public fixed sbyte alias[4];
		}

		[StructLayout(LayoutKind.Sequential)]
		public unsafe struct XkbNamesRec
		{
			public nuint keycodes;
			public nuint geometry;
			public nuint symbols;
			public nuint types;
			public nuint compat;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			public nuint[] vmods;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
			public nuint[] indicators;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public nuint[] groups;
			public XkbKeyNameRec* keys;
			public XkbKeyAliasRec* key_aliases;
			public nuint* radio_groups;
			public nuint phys_symbols;

			public byte num_keys;
			public byte num_key_aliases;
			public ushort num_rg;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct XkbDescRec
		{
			public IntPtr dpy;
			public ushort flags;
			public ushort device_spec;
			public byte min_key_code;
			public byte max_key_code;

			public IntPtr ctrls;
			public IntPtr server;
			public IntPtr map;
			public IntPtr indicators;
			public IntPtr names; // XkbNamesRec*
			public IntPtr compat;
			public IntPtr geom;
		}

		[DllImport(LIB)]
		public static extern unsafe XkbDescRec* XkbAllocKeyboard(IntPtr display);

		[DllImport(LIB)]
		public static extern unsafe void XkbFreeKeyboard(XkbDescRec* xkb, int which, [MarshalAs(UnmanagedType.Bool)] bool free_all);

		[DllImport(LIB)]
		public static extern unsafe int XkbGetNames(IntPtr display, uint which, XkbDescRec* xkb);

		[DllImport(LIB)]
		public static extern Keysym XkbKeycodeToKeysym(IntPtr display, int keycode, int group, int level);
	}
}
