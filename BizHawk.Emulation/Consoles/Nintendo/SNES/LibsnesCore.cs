using System;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Consoles.Nintendo.SNES
{
	public unsafe static class LibsnesDll
	{
		[DllImport("snes.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern string snes_library_id();
		[DllImport("snes.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern uint snes_library_revision_major();
		[DllImport("snes.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern uint snes_library_revision_minor();

		[DllImport("snes.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_init();
		[DllImport("snes.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_power();
		[DllImport("snes.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_run();
		[DllImport("snes.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_term();

		[DllImport("snes.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_load_cartridge_normal(
			[MarshalAs(UnmanagedType.LPStr)]
			string rom_xml, 
			[MarshalAs(UnmanagedType.LPArray)]
			byte[] rom_data, 
			int rom_size);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void snes_video_refresh_t(ushort *data, int width, int height);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void snes_input_poll_t();
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate ushort snes_input_state_t(int port, int device, int index, int id);

		[DllImport("snes.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_video_refresh(snes_video_refresh_t video_refresh);
		[DllImport("snes.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_input_poll(snes_input_poll_t input_poll);
		[DllImport("snes.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_input_state(snes_input_state_t input_state);

		public enum Device : uint
		{
			None,
			Joypad,
			Multitap,
			Mouse,
			SuperScope,
			Justifier,
			Justifiers,
			USART,
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

	//TODO - libsnes needs to be modified to support multiple instances
	//TODO - rename snes.dll so nobody thinks it's a stock snes.dll (we'll be editing it substantially at some point)
	public unsafe class LibsnesCore : IEmulator, IVideoProvider
	{
		static LibsnesCore()
		{
			LibsnesDll.snes_init();
		}

		public void Dispose()
		{
			LibsnesDll.snes_term();
			_gc_snes_video_refresh.Free();
		}

		public LibsnesCore(byte[] romData)
		{
			//strip header
			if ((romData.Length & 0x7FFF) == 512)
			{
				var newData = new byte[romData.Length - 512];
				Array.Copy(romData, 512, newData, 0, newData.Length);
				romData = newData;
			}
			LibsnesDll.snes_load_cartridge_normal(null, romData, romData.Length);
			
			var vidcb = new LibsnesDll.snes_video_refresh_t(snes_video_refresh);
			_gc_snes_video_refresh = GCHandle.Alloc(vidcb);
			BizHawk.Emulation.Consoles.Nintendo.SNES.LibsnesDll.snes_set_video_refresh(vidcb);

			var pollcb = new LibsnesDll.snes_input_poll_t(snes_input_poll);
			_gc_snes_input_poll = GCHandle.Alloc(pollcb);
			BizHawk.Emulation.Consoles.Nintendo.SNES.LibsnesDll.snes_set_input_poll(pollcb);

			var inputcb = new LibsnesDll.snes_input_state_t(snes_input_state);
			_gc_snes_input_state = GCHandle.Alloc(inputcb);
			BizHawk.Emulation.Consoles.Nintendo.SNES.LibsnesDll.snes_set_input_state(inputcb);
			
			LibsnesDll.snes_power();
		}

		GCHandle _gc_snes_input_state;
		ushort snes_input_state(int port, int device, int index, int id)
		{
			//Console.WriteLine("{0} {1} {2} {3}", port, device, index, id);

			string key = "P" + (1 + port) + " ";
			if ((LibsnesDll.Device)device == LibsnesDll.Device.Joypad)
			{
				switch((LibsnesDll.SNES_DEVICE_ID)id)
				{
					case LibsnesDll.SNES_DEVICE_ID.JOYPAD_A: key += "A"; break;
					case LibsnesDll.SNES_DEVICE_ID.JOYPAD_B: key += "B"; break;
					case LibsnesDll.SNES_DEVICE_ID.JOYPAD_X: key += "X"; break;
					case LibsnesDll.SNES_DEVICE_ID.JOYPAD_Y: key += "Y"; break;
					case LibsnesDll.SNES_DEVICE_ID.JOYPAD_UP: key += "Up"; break;
					case LibsnesDll.SNES_DEVICE_ID.JOYPAD_DOWN: key += "Down"; break;
					case LibsnesDll.SNES_DEVICE_ID.JOYPAD_LEFT: key += "Left"; break;
					case LibsnesDll.SNES_DEVICE_ID.JOYPAD_RIGHT: key += "Right"; break;
					case LibsnesDll.SNES_DEVICE_ID.JOYPAD_L: key += "L"; break;
					case LibsnesDll.SNES_DEVICE_ID.JOYPAD_R: key += "R"; break;
					case LibsnesDll.SNES_DEVICE_ID.JOYPAD_SELECT: key += "Select"; break;
					case LibsnesDll.SNES_DEVICE_ID.JOYPAD_START: key += "Start"; break;
				}

				return (ushort)(Controller[key] ? 1 : 0);
			}

			return 0;

		}

		GCHandle _gc_snes_input_poll;
		void snes_input_poll()
		{
		}

		GCHandle _gc_snes_video_refresh;
		void snes_video_refresh(ushort* data, int width, int height)
		{
			vidWidth = width;
			vidHeight = height;
			int size = vidWidth * vidHeight;
			if (vidBuffer.Length != size)
				vidBuffer = new int[size];
			for (int y = 0; y < height; y++)
				for (int x = 0; x < width; x++)
				{
					int si = y * 1024 + x;
					int di = y * vidWidth + x;
					ushort rgb = data[si];
					int r = rgb >> 10;
					int g = (rgb >> 5) & 0x1F;
					int b = (rgb) & 0x1F;
					r = r * 255 / 31;
					g = g * 255 / 31;
					b = b * 255 / 31;
					vidBuffer[di] = (int)unchecked((int)0xFF000000 | (r << 16) | (g << 8) | b);
				}
		}

		public void FrameAdvance(bool render)
		{
			//apparently this is one frame?
			LibsnesDll.snes_run();
		}

		//video provider
		int IVideoProvider.BackgroundColor { get { return 0; } }
		int[] IVideoProvider.GetVideoBuffer() { return vidBuffer; }
		int IVideoProvider.VirtualWidth { get { return vidWidth; } }
		int IVideoProvider.BufferWidth  { get { return vidWidth; } }
		int IVideoProvider.BufferHeight { get { return vidHeight; } }

		int[] vidBuffer = new int[256*256];
		int vidWidth=256, vidHeight=256;

		public IVideoProvider VideoProvider { get { return this; } }
		public ISoundProvider SoundProvider { get { return new NullSound(); } }

		public ControllerDefinition ControllerDefinition { get { return SNESController; } }
		IController controller;
		public IController Controller
		{
			get { return controller; }
			set { controller = value; }
		}

		public static readonly ControllerDefinition SNESController =
			new ControllerDefinition
			{
				Name = "SNES Controller",
				BoolButtons = {
					"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Select", "P1 Start", "P1 B", "P1 A", "P1 X", "P1 Y", "P1 L", "P1 R", "Reset",
				}
			};
		
		int timeFrameCounter;
		public int Frame { get { return timeFrameCounter; } }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; private set; }
		public string SystemId { get { return "SNES"; } }
		public bool DeterministicEmulation { get; set; }
		public byte[] SaveRam { get { return new byte[0]; } }
		public bool SaveRamModified { get; set; }

		public void ResetFrameCounter() { }
		public void SaveStateText(TextWriter writer) { }
		public void LoadStateText(TextReader reader) { }
		public void SaveStateBinary(BinaryWriter writer) { }
		public void LoadStateBinary(BinaryReader reader) { }
		public byte[] SaveStateBinary() { return new byte[0]; }

		// Arbitrary extensible core comm mechanism
		CoreInputComm IEmulator.CoreInputComm { get; set; }
		CoreOutputComm IEmulator.CoreOutputComm { get { return CoreOutputComm; } }
		CoreOutputComm CoreOutputComm = new CoreOutputComm();

		// ----- Client Debugging API stuff -----
		public IList<MemoryDomain> MemoryDomains { get { return new List<MemoryDomain>(); } }
		public MemoryDomain MainMemory { get { return new MemoryDomain(); } }
	}
}