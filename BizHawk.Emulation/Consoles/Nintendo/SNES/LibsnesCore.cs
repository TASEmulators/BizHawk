//TODO 
//libsnes needs to be modified to support multiple instances - THIS IS NECESSARY - or else loading one game and then another breaks things
//rename snes.dll so nobody thinks it's a stock snes.dll (we'll be editing it substantially at some point)
//wrap dll code around some kind of library-accessing interface so that it doesnt malfunction if the dll is unavailable

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
		public static extern int snes_library_revision_major();
		[DllImport("snes.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int snes_library_revision_minor();

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
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void snes_audio_sample_t(ushort left, ushort right);

		[DllImport("snes.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_video_refresh(snes_video_refresh_t video_refresh);
		[DllImport("snes.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_input_poll(snes_input_poll_t input_poll);
		[DllImport("snes.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_input_state(snes_input_state_t input_state);
		[DllImport("snes.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_audio_sample(snes_audio_sample_t audio_sample);

		[DllImport("snes.dll", CallingConvention = CallingConvention.Cdecl)]
		[return: MarshalAs(UnmanagedType.U1)]
		public static extern bool snes_check_cartridge(
			[MarshalAs(UnmanagedType.LPArray)] byte[] rom_data,
			int rom_size);

		[DllImport("snes.dll", CallingConvention = CallingConvention.Cdecl)]
		[return: MarshalAs(UnmanagedType.U1)]
		public static extern SNES_REGION snes_get_region();

		[DllImport("snes.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int snes_get_memory_size(SNES_MEMORY id);
		[DllImport("snes.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr snes_get_memory_data(SNES_MEMORY id);

		[DllImport("snes.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int snes_serialize_size();
    
		[return: MarshalAs(UnmanagedType.U1)]
    [DllImport("snes.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool snes_serialize(IntPtr data, int size);
		
		[return: MarshalAs(UnmanagedType.U1)]
		[DllImport("snes.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool snes_unserialize(IntPtr data, int size);

		[DllImport("snes.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_layer_enable(int layer, int priority,
			[MarshalAs(UnmanagedType.U1)]
			bool enable
			);
		
		public enum SNES_MEMORY : uint
		{
			CARTRIDGE_RAM = 0,
			CARTRIDGE_RTC = 1,
			BSX_RAM = 2,
			BSX_PRAM = 3,
			SUFAMI_TURBO_A_RAM = 4,
			SUFAMI_TURBO_B_RAM = 5,
			GAME_BOY_RAM = 6,
			GAME_BOY_RTC = 7,

			WRAM = 100,
			APURAM = 101,
			VRAM = 102,
			OAM = 103,
			CGRAM = 104,
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

	public unsafe class LibsnesCore : IEmulator, IVideoProvider, ISoundProvider
	{
		public void Dispose()
		{
			LibsnesDll.snes_term();
			_gc_snes_video_refresh.Free();
			_gc_snes_input_poll.Free();
			_gc_snes_input_state.Free();
			_gc_snes_audio_sample.Free();
		}

		public LibsnesCore(byte[] romData)
		{
			LibsnesDll.snes_init();

			var vidcb = new LibsnesDll.snes_video_refresh_t(snes_video_refresh);
			_gc_snes_video_refresh = GCHandle.Alloc(vidcb);
			BizHawk.Emulation.Consoles.Nintendo.SNES.LibsnesDll.snes_set_video_refresh(vidcb);

			var pollcb = new LibsnesDll.snes_input_poll_t(snes_input_poll);
			_gc_snes_input_poll = GCHandle.Alloc(pollcb);
			BizHawk.Emulation.Consoles.Nintendo.SNES.LibsnesDll.snes_set_input_poll(pollcb);

			var inputcb = new LibsnesDll.snes_input_state_t(snes_input_state);
			_gc_snes_input_state = GCHandle.Alloc(inputcb);
			BizHawk.Emulation.Consoles.Nintendo.SNES.LibsnesDll.snes_set_input_state(inputcb);

			var soundcb = new LibsnesDll.snes_audio_sample_t(snes_audio_sample);
			_gc_snes_audio_sample = GCHandle.Alloc(soundcb);
			BizHawk.Emulation.Consoles.Nintendo.SNES.LibsnesDll.snes_set_audio_sample(soundcb);

			//strip header
			if ((romData.Length & 0x7FFF) == 512)
			{
				var newData = new byte[romData.Length - 512];
				Array.Copy(romData, 512, newData, 0, newData.Length);
				romData = newData;
			}
			LibsnesDll.snes_load_cartridge_normal(null, romData, romData.Length);

			SetupMemoryDomains(romData);
		}

		GCHandle _gc_snes_input_state;
		ushort snes_input_state(int port, int device, int index, int id)
		{
			//Console.WriteLine("{0} {1} {2} {3}", port, device, index, id);

			string key = "P" + (1 + port) + " ";
			if ((LibsnesDll.SNES_DEVICE)device == LibsnesDll.SNES_DEVICE.JOYPAD)
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
			LibsnesDll.snes_set_layer_enable(0, 0, CoreInputComm.SNES_ShowBG1_0);
			LibsnesDll.snes_set_layer_enable(0, 1, CoreInputComm.SNES_ShowBG1_1);
			LibsnesDll.snes_set_layer_enable(1, 0, CoreInputComm.SNES_ShowBG2_0);
			LibsnesDll.snes_set_layer_enable(1, 1, CoreInputComm.SNES_ShowBG2_1);
			LibsnesDll.snes_set_layer_enable(2, 0, CoreInputComm.SNES_ShowBG3_0);
			LibsnesDll.snes_set_layer_enable(2, 1, CoreInputComm.SNES_ShowBG3_1);
			LibsnesDll.snes_set_layer_enable(3, 0, CoreInputComm.SNES_ShowBG4_0);
			LibsnesDll.snes_set_layer_enable(3, 1, CoreInputComm.SNES_ShowBG4_1);
			LibsnesDll.snes_set_layer_enable(4, 0, CoreInputComm.SNES_ShowOBJ_0);
			LibsnesDll.snes_set_layer_enable(4, 1, CoreInputComm.SNES_ShowOBJ_1);
			LibsnesDll.snes_set_layer_enable(4, 2, CoreInputComm.SNES_ShowOBJ_2);
			LibsnesDll.snes_set_layer_enable(4, 3, CoreInputComm.SNES_ShowOBJ_3);

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
		public ISoundProvider SoundProvider { get { return this; } }

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
		public bool SaveRamModified
		{
			set { }
			get
			{
				return LibsnesDll.snes_get_memory_size(LibsnesDll.SNES_MEMORY.CARTRIDGE_RAM) != 0;
			}
		}
		
		public byte[] ReadSaveRam { get { return snes_get_memory_data_read(LibsnesDll.SNES_MEMORY.CARTRIDGE_RAM); } }
		public static byte[] snes_get_memory_data_read(LibsnesDll.SNES_MEMORY id)
		{
			var size = (int)LibsnesDll.snes_get_memory_size(id);
			if (size == 0) return new byte[0];
			var data = LibsnesDll.snes_get_memory_data(id);
			var ret = new byte[size];
			Marshal.Copy(data,ret,0,size);
			return ret;
		}

		public void StoreSaveRam(byte[] data)
		{
			var size = (int)LibsnesDll.snes_get_memory_size(LibsnesDll.SNES_MEMORY.CARTRIDGE_RAM);
			if (size == 0) return;
			var emudata = LibsnesDll.snes_get_memory_data(LibsnesDll.SNES_MEMORY.CARTRIDGE_RAM);
			Marshal.Copy(data, 0, emudata, size);
		}

		public void ResetFrameCounter() { }
		public void SaveStateText(TextWriter writer)
		{
			var temp = SaveStateBinary();
			temp.SaveAsHex(writer);
		}
		public void LoadStateText(TextReader reader)
		{
			string hex = reader.ReadLine();
			byte[] state = new byte[hex.Length / 2];
			state.ReadFromHex(hex);
			LoadStateBinary(new BinaryReader(new MemoryStream(state)));
		}
		
		public void SaveStateBinary(BinaryWriter writer)
		{
			int size = LibsnesDll.snes_serialize_size();
			byte[] buf = new byte[size];
			fixed (byte* pbuf = &buf[0])
				LibsnesDll.snes_serialize(new IntPtr(pbuf), size);
			writer.Write(buf);
			writer.Flush();
		}
		public void LoadStateBinary(BinaryReader reader)
		{
			int size = LibsnesDll.snes_serialize_size();
			var ms = new MemoryStream();
			reader.BaseStream.CopyTo(ms);
			var buf = ms.ToArray();
			fixed (byte* pbuf = &buf[0])
				LibsnesDll.snes_unserialize(new IntPtr(pbuf), size);
		}
		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		// Arbitrary extensible core comm mechanism
		public CoreInputComm CoreInputComm { get; set; }
		public CoreOutputComm CoreOutputComm { get { return _CoreOutputComm; } }
		CoreOutputComm _CoreOutputComm = new CoreOutputComm();

		// ----- Client Debugging API stuff -----
		unsafe MemoryDomain MakeMemoryDomain(string name, LibsnesDll.SNES_MEMORY id, Endian endian)
		{
			IntPtr block = LibsnesDll.snes_get_memory_data(LibsnesDll.SNES_MEMORY.WRAM);
			int size = LibsnesDll.snes_get_memory_size(id);
			int mask = size - 1;
			byte* blockptr = (byte*)block.ToPointer();
			MemoryDomain md;
			
			//have to bitmask these somehow because it's unmanaged memory and we would hate to clobber things or make them nondeterministic
			if (Util.IsPowerOfTwo(size))
			{
				//can &mask for speed
				md = new MemoryDomain(name, size, endian,
					 (addr) => blockptr[addr & mask],
					 (addr, value) => blockptr[addr & mask] = value);
			}
			else
			{
				//have to use % (only OAM needs this, it seems)
				md = new MemoryDomain(name, size, endian,
					 (addr) => blockptr[addr % size],
					 (addr, value) => blockptr[addr % size] = value);
			}

			MemoryDomains.Add(md);

			return md;

			//doesnt cache the addresses. safer. slower. necessary? don't know
			//return new MemoryDomain(name, size, endian,
			//  (addr) => Peek(LibsnesDll.SNES_MEMORY.WRAM, addr & mask),
			//  (addr, value) => Poke(LibsnesDll.SNES_MEMORY.WRAM, addr & mask, value));
		}

		void SetupMemoryDomains(byte[] romData)
		{
			MemoryDomains = new List<MemoryDomain>();
			
			var romDomain = new MemoryDomain("CARTROM", romData.Length, Endian.Little,
				(addr) => romData[addr],
				(addr, value) => romData[addr] = value);

			MainMemory = MakeMemoryDomain("WRAM", LibsnesDll.SNES_MEMORY.WRAM, Endian.Little);
			MemoryDomains.Add(romDomain);
			MakeMemoryDomain("CARTRAM", LibsnesDll.SNES_MEMORY.CARTRIDGE_RAM, Endian.Little);
			MakeMemoryDomain("VRAM", LibsnesDll.SNES_MEMORY.VRAM, Endian.Little);
			MakeMemoryDomain("OAM", LibsnesDll.SNES_MEMORY.OAM, Endian.Little);
			MakeMemoryDomain("CGRAM", LibsnesDll.SNES_MEMORY.CGRAM, Endian.Little);
			MakeMemoryDomain("APURAM", LibsnesDll.SNES_MEMORY.APURAM, Endian.Little);
		}
		public IList<MemoryDomain> MemoryDomains { get; private set; }
		public MemoryDomain MainMemory { get; private set; }


		Queue<short> AudioBuffer = new Queue<short>();

		GCHandle _gc_snes_audio_sample;
		void snes_audio_sample(ushort left, ushort right)
		{
			AudioBuffer.Enqueue((short)left);
			AudioBuffer.Enqueue((short)right);
		}


		/// <summary>
		/// basic linear audio resampler. sampling rate is inferred from buffer sizes
		/// </summary>
		/// <param name="input">stereo s16</param>
		/// <param name="output">stereo s16</param>
		static void LinearDownsampler(short[] input, short[] output)
		{
			// TODO - this also appears in YM2612.cs ... move to common if it's found useful

			double samplefactor = (input.Length - 2) / (double)output.Length;

			for (int i = 0; i < output.Length / 2; i++)
			{
				// exact position on input stream
				double inpos = i * samplefactor;
				// selected interpolation points and weights
				int pt0 = (int)inpos; // pt1 = pt0 + 1
				double wt1 = inpos - pt0; // wt0 = 1 - wt1
				double wt0 = 1.0 - wt1;

				output[i * 2 + 0] = (short)(input[pt0 * 2 + 0] * wt0 + input[pt0 * 2 + 2] * wt1);
				output[i * 2 + 1] = (short)(input[pt0 * 2 + 1] * wt0 + input[pt0 * 2 + 3] * wt1);
			}
		}


		public void GetSamples(short[] samples)
		{
			// resample approximately 32k->44k
			int inputcount = samples.Length * 32040 / 44100;
			inputcount /= 2;

			if (inputcount < 2) inputcount = 2;

			short[] input = new short[inputcount * 2];

			int i;

			for (i = 0; i < inputcount * 2 && AudioBuffer.Count > 0; i++)
				input[i] = AudioBuffer.Dequeue();
			for (; i < inputcount * 2; i++)
				input[i] = 0;

			LinearDownsampler(input, samples);

			// drop if too many
			if (AudioBuffer.Count > samples.Length * 3)
				AudioBuffer.Clear();
		}

		public void DiscardSamples()
		{
			AudioBuffer.Clear();
		}

		// ignore for now
		public int MaxVolume
		{
			get;
			set;
		}
	}
}