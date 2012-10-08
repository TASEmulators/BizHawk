//TODO - add serializer, add interlace field variable to serializer

//http://wiki.superfamicom.org/snes/show/Backgrounds

//TODO 
//libsnes needs to be modified to support multiple instances - THIS IS NECESSARY - or else loading one game and then another breaks things
// edit - this is a lot of work
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
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern string snes_library_id();
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int snes_library_revision_major();
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int snes_library_revision_minor();

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_init();
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_power();
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_reset();
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_run();
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_term();
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_unload_cartridge();

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_cartridge_basename(string basename);

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		[return: MarshalAs(UnmanagedType.U1)]
		public static extern bool snes_load_cartridge_normal(
			[MarshalAs(UnmanagedType.LPStr)]
			string rom_xml,
			[MarshalAs(UnmanagedType.LPArray)]
			byte[] rom_data,
			uint rom_size);

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		[return: MarshalAs(UnmanagedType.U1)]
		public static extern bool snes_load_cartridge_super_game_boy(
			[MarshalAs(UnmanagedType.LPStr)]
			string rom_xml,
			[MarshalAs(UnmanagedType.LPArray)]
			byte[] rom_data,
			uint rom_size,
			[MarshalAs(UnmanagedType.LPStr)]
			string dmg_xml,
			[MarshalAs(UnmanagedType.LPArray)]
			byte[] dmg_data,
			uint dmg_size);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void snes_video_refresh_t(int* data, int width, int height);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void snes_input_poll_t();
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate ushort snes_input_state_t(int port, int device, int index, int id);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void snes_input_notify_t(int index);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void snes_audio_sample_t(ushort left, ushort right);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void snes_scanlineStart_t(int line);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate string snes_path_request_t(int slot, string hint);

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_video_refresh(snes_video_refresh_t video_refresh);
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_input_poll(snes_input_poll_t input_poll);
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_input_state(snes_input_state_t input_state);
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_input_notify(snes_input_notify_t input_notify);
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_audio_sample(snes_audio_sample_t audio_sample);
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_scanlineStart(snes_scanlineStart_t scanlineStart);
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_path_request(snes_path_request_t scanlineStart);

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		[return: MarshalAs(UnmanagedType.U1)]
		public static extern bool snes_check_cartridge(
			[MarshalAs(UnmanagedType.LPArray)] byte[] rom_data,
			int rom_size);

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		[return: MarshalAs(UnmanagedType.U1)]
		public static extern SNES_REGION snes_get_region();

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int snes_get_memory_size(SNES_MEMORY id);
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr snes_get_memory_data(SNES_MEMORY id);

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern byte bus_read(uint addr);
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void bus_write(uint addr, byte val);

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int snes_serialize_size();

		[return: MarshalAs(UnmanagedType.U1)]
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool snes_serialize(IntPtr data, int size);

		[return: MarshalAs(UnmanagedType.U1)]
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool snes_unserialize(IntPtr data, int size);

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int snes_poll_message();

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_dequeue_message(IntPtr strBuffer);

		public static bool HasMessage { get { return snes_poll_message() != -1; } }

		public static string DequeueMessage()
		{
			int len = snes_poll_message();
			sbyte* temp = stackalloc sbyte[len + 1];
			temp[len] = 0;
			snes_dequeue_message(new IntPtr(temp));
			return new string(temp);
		}

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_layer_enable(int layer, int priority,
			[MarshalAs(UnmanagedType.U1)]
			bool enable
			);

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int snes_peek_logical_register(SNES_REG reg);

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
		}

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

		public enum SNES_REGION : byte
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

	public class ScanlineHookManager
	{
		public void Register(object tag, Action<int> callback)
		{
			var rr = new RegistrationRecord();
			rr.tag = tag;
			rr.callback = callback;

			Unregister(tag);
			records.Add(rr);
			OnHooksChanged();
		}

		public int HookCount { get { return records.Count; } }

		public virtual void OnHooksChanged() { }

		public void Unregister(object tag)
		{
			records.RemoveAll((r) => r.tag == tag);
		}

		public void HandleScanline(int scanline)
		{
			foreach (var rr in records) rr.callback(scanline);
		}

		List<RegistrationRecord> records = new List<RegistrationRecord>();

		class RegistrationRecord
		{
			public object tag;
			public int scanline;
			public Action<int> callback;
		}
	}

	public unsafe class LibsnesCore : IEmulator, IVideoProvider, ISoundProvider
	{
		public bool IsSGB { get; private set; }

		/// <summary>disable all external callbacks.  the front end should not even know the core is frame advancing</summary>
		bool nocallbacks = false;

		bool disposed = false;
		public void Dispose()
		{
			if (disposed) return;
			disposed = true;

			disposedSaveRam = ReadSaveRam();

			BizHawk.Emulation.Consoles.Nintendo.SNES.LibsnesDll.snes_set_video_refresh(null);
			BizHawk.Emulation.Consoles.Nintendo.SNES.LibsnesDll.snes_set_input_poll(null);
			BizHawk.Emulation.Consoles.Nintendo.SNES.LibsnesDll.snes_set_input_state(null);
			BizHawk.Emulation.Consoles.Nintendo.SNES.LibsnesDll.snes_set_audio_sample(null);
			BizHawk.Emulation.Consoles.Nintendo.SNES.LibsnesDll.snes_set_scanlineStart(null);

			LibsnesDll.snes_unload_cartridge();
			LibsnesDll.snes_term();

			resampler.Dispose();
		}
		//save the save memory before disposing the core, so we can pull from it in the future after the core is terminated
		//that will be necessary to get it saving to disk
		byte[] disposedSaveRam;

		//we can only have one active snes core at a time, due to libsnes being so static.
		//so we'll track the current one here and detach the previous one whenever a new one is booted up.
		static LibsnesCore CurrLibsnesCore;

		public class MyScanlineHookManager : ScanlineHookManager
		{
			public MyScanlineHookManager(LibsnesCore core)
			{
				this.core = core;
			}
			LibsnesCore core;

			public override void OnHooksChanged()
			{
				core.OnScanlineHooksChanged();
			}
		}
		public MyScanlineHookManager ScanlineHookManager;
		void OnScanlineHooksChanged()
		{
			if (disposed) return;
			if (ScanlineHookManager.HookCount == 0) LibsnesDll.snes_set_scanlineStart(null);
			else LibsnesDll.snes_set_scanlineStart(scanlineStart_cb);
		}

		void snes_scanlineStart(int line)
		{
			ScanlineHookManager.HandleScanline(line);
		}

		string snes_path_request(int slot, string hint)
		{
			//every rom requests this byuu homemade rom
			if (hint == "msu1.rom") return "";

			//build romfilename
			string test = Path.Combine(CoreInputComm.SNES_FirmwaresPath ?? "", hint);

			//does it exist?
			if (!File.Exists(test))
			{
				System.Windows.Forms.MessageBox.Show("The SNES core is referencing a firmware file which could not be found. Please make sure it's in your configured SNES firmwares folder. The referenced filename is: " + hint);
				return "";
			}

			//return the path we built
			return test;
		}

		public LibsnesCore()
		{
		}

		public void Load(GameInfo game, byte[] romData, byte[] sgbRomData, bool DeterministicEmulation)
		{
			//attach this core as the current
			if (CurrLibsnesCore != null)
				CurrLibsnesCore.Dispose();
			CurrLibsnesCore = this;

			ScanlineHookManager = new MyScanlineHookManager(this);

			LibsnesDll.snes_init();

			//LibsnesDll.snes_set_cartridge_basename(@);

			vidcb = new LibsnesDll.snes_video_refresh_t(snes_video_refresh);
			BizHawk.Emulation.Consoles.Nintendo.SNES.LibsnesDll.snes_set_video_refresh(vidcb);

			pollcb = new LibsnesDll.snes_input_poll_t(snes_input_poll);
			BizHawk.Emulation.Consoles.Nintendo.SNES.LibsnesDll.snes_set_input_poll(pollcb);

			inputcb = new LibsnesDll.snes_input_state_t(snes_input_state);
			BizHawk.Emulation.Consoles.Nintendo.SNES.LibsnesDll.snes_set_input_state(inputcb);

			notifycb = new LibsnesDll.snes_input_notify_t(snes_input_notify);
			BizHawk.Emulation.Consoles.Nintendo.SNES.LibsnesDll.snes_set_input_notify(notifycb);

			soundcb = new LibsnesDll.snes_audio_sample_t(snes_audio_sample);
			BizHawk.Emulation.Consoles.Nintendo.SNES.LibsnesDll.snes_set_audio_sample(soundcb);

			pathRequest_cb = new LibsnesDll.snes_path_request_t(snes_path_request);
			BizHawk.Emulation.Consoles.Nintendo.SNES.LibsnesDll.snes_set_path_request(pathRequest_cb);


			scanlineStart_cb = new LibsnesDll.snes_scanlineStart_t(snes_scanlineStart);

			// start up audio resampler
			InitAudio();

			//strip header
			if ((romData.Length & 0x7FFF) == 512)
			{
				var newData = new byte[romData.Length - 512];
				Array.Copy(romData, 512, newData, 0, newData.Length);
				romData = newData;
			}

			if (game["SGB"])
			{
				IsSGB = true;
				SystemId = "SNES";
				if (!LibsnesDll.snes_load_cartridge_super_game_boy(null, sgbRomData, (uint)sgbRomData.Length, null, romData, (uint)romData.Length))
					throw new Exception("snes_load_cartridge_super_game_boy() failed");
			}
			else
			{
				SystemId = "SNES";
				if (!LibsnesDll.snes_load_cartridge_normal(null, romData, (uint)romData.Length))
					throw new Exception("snes_load_cartridge_normal() failed");
			}

			if (LibsnesDll.snes_get_region() == LibsnesDll.SNES_REGION.NTSC)
			{
				//similar to what aviout reports from snes9x and seems logical from bsnes first principles. bsnes uses that numerator (ntsc master clockrate) for sure.
				CoreOutputComm.VsyncNum = 21477272;
				CoreOutputComm.VsyncDen = 4 * 341 * 262;
			}
			else
			{
				CoreOutputComm.VsyncNum = 50;
				CoreOutputComm.VsyncDen = 1;
			}

			LibsnesDll.snes_power();

			SetupMemoryDomains(romData);

			this.DeterministicEmulation = DeterministicEmulation;
			if (DeterministicEmulation) // save frame-0 savestate now
			{
				MemoryStream ms = new MemoryStream();
				BinaryWriter bw = new BinaryWriter(ms);
				bw.Write(CoreSaveState());
				bw.Write(true); // framezero, so no controller follows and don't frameadvance on load
				// hack: write fake dummy controller info
				bw.Write(new byte[536]);
				bw.Close();
				savestatebuff = ms.ToArray();
			}
		}

		//must keep references to these so that they wont get garbage collected
		LibsnesDll.snes_video_refresh_t vidcb;
		LibsnesDll.snes_input_poll_t pollcb;
		LibsnesDll.snes_input_state_t inputcb;
		LibsnesDll.snes_input_notify_t notifycb;
		LibsnesDll.snes_audio_sample_t soundcb;
		LibsnesDll.snes_scanlineStart_t scanlineStart_cb;
		LibsnesDll.snes_path_request_t pathRequest_cb;

		ushort snes_input_state(int port, int device, int index, int id)
		{
			if (!nocallbacks && CoreInputComm.InputCallback != null) CoreInputComm.InputCallback();
			//Console.WriteLine("{0} {1} {2} {3}", port, device, index, id);

			string key = "P" + (1 + port) + " ";
			if ((LibsnesDll.SNES_DEVICE)device == LibsnesDll.SNES_DEVICE.JOYPAD)
			{
				switch ((LibsnesDll.SNES_DEVICE_ID)id)
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
					default: return 0;
				}

				return (ushort)(Controller[key] ? 1 : 0);
			}

			return 0;

		}

		void snes_input_poll()
		{
			// this doesn't actually correspond to anything in the underlying bsnes;
			// it gets called once per frame with video_refresh() and has nothing to do with anything
		}

		void snes_input_notify(int index)
		{
			IsLagFrame = false;
		}

		int field = 0;
		void snes_video_refresh(int* data, int width, int height)
		{
			vidWidth = width;
			vidHeight = height;

			//if we are in high-res mode, we get double width. so, lets double the height here to keep it square.
			//TODO - does interlacing have something to do with the correct way to handle this? need an example that turns it on.
			int yskip = 1;
			if (width == 512)
			{
				vidHeight *= 2;
				yskip = 2;
			}

			int srcPitch = 1024;
			int srcStart = 0;

			//for interlaced mode, we're gonna alternate fields. you know, like we're supposed to
			bool interlaced = (height == 478 || height == 448);
			if (interlaced)
			{
				srcPitch = 1024;
				if (field == 1)
					srcStart = 512; //start on second field
				//really only half as high as the video output
				vidHeight /= 2;
				height /= 2;
				//alternate fields
				field ^= 1;
			}

			int size = vidWidth * vidHeight;
			if (vidBuffer.Length != size)
				vidBuffer = new int[size];


			for (int y = 0; y < height; y++)
				for (int x = 0; x < width; x++)
				{
					int si = y * srcPitch + x + srcStart;
					int di = y * vidWidth * yskip + x;
					int rgb = data[si];
					vidBuffer[di] = rgb;
				}

			//alternate scanlines
			if (width == 512)
				for (int y = 0; y < height; y++)
					for (int x = 0; x < width; x++)
					{
						int si = y * 1024 + x;
						int di = y * vidWidth * yskip + x + 512;
						int rgb = data[si];
						vidBuffer[di] = rgb;
					}
		}

		public void FrameAdvance(bool render, bool rendersound)
		{
			// for deterministic emulation, save the state we're going to use before frame advance
			// don't do this during nocallbacks though, since it's already been done
			if (!nocallbacks && DeterministicEmulation)
			{
				MemoryStream ms = new MemoryStream();
				BinaryWriter bw = new BinaryWriter(ms);
				bw.Write(CoreSaveState());
				bw.Write(false); // not framezero
				SnesSaveController ssc = new SnesSaveController();
				ssc.CopyFrom(Controller);
				ssc.Serialize(bw);
				bw.Close();
				savestatebuff = ms.ToArray();
			}

			// speedup when sound rendering is not needed
			if (!rendersound)
				LibsnesDll.snes_set_audio_sample(null);
			else
				LibsnesDll.snes_set_audio_sample(soundcb);

			bool resetSignal = Controller["Reset"];
			if (resetSignal) LibsnesDll.snes_reset();

			bool powerSignal = Controller["Power"];
			if (powerSignal) LibsnesDll.snes_power();

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

			// if the input poll callback is called, it will set this to false
			IsLagFrame = true;

			//apparently this is one frame?
			timeFrameCounter++;
			LibsnesDll.snes_run();

			while (LibsnesDll.HasMessage)
				Console.WriteLine(LibsnesDll.DequeueMessage());

			if (IsLagFrame)
				LagCount++;

		}

		public DisplayType DisplayType
		{
			get
			{
				if (LibsnesDll.snes_get_region() == LibsnesDll.SNES_REGION.NTSC)
					return BizHawk.DisplayType.NTSC;
				else
					return BizHawk.DisplayType.PAL;
			}
		}

		//video provider
		int IVideoProvider.BackgroundColor { get { return 0; } }
		int[] IVideoProvider.GetVideoBuffer() { return vidBuffer; }
		int IVideoProvider.VirtualWidth { get { return vidWidth; } }
		int IVideoProvider.BufferWidth { get { return vidWidth; } }
		int IVideoProvider.BufferHeight { get { return vidHeight; } }

		int[] vidBuffer = new int[256 * 224];
		int vidWidth = 256, vidHeight = 224;

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
					"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Select", "P1 Start", "P1 B", "P1 A", "P1 X", "P1 Y", "P1 L", "P1 R", "Reset", "Power",
					"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Select", "P2 Start", "P2 B", "P2 A", "P2 X", "P2 Y", "P2 L", "P2 R",
					"P3 Up", "P3 Down", "P3 Left", "P3 Right", "P3 Select", "P3 Start", "P3 B", "P3 A", "P3 X", "P3 Y", "P3 L", "P3 R",
					"P4 Up", "P4 Down", "P4 Left", "P4 Right", "P4 Select", "P4 Start", "P4 B", "P4 A", "P4 X", "P4 Y", "P4 L", "P4 R",
				}
			};

		int timeFrameCounter;
		public int Frame { get { return timeFrameCounter; } set { timeFrameCounter = value; } }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; private set; }
		public string SystemId { get; private set; }

		public bool DeterministicEmulation
		{
			get;
			private set;
		}


		public bool SaveRamModified
		{
			set { }
			get
			{
				return LibsnesDll.snes_get_memory_size(LibsnesDll.SNES_MEMORY.CARTRIDGE_RAM) != 0;
			}
		}

		public byte[] ReadSaveRam()
		{
			if (disposedSaveRam != null) return disposedSaveRam;
			return snes_get_memory_data_read(LibsnesDll.SNES_MEMORY.CARTRIDGE_RAM);
		}

		public static byte[] snes_get_memory_data_read(LibsnesDll.SNES_MEMORY id)
		{
			var size = (int)LibsnesDll.snes_get_memory_size(id);
			if (size == 0) return new byte[0];
			var data = LibsnesDll.snes_get_memory_data(id);
			var ret = new byte[size];
			Marshal.Copy(data, ret, 0, size);
			return ret;
		}

		public void StoreSaveRam(byte[] data)
		{
			var size = (int)LibsnesDll.snes_get_memory_size(LibsnesDll.SNES_MEMORY.CARTRIDGE_RAM);
			if (size == 0) return;
			var emudata = LibsnesDll.snes_get_memory_data(LibsnesDll.SNES_MEMORY.CARTRIDGE_RAM);
			Marshal.Copy(data, 0, emudata, size);
		}

		public void ClearSaveRam()
		{
			byte[] cleardata = new byte[(int)LibsnesDll.snes_get_memory_size(LibsnesDll.SNES_MEMORY.CARTRIDGE_RAM)];
			StoreSaveRam(cleardata);
		}

		public void ResetFrameCounter() { timeFrameCounter = 0; }

		#region savestates

		/// <summary>
		/// can freeze a copy of a controller input set and serialize\deserialize it
		/// </summary>
		class SnesSaveController : IController
		{
			// this is all rather general, so perhaps should be moved out of LibsnesCore

			ControllerDefinition def;

			public SnesSaveController()
			{
				this.def = null;
			}

			public SnesSaveController(ControllerDefinition def)
			{
				this.def = def;
			}

			WorkingDictionary<string, float> buttons = new WorkingDictionary<string,float>();

			/// <summary>
			/// invalid until CopyFrom has been called
			/// </summary>
			public ControllerDefinition Type
			{
				get { return def; }
			}

			public void Serialize(BinaryWriter b)
			{
				b.Write(buttons.Keys.Count);
				foreach (var k in buttons.Keys)
				{
					b.Write(k);
					b.Write(buttons[k]);
				}
			}

			/// <summary>
			/// no checking to see if the deserialized controls match any definition
			/// </summary>
			/// <param name="b"></param>
			public void DeSerialize(BinaryReader b)
			{
				buttons.Clear();
				int numbuttons = b.ReadInt32();
				for (int i = 0; i < numbuttons; i++)
				{
					string k = b.ReadString();
					float v = b.ReadSingle();
					buttons.Add(k, v);
				}
			}
			
			/// <summary>
			/// this controller's definition changes to that of source
			/// </summary>
			/// <param name="source"></param>
			public void CopyFrom(IController source)
			{
				this.def = source.Type;
				buttons.Clear();
				foreach (var k in def.BoolButtons)
					buttons.Add(k, source.IsPressed(k) ? 1.0f : 0);
				foreach (var k in def.FloatControls)
				{
					if (buttons.Keys.Contains(k))
						throw new Exception("name collision between bool and float lists!");
					buttons.Add(k, source.GetFloat(k));
				}
			}

			public bool this[string button]
			{
				get { return buttons[button] != 0; }
			}

			public bool IsPressed(string button)
			{
				return buttons[button] != 0;
			}

			public float GetFloat(string name)
			{
				return buttons[name];
			}

			public void UpdateControls(int frame)
			{
				throw new NotImplementedException();
			}
		}


		public void SaveStateText(TextWriter writer)
		{
			var temp = SaveStateBinary();
			temp.SaveAsHex(writer);
			// write extra copy of stuff we don't use
			writer.WriteLine("Frame {0}", Frame);
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
			if (!DeterministicEmulation)
				writer.Write(CoreSaveState());
			else
				writer.Write(savestatebuff);

			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);

			writer.Flush();
		}
		public void LoadStateBinary(BinaryReader reader)
		{
			int size = LibsnesDll.snes_serialize_size();
			byte[] buf = reader.ReadBytes(size);
			CoreLoadState(buf);

			if (DeterministicEmulation) // deserialize controller and fast-foward now
			{
				// reconstruct savestatebuff at the same time to avoid a costly core serialize
				MemoryStream ms = new MemoryStream();
				BinaryWriter bw = new BinaryWriter(ms);
				bw.Write(buf);
				bool framezero = reader.ReadBoolean();
				bw.Write(framezero);
				if (!framezero)
				{
					SnesSaveController ssc = new SnesSaveController(ControllerDefinition);
					ssc.DeSerialize(reader);
					IController tmp = this.Controller;
					this.Controller = ssc;
					nocallbacks = true;
					FrameAdvance(false, false);
					nocallbacks = false;
					this.Controller = tmp;
					ssc.Serialize(bw);
				}
				else // hack: dummy controller info
				{
					bw.Write(reader.ReadBytes(536));
				}
				bw.Close();
				savestatebuff = ms.ToArray();
			}

			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
		}
		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		/// <summary>
		/// handle the unmanaged part of loadstating
		/// </summary>
		void CoreLoadState(byte[] data)
		{
			int size = LibsnesDll.snes_serialize_size();
			if (data.Length != size)
				throw new Exception("Libsnes internal savestate size mismatch!");		
			fixed (byte* pbuf = &data[0])
				LibsnesDll.snes_unserialize(new IntPtr(pbuf), size);
		}
		/// <summary>
		/// handle the unmanaged part of savestating
		/// </summary>
		byte[] CoreSaveState()
		{
			int size = LibsnesDll.snes_serialize_size();
			byte[] buf = new byte[size];
			fixed (byte* pbuf = &buf[0])
				LibsnesDll.snes_serialize(new IntPtr(pbuf), size);
			return buf;
		}

		/// <summary>
		/// most recent internal savestate, for deterministic mode ONLY
		/// </summary>
		byte[] savestatebuff;

		#endregion

		// Arbitrary extensible core comm mechanism
		public CoreInputComm CoreInputComm { get; set; }
		public CoreOutputComm CoreOutputComm { get { return _CoreOutputComm; } }
		CoreOutputComm _CoreOutputComm = new CoreOutputComm();

		// ----- Client Debugging API stuff -----
		unsafe MemoryDomain MakeMemoryDomain(string name, LibsnesDll.SNES_MEMORY id, Endian endian)
		{
			IntPtr block = LibsnesDll.snes_get_memory_data(id);
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

			if (!DeterministicEmulation)
				MemoryDomains.Add(new MemoryDomain("BUS", 0x1000000, Endian.Little,
					(addr) => LibsnesDll.bus_read((uint)addr),
					(addr, val) => LibsnesDll.bus_write((uint)addr, val)));
		}
		public IList<MemoryDomain> MemoryDomains { get; private set; }
		public MemoryDomain MainMemory { get; private set; }




		#region audio stuff

		void InitAudio()
		{
			metaspu = new Sound.MetaspuSoundProvider(Sound.ESynchMethod.ESynchMethod_V);
			resampler = new Sound.Utilities.SpeexResampler(6, 64081, 88200, 32041, 44100, new Action<short[], int>(metaspu.buffer.enqueue_samples));

		}

		Sound.Utilities.SpeexResampler resampler;

		Sound.MetaspuSoundProvider metaspu;

		void snes_audio_sample(ushort left, ushort right)
		{
			resampler.EnqueueSample((short)left, (short)right);
		}


		//BinaryWriter dbgs = new BinaryWriter(File.Open("dbgwav.raw", FileMode.Create, FileAccess.Write));

		public void GetSamples(short[] samples)
		{
			resampler.Flush();
			metaspu.GetSamples(samples);
		}

		public void DiscardSamples()
		{
			metaspu.DiscardSamples();
		}

		public int MaxVolume { get; set; }

		#endregion audio stuff

	}
}
