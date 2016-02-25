using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Sony.PSP
{
	public static class PPSSPPDll
	{
		const CallingConvention cc = CallingConvention.StdCall;
		const string dd = "PPSSPPBizhawk.dll";

		[UnmanagedFunctionPointer(cc)]
		public delegate void LogCB(char type, string message);

		[DllImport(dd, CallingConvention = cc)]
		public static extern bool BizInit(string fn, LogCB logcallback);

		//[DllImport(dd, CallingConvention = cc)]
		//public static extern void setvidbuff(IntPtr buff);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int BizClose();

		[DllImport(dd, CallingConvention = cc)]
		public static extern void BizAdvance(int[] vidbuff, [In]Input input);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int MixSound(short[] buff, int nsamp);

		public enum Buttons : int
		{
			/*
			A = 1, // this is what they're called in the source...
			B = 2,
			X = 4,
			Y = 8,
			LBUMPER = 16,
			RBUMPER = 32,
			START = 64,
			SELECT = 128,
			UP = 256,
			DOWN = 512,
			LEFT = 1024,
			RIGHT = 2048,
			MENU = 4096,
			BACK = 8192*/
			/*SQUARE*/ A= 0x8000,
			/*TRIANGLE*/ B= 0x1000,
			/*CIRCLE*/ X= 0x2000,
			/*CROSS*/ Y= 0x4000,
			UP = 0x0010,
			DOWN = 0x0040,
			LEFT = 0x0080,
			RIGHT = 0x0020,
			START = 0x0008,
			SELECT = 0x0001,
			/*LTRIGGER*/ LBUMPER= 0x0100,
			/*RTRIGGER*/ RBUMPER= 0x0200,
			MENU=0,
			BACK=0
		}

		[StructLayout(LayoutKind.Sequential)]
		public class Input
		{
			public Buttons CurrentButtons; // this frame
			public Buttons LastButtons; // last frame
			public Buttons DownButtons; // rising edge
			public Buttons UpButtons; // falling edge
			public float LeftStickX;
			public float LeftStickY;
			public float RightStickX;
			public float RightStickY;
			public float LeftTrigger;
			public float RightTrigger;

			public void SetButtons(Buttons newButtons)
			{
				LastButtons = CurrentButtons;
				CurrentButtons = newButtons;
				DownButtons = (LastButtons ^ CurrentButtons) & CurrentButtons;
				UpButtons = (LastButtons ^ CurrentButtons) & LastButtons;
			}
		}
	}
}
