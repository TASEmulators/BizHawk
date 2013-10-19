//TODO - add serializer, add interlace field variable to serializer

//http://wiki.superfamicom.org/snes/show/Backgrounds

//TODO 
//libsnes needs to be modified to support multiple instances - THIS IS NECESSARY - or else loading one game and then another breaks things
// edit - this is a lot of work
//wrap dll code around some kind of library-accessing interface so that it doesnt malfunction if the dll is unavailable

using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Consoles.Nintendo.SNES
{

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
			public int scanline = 0;
			public Action<int> callback;
		}
	}

	public unsafe class LibsnesCore : IEmulator, IVideoProvider
	{
		public bool IsSGB { get; private set; }

		/// <summary>disable all external callbacks.  the front end should not even know the core is frame advancing</summary>
		bool nocallbacks = false;

		bool disposed = false;
		public void Dispose()
		{
			if (disposed) return;
			disposed = true;

			api.snes_unload_cartridge();
			api.snes_term();

			resampler.Dispose();
			api.Dispose();
		}

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
			if (ScanlineHookManager.HookCount == 0) api.snes_set_scanlineStart(null);
			else api.snes_set_scanlineStart(scanlineStart_cb);
		}

		void snes_scanlineStart(int line)
		{
			ScanlineHookManager.HandleScanline(line);
		}

		string snes_path_request(int slot, string hint)
		{
			//every rom requests msu1.rom... why? who knows.
			//also handle msu-1 pcm files here
			bool is_msu1_rom = hint == "msu1.rom";
			bool is_msu1_pcm = Path.GetExtension(hint).ToLower() == ".pcm";
			if (is_msu1_rom || is_msu1_pcm)
			{
				//well, check if we have an msu-1 xml
				if (romxml != null && romxml["cartridge"] != null && romxml["cartridge"]["msu1"] != null)
				{
					var msu1 = romxml["cartridge"]["msu1"];
					if (is_msu1_rom && msu1["rom"].Attributes["name"] != null)
						return CoreComm.CoreFileProvider.PathSubfile(msu1["rom"].Attributes["name"].Value);
					if (is_msu1_pcm)
					{
						//return @"D:\roms\snes\SuperRoadBlaster\SuperRoadBlaster-1.pcm";
						//return "";
						int wantsTrackNumber = int.Parse(hint.Replace("track-", "").Replace(".pcm", ""));
						wantsTrackNumber++;
						string wantsTrackString = wantsTrackNumber.ToString();
						foreach (var child in msu1.ChildNodes.Cast<XmlNode>())
						{
							if (child.Name == "track" && child.Attributes["number"].Value == wantsTrackString)
								return CoreComm.CoreFileProvider.PathSubfile(child.Attributes["name"].Value);
						}
					}
				}

				//not found.. what to do? (every rom will get here when msu1.rom is requested)
				return "";
			}

			//build romfilename
			string test = Path.Combine(CoreComm.SNES_FirmwaresPath ?? "", hint);

			//does it exist?
			if (!File.Exists(test))
			{
				System.Windows.Forms.MessageBox.Show("The SNES core is referencing a firmware file which could not be found. Please make sure it's in your configured SNES firmwares folder. The referenced filename is: " + hint);
				return "";
			}

			Console.WriteLine("Served libsnes request for firmware \"{0}\" with \"{1}\"", hint, test);

			//return the path we built
			return test;
		}

		void snes_trace(string msg)
		{
			CoreComm.Tracer.Put(msg);
		}

		public SnesColors.ColorType CurrPalette { get; private set; }

		public void SetPalette(SnesColors.ColorType pal)
		{
			CurrPalette = pal;
			int[] tmp = SnesColors.GetLUT(pal);
			fixed (int* p = &tmp[0])
				api.snes_set_color_lut((IntPtr)p);
		}

		public LibsnesApi api;
		System.Xml.XmlDocument romxml;

		public LibsnesCore(CoreComm comm)
		{
			CoreComm = comm;
			api = new LibsnesApi(CoreComm.SNES_ExePath);
			api.snes_init();
		}

		LibsnesApi.snes_scanlineStart_t scanlineStart_cb;
		LibsnesApi.snes_trace_t tracecb;
		LibsnesApi.snes_audio_sample_t soundcb;

		public void Load(GameInfo game, byte[] romData, byte[] sgbRomData, bool DeterministicEmulation, byte[] xmlData)
		{
			ScanlineHookManager = new MyScanlineHookManager(this);

			api.snes_init();

			api.snes_set_video_refresh(snes_video_refresh);
			api.snes_set_input_poll(snes_input_poll);
			api.snes_set_input_state(snes_input_state);
			api.snes_set_input_notify(snes_input_notify);
			api.snes_set_path_request(snes_path_request);
			
			scanlineStart_cb = new LibsnesApi.snes_scanlineStart_t(snes_scanlineStart);
			tracecb = new LibsnesApi.snes_trace_t(snes_trace);

			soundcb = new LibsnesApi.snes_audio_sample_t(snes_audio_sample);
			api.snes_set_audio_sample(soundcb);

			// set default palette. Should be overridden by frontend probably
			SetPalette(SnesColors.ColorType.BizHawk);


			// start up audio resampler
			InitAudio();

			//strip header
			if(romData != null)
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
				if (!api.snes_load_cartridge_super_game_boy(null, sgbRomData, (uint)sgbRomData.Length, null, romData, (uint)romData.Length))
					throw new Exception("snes_load_cartridge_super_game_boy() failed");
			}
			else
			{
				//we may need to get some information out of the cart, even during the following bootup/load process
				if (xmlData != null)
				{
					romxml = new System.Xml.XmlDocument();
					romxml.Load(new MemoryStream(xmlData));

					//bsnes wont inspect the xml to load the necessary sfc file.
					//so, we have to do that here and pass it in as the romData :/
					if (romxml["cartridge"] != null && romxml["cartridge"]["rom"] != null)
						romData = File.ReadAllBytes(CoreComm.CoreFileProvider.PathSubfile(romxml["cartridge"]["rom"].Attributes["name"].Value));
					else
						throw new Exception("Could not find rom file specification in xml file. Please check the integrity of your xml file");
				}

				SystemId = "SNES";
				if (!api.snes_load_cartridge_normal(xmlData, romData))
					throw new Exception("snes_load_cartridge_normal() failed");
			}

			if (api.snes_get_region() == LibsnesApi.SNES_REGION.NTSC)
			{
				//similar to what aviout reports from snes9x and seems logical from bsnes first principles. bsnes uses that numerator (ntsc master clockrate) for sure.
				CoreComm.VsyncNum = 21477272;
				CoreComm.VsyncDen = 4 * 341 * 262;
			}
			else
			{
				//http://forums.nesdev.com/viewtopic.php?t=5367&start=19
				CoreComm.VsyncNum = 21281370;
				CoreComm.VsyncDen = 4 * 341 * 312;
			}

			CoreComm.CpuTraceAvailable = true;

			api.snes_power();

			SetupMemoryDomains(romData,sgbRomData);

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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="port">0 or 1, corresponding to L and R physical ports on the snes</param>
		/// <param name="device">LibsnesApi.SNES_DEVICE enum index specifying type of device</param>
		/// <param name="index">meaningless for most controllers.  for multitap, 0-3 for which multitap controller</param>
		/// <param name="id">button ID enum; in the case of a regular controller, this corresponds to shift register position</param>
		/// <returns>for regular controllers, one bit D0 of button status.  for other controls, varying ranges depending on id</returns>
		ushort snes_input_state(int port, int device, int index, int id)
		{
			// as this is implemented right now, only P1 and P2 normal controllers work

			if (!nocallbacks && CoreComm.InputCallback != null) CoreComm.InputCallback();
			//Console.WriteLine("{0} {1} {2} {3}", port, device, index, id);

			string key = "P" + (1 + port) + " ";
			if ((LibsnesApi.SNES_DEVICE)device == LibsnesApi.SNES_DEVICE.JOYPAD)
			{
				switch ((LibsnesApi.SNES_DEVICE_ID)id)
				{
					case LibsnesApi.SNES_DEVICE_ID.JOYPAD_A: key += "A"; break;
					case LibsnesApi.SNES_DEVICE_ID.JOYPAD_B: key += "B"; break;
					case LibsnesApi.SNES_DEVICE_ID.JOYPAD_X: key += "X"; break;
					case LibsnesApi.SNES_DEVICE_ID.JOYPAD_Y: key += "Y"; break;
					case LibsnesApi.SNES_DEVICE_ID.JOYPAD_UP: key += "Up"; break;
					case LibsnesApi.SNES_DEVICE_ID.JOYPAD_DOWN: key += "Down"; break;
					case LibsnesApi.SNES_DEVICE_ID.JOYPAD_LEFT: key += "Left"; break;
					case LibsnesApi.SNES_DEVICE_ID.JOYPAD_RIGHT: key += "Right"; break;
					case LibsnesApi.SNES_DEVICE_ID.JOYPAD_L: key += "L"; break;
					case LibsnesApi.SNES_DEVICE_ID.JOYPAD_R: key += "R"; break;
					case LibsnesApi.SNES_DEVICE_ID.JOYPAD_SELECT: key += "Select"; break;
					case LibsnesApi.SNES_DEVICE_ID.JOYPAD_START: key += "Start"; break;
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
			bool doubleSize = CoreComm.SNES_AlwaysDoubleSize;
			bool lineDouble = doubleSize, dotDouble = doubleSize;

			vidWidth = width;
			vidHeight = height;

			int yskip = 1, xskip = 1;

			//if we are in high-res mode, we get double width. so, lets double the height here to keep it square.
			//TODO - does interlacing have something to do with the correct way to handle this? need an example that turns it on.
			if (width == 512)
			{
				vidHeight *= 2;
				yskip = 2;

				lineDouble = true;
				//we dont dot double here because the user wanted double res and the game provided double res
				dotDouble = false;
			}
			else if (lineDouble)
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

			if (dotDouble)
			{
				vidWidth *= 2;
				xskip = 2;
			}

			int size = vidWidth * vidHeight;
			if (vidBuffer.Length != size)
				vidBuffer = new int[size];

			for (int j = 0; j < 2; j++)
			{
				if (j == 1 && !dotDouble) break;
				int xbonus = j;
				for (int i = 0; i < 2; i++)
				{
					//potentially do this twice, if we need to line double
					if (i == 1 && !lineDouble) break;

					int bonus = i * vidWidth + xbonus;
					for (int y = 0; y < height; y++)
						for (int x = 0; x < width; x++)
						{
							int si = y * srcPitch + x + srcStart;
							int di = y * vidWidth * yskip + x * xskip + bonus;
							int rgb = data[si];
							vidBuffer[di] = rgb;
						}
				}
			}
		}

		public void FrameAdvance(bool render, bool rendersound)
		{
			api.MessageCounter = 0;

			if(CoreComm.SNES_UseRingBuffer)
				api.BeginBufferIO();

			/* if the input poll callback is called, it will set this to false
			 * this has to be done before we save the per-frame state in deterministic
			 * mode, because in there, the core actually advances, and might advance
			 * through the point in time where IsLagFrame gets set to false.  makes sense?
			 */

			IsLagFrame = true;

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

			if (!nocallbacks && CoreComm.Tracer.Enabled)
				api.snes_set_trace_callback(tracecb);
			else
				api.snes_set_trace_callback(null);

			// speedup when sound rendering is not needed
			if (!rendersound)
				api.snes_set_audio_sample(null);
			else
				api.snes_set_audio_sample(soundcb);

			bool resetSignal = Controller["Reset"];
			if (resetSignal) api.snes_reset();

			bool powerSignal = Controller["Power"];
			if (powerSignal) api.snes_power();

			//too many messages
			api.snes_set_layer_enable(0, 0, CoreComm.SNES_ShowBG1_0);
			api.snes_set_layer_enable(0, 1, CoreComm.SNES_ShowBG1_1);
			api.snes_set_layer_enable(1, 0, CoreComm.SNES_ShowBG2_0);
			api.snes_set_layer_enable(1, 1, CoreComm.SNES_ShowBG2_1);
			api.snes_set_layer_enable(2, 0, CoreComm.SNES_ShowBG3_0);
			api.snes_set_layer_enable(2, 1, CoreComm.SNES_ShowBG3_1);
			api.snes_set_layer_enable(3, 0, CoreComm.SNES_ShowBG4_0);
			api.snes_set_layer_enable(3, 1, CoreComm.SNES_ShowBG4_1);
			api.snes_set_layer_enable(4, 0, CoreComm.SNES_ShowOBJ_0);
			api.snes_set_layer_enable(4, 1, CoreComm.SNES_ShowOBJ_1);
			api.snes_set_layer_enable(4, 2, CoreComm.SNES_ShowOBJ_2);
			api.snes_set_layer_enable(4, 3, CoreComm.SNES_ShowOBJ_3);

			//apparently this is one frame?
			timeFrameCounter++;
			api.snes_run();

			while (api.HasMessage)
				Console.WriteLine(api.DequeueMessage());

			if (IsLagFrame)
				LagCount++;

			//diagnostics for IPC traffic
			//Console.WriteLine(api.MessageCounter);

			api.EndBufferIO();
		}

		public DisplayType DisplayType
		{
			get
			{
				if (api.snes_get_region() == LibsnesApi.SNES_REGION.NTSC)
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
					"Reset", "Power",
					"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Select", "P1 Start", "P1 Y", "P1 X", "P1 B", "P1 A", "P1 L", "P1 R",
					"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Select", "P2 Start", "P2 Y", "P2 X", "P2 B", "P2 A", "P2 L", "P2 R",
					"P3 Up", "P3 Down", "P3 Left", "P3 Right", "P3 Select", "P3 Start", "P3 Y", "P3 X", "P3 B", "P3 A", "P3 L", "P3 R",
					"P4 Up", "P4 Down", "P4 Left", "P4 Right", "P4 Select", "P4 Start", "P4 Y", "P4 X", "P4 B", "P4 A", "P4 L", "P4 R",
				}
			};

		int timeFrameCounter;
		public int Frame { get { return timeFrameCounter; } set { timeFrameCounter = value; } }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; private set; }
		public string SystemId { get; private set; }

		public string BoardName { get { return null; } }

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
				return api.snes_get_memory_size(LibsnesApi.SNES_MEMORY.CARTRIDGE_RAM) != 0;
			}
		}

		public byte[] ReadSaveRam()
		{
			byte* buf = api.snes_get_memory_data(LibsnesApi.SNES_MEMORY.CARTRIDGE_RAM);
			var size = api.snes_get_memory_size(LibsnesApi.SNES_MEMORY.CARTRIDGE_RAM);
			var ret = new byte[size];
			Marshal.Copy((IntPtr)buf, ret, 0, size);
			return ret;
		}

		//public byte[] snes_get_memory_data_read(LibsnesApi.SNES_MEMORY id)
		//{
		//  var size = (int)api.snes_get_memory_size(id);
		//  if (size == 0) return new byte[0];
		//  var ret = api.snes_get_memory_data(id);
		//  return ret;
		//}

		public void StoreSaveRam(byte[] data)
		{
			var size = api.snes_get_memory_size(LibsnesApi.SNES_MEMORY.CARTRIDGE_RAM);
			if (size == 0) return;
			if (size != data.Length) throw new InvalidOperationException("Somehow, we got a mismatch between saveram size and what bsnes says the saveram size is");
			byte* buf = api.snes_get_memory_data(LibsnesApi.SNES_MEMORY.CARTRIDGE_RAM);
			Marshal.Copy(data, 0, (IntPtr)buf, size);
		}

		public void ClearSaveRam()
		{
			byte[] cleardata = new byte[(int)api.snes_get_memory_size(LibsnesApi.SNES_MEMORY.CARTRIDGE_RAM)];
			StoreSaveRam(cleardata);
		}

		public void ResetFrameCounter()
		{
			timeFrameCounter = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		#region savestates

		/// <summary>
		/// can freeze a copy of a controller input set and serialize\deserialize it
		/// </summary>
		public class SnesSaveController : IController
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

			public void Clear()
			{
				buttons.Clear();
			}

			public void Set(string button)
			{
				buttons[button] = 1.0f;
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
				//throw new NotImplementedException();
			}
		}


		public void SaveStateText(TextWriter writer)
		{
			var temp = SaveStateBinary();
			temp.SaveAsHexFast(writer);
			writer.WriteLine("Frame {0}", Frame); // we don't parse this, it's only for the client to use
			writer.WriteLine("Profile {0}", CoreComm.SNES_Profile);
		}
		public void LoadStateText(TextReader reader)
		{
			string hex = reader.ReadLine();
			if (hex.StartsWith("emuVersion")) // movie save
			{
				do // theoretically, our portion should start right after StartsFromSavestate, maybe...
				{
					hex = reader.ReadLine();
				} while (!hex.StartsWith("StartsFromSavestate"));
				hex = reader.ReadLine();
			}
			byte[] state = new byte[hex.Length / 2];
			state.ReadFromHexFast(hex);
			LoadStateBinary(new BinaryReader(new MemoryStream(state)));
			reader.ReadLine(); // Frame #
			var profile = reader.ReadLine().Split(' ')[1];
			ValidateLoadstateProfile(profile);
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
			writer.Write(CoreComm.SNES_Profile);

			writer.Flush();
		}
		public void LoadStateBinary(BinaryReader reader)
		{
			int size = api.snes_serialize_size();
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
			var profile = reader.ReadString();
			ValidateLoadstateProfile(profile);
		}

		void ValidateLoadstateProfile(string profile)
		{
			if (profile != CoreComm.SNES_Profile)
			{
				throw new InvalidOperationException("You've attempted to load a savestate made using a different SNES profile than your current configuration. We COULD automatically switch for you, but we havent done that yet. This error is to make sure you know that this isnt going to work right now.");
			}
		}

		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		public bool BinarySaveStatesPreferred { get { return true; } }

		/// <summary>
		/// handle the unmanaged part of loadstating
		/// </summary>
		void CoreLoadState(byte[] data)
		{
			int size = api.snes_serialize_size();
			if (data.Length != size)
				throw new Exception("Libsnes internal savestate size mismatch!");
			api.snes_init();
			fixed (byte* pbuf = &data[0])
				api.snes_unserialize(new IntPtr(pbuf), size);
		}
		/// <summary>
		/// handle the unmanaged part of savestating
		/// </summary>
		byte[] CoreSaveState()
		{
			int size = api.snes_serialize_size();
			byte[] buf = new byte[size];
			fixed (byte* pbuf = &buf[0])
				api.snes_serialize(new IntPtr(pbuf), size);
			return buf;
		}

		/// <summary>
		/// most recent internal savestate, for deterministic mode ONLY
		/// </summary>
		byte[] savestatebuff;

		#endregion

		public CoreComm CoreComm { get; private set; }

		// ----- Client Debugging API stuff -----
		unsafe MemoryDomain MakeMemoryDomain(string name, LibsnesApi.SNES_MEMORY id, Endian endian)
		{
			int size = api.snes_get_memory_size(id);
			int mask = size - 1;

			//if this type of memory isnt available, dont make the memory domain (most commonly save ram)
			if (size == 0)
				return null;

			byte* blockptr = api.snes_get_memory_data(id);

			MemoryDomain md;

			if(id == LibsnesApi.SNES_MEMORY.OAM)
			{
				//OAM is actually two differently sized banks of memory which arent truly considered adjacent. 
				//maybe a better way to visualize it is with an empty bus and adjacent banks
				//so, we just throw away everything above its size of 544 bytes
				if (size != 544) throw new InvalidOperationException("oam size isnt 544 bytes.. wtf?");
				md = new MemoryDomain(name, size, endian,
				   (addr) => (addr < 544) ? blockptr[addr] : (byte)0x00,
					 (addr, value) => { if (addr < 544) blockptr[addr] = value; }
					 );
			}
			else
				md = new MemoryDomain(name, size, endian,
						(addr) => blockptr[addr & mask],
						(addr, value) => blockptr[addr & mask] = value);

			MemoryDomains.Add(md);

			return md;
		}

		void SetupMemoryDomains(byte[] romData, byte[] sgbRomData)
		{
			MemoryDomains = new List<MemoryDomain>();
			// remember, MainMemory must always be the same as MemoryDomains[0], else GIANT DRAGONS
			//<zeromus> - this is stupid.

			//lets just do this entirely differently for SGB
			if (IsSGB)
			{
				//NOTE: CGB has 32K of wram, and DMG has 8KB of wram. Not sure how to control this right now.. bsnes might not have any ready way of doign that? I couldnt spot it. 
				//You wouldnt expect a DMG game to access excess wram, but what if it tried to? maybe an oversight in bsnes?
				MakeMemoryDomain("SGB WRAM", LibsnesApi.SNES_MEMORY.SGB_WRAM, Endian.Little);


				var romDomain = new MemoryDomain("SGB CARTROM", romData.Length, Endian.Little,
					(addr) => romData[addr],
					(addr, value) => romData[addr] = value);
				MemoryDomains.Add(romDomain);
		

				//the last 1 byte of this is special.. its an interrupt enable register, instead of ram. weird. maybe its actually ram and just getting specially used?
				MakeMemoryDomain("SGB HRAM", LibsnesApi.SNES_MEMORY.SGB_HRAM, Endian.Little);

				MakeMemoryDomain("SGB CARTRAM", LibsnesApi.SNES_MEMORY.SGB_CARTRAM, Endian.Little);

				MainMemory = MakeMemoryDomain("WRAM", LibsnesApi.SNES_MEMORY.WRAM, Endian.Little);

				var sgbromDomain = new MemoryDomain("SGB.SFC ROM", sgbRomData.Length, Endian.Little,
					(addr) => sgbRomData[addr],
					(addr, value) => sgbRomData[addr] = value);
				MemoryDomains.Add(sgbromDomain);
			}
			else
			{
				MainMemory = MakeMemoryDomain("WRAM", LibsnesApi.SNES_MEMORY.WRAM, Endian.Little);

				var romDomain = new MemoryDomain("CARTROM", romData.Length, Endian.Little,
					(addr) => romData[addr],
					(addr, value) => romData[addr] = value);
				MemoryDomains.Add(romDomain);

				MakeMemoryDomain("CARTRAM", LibsnesApi.SNES_MEMORY.CARTRIDGE_RAM, Endian.Little);
				MakeMemoryDomain("VRAM", LibsnesApi.SNES_MEMORY.VRAM, Endian.Little);
				MakeMemoryDomain("OAM", LibsnesApi.SNES_MEMORY.OAM, Endian.Little);
				MakeMemoryDomain("CGRAM", LibsnesApi.SNES_MEMORY.CGRAM, Endian.Little);
				MakeMemoryDomain("APURAM", LibsnesApi.SNES_MEMORY.APURAM, Endian.Little);

				if (!DeterministicEmulation)
					MemoryDomains.Add(new MemoryDomain("BUS", 0x1000000, Endian.Little,
						(addr) => api.peek(LibsnesApi.SNES_MEMORY.SYSBUS, (uint)addr),
						(addr, val) => api.poke(LibsnesApi.SNES_MEMORY.SYSBUS, (uint)addr, val)));

			}
		}

		public IList<MemoryDomain> MemoryDomains { get; private set; }
		public MemoryDomain MainMemory { get; private set; }

		#region audio stuff

		Sound.Utilities.SpeexResampler resampler;

		void InitAudio()
		{
			resampler = new Sound.Utilities.SpeexResampler(6, 64081, 88200, 32041, 44100);
		}

		void snes_audio_sample(ushort left, ushort right)
		{
			resampler.EnqueueSample((short)left, (short)right);
		}

		public ISoundProvider SoundProvider { get { return null; } }
		public ISyncSoundProvider SyncSoundProvider { get { return resampler; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }

		#endregion audio stuff
	}
}
