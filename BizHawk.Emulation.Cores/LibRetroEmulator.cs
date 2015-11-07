using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Common.BufferExtensions;

namespace BizHawk.Emulation.Cores
{
	[CoreAttributes("Libretro", "natt&zeromus")]
	public unsafe class LibRetroEmulator : IEmulator, ISettable<LibRetroEmulator.Settings, LibRetroEmulator.SyncSettings>,
		ISaveRam, IStatable, IVideoProvider, IInputPollable
	{
		#region Settings

		Settings _Settings = new Settings();
		SyncSettings _SyncSettings;

		public class SyncSettings
		{
			public SyncSettings Clone()
			{
				return JsonConvert.DeserializeObject<SyncSettings>(JsonConvert.SerializeObject(this));
			}

			public SyncSettings()
			{
			}
		}


		public class Settings
		{
			public void Validate()
			{
			}

			public Settings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public Settings Clone()
			{
				return (Settings)MemberwiseClone();
			}
		}

		public Settings GetSettings()
		{
			return _Settings.Clone();
		}

		public SyncSettings GetSyncSettings()
		{
			return _SyncSettings.Clone();
		}

		public bool PutSettings(Settings o)
		{
			_Settings.Validate();
			_Settings = o;

			//TODO - store settings into core? or we can just keep doing it before frameadvance

			return false;
		}

		public bool PutSyncSettings(SyncSettings o)
		{
			//currently LEC and pad settings changes both require reboot
			bool reboot = true;

			//we could do it this way roughly if we need to
			//if(JsonConvert.SerializeObject(o.FIOConfig) != JsonConvert.SerializeObject(_SyncSettings.FIOConfig)


			_SyncSettings = o;


			return reboot;
		}

		#endregion

		#region callbacks

		unsafe bool retro_environment(LibRetro.RETRO_ENVIRONMENT cmd, IntPtr data)
		{
			Console.WriteLine(cmd);
			switch (cmd)
			{
				case LibRetro.RETRO_ENVIRONMENT.SET_ROTATION:
					return false;
				case LibRetro.RETRO_ENVIRONMENT.GET_OVERSCAN:
					return false;
				case LibRetro.RETRO_ENVIRONMENT.GET_CAN_DUPE:
					return true;
				case LibRetro.RETRO_ENVIRONMENT.SET_MESSAGE:
					{
						LibRetro.retro_message msg = new LibRetro.retro_message();
						Marshal.PtrToStructure(data, msg);
						if (!string.IsNullOrEmpty(msg.msg))
							Console.WriteLine("LibRetro Message: {0}", msg.msg);
						return true;
					}
				case LibRetro.RETRO_ENVIRONMENT.SHUTDOWN:
					return false;
				case LibRetro.RETRO_ENVIRONMENT.SET_PERFORMANCE_LEVEL:
					return false;
				case LibRetro.RETRO_ENVIRONMENT.GET_SYSTEM_DIRECTORY:
					//an alternative (alongside where the saverams and such will go?)
					//*((IntPtr*)data.ToPointer()) = unmanagedResources.StringToHGlobalAnsi(CoreComm.CoreFileProvider.GetGameBasePath());
					*((IntPtr*)data.ToPointer()) = SystemDirectoryAtom;
					return false;
				case LibRetro.RETRO_ENVIRONMENT.SET_PIXEL_FORMAT:
					{
						LibRetro.RETRO_PIXEL_FORMAT fmt = 0;
						int[] tmp = new int[1];
						Marshal.Copy(data, tmp, 0, 1);
						fmt = (LibRetro.RETRO_PIXEL_FORMAT)tmp[0];
						switch (fmt)
						{
							case LibRetro.RETRO_PIXEL_FORMAT.RGB565:
							case LibRetro.RETRO_PIXEL_FORMAT.XRGB1555:
							case LibRetro.RETRO_PIXEL_FORMAT.XRGB8888:
								pixelfmt = fmt;
								Console.WriteLine("New pixel format set: {0}", pixelfmt);
								return true;
							default:
								Console.WriteLine("Unrecognized pixel format: {0}", (int)pixelfmt);
								return false;
						}
					}
				case LibRetro.RETRO_ENVIRONMENT.SET_INPUT_DESCRIPTORS:
					return false;
				case LibRetro.RETRO_ENVIRONMENT.SET_KEYBOARD_CALLBACK:
					return false;
				case LibRetro.RETRO_ENVIRONMENT.SET_DISK_CONTROL_INTERFACE:
					return false;
				case LibRetro.RETRO_ENVIRONMENT.SET_HW_RENDER:
					// this can be done in principle, but there's no reason to right now
					return false;
				case LibRetro.RETRO_ENVIRONMENT.GET_VARIABLE:
					{
						void** variables = (void**)data.ToPointer();
						IntPtr pKey = new IntPtr(*variables++);
						string key = Marshal.PtrToStringAnsi(pKey);
						Console.WriteLine("Requesting variable: {0}", key);
						*variables = unmanagedResources.StringToHGlobalAnsi("0").ToPointer();
					}
					return false;
				case LibRetro.RETRO_ENVIRONMENT.SET_VARIABLES:
					{
						void** variables = (void**)data.ToPointer();
						for (; ; )
						{
							IntPtr pKey = new IntPtr(*variables++);
							IntPtr pValue = new IntPtr(*variables++);
							if(pKey == IntPtr.Zero)
								break;
							string key = Marshal.PtrToStringAnsi(pKey);
							string value = Marshal.PtrToStringAnsi(pValue);
							Console.WriteLine("Defined variable: {0} = {1}", key, value);
						}
					}
					return false;
				case LibRetro.RETRO_ENVIRONMENT.GET_VARIABLE_UPDATE:
					return false;
				case LibRetro.RETRO_ENVIRONMENT.SET_SUPPORT_NO_GAME:
					EnvironmentInfo.SupportNoGame = true;
					return false;
				case LibRetro.RETRO_ENVIRONMENT.GET_LIBRETRO_PATH:
					return false;
				case LibRetro.RETRO_ENVIRONMENT.SET_AUDIO_CALLBACK:
					return false;
				case LibRetro.RETRO_ENVIRONMENT.SET_FRAME_TIME_CALLBACK:
					return false;
				case LibRetro.RETRO_ENVIRONMENT.GET_RUMBLE_INTERFACE:
					return false;
				case LibRetro.RETRO_ENVIRONMENT.GET_INPUT_DEVICE_CAPABILITIES:
					return false;
				case LibRetro.RETRO_ENVIRONMENT.GET_LOG_INTERFACE:
					return false;
				case LibRetro.RETRO_ENVIRONMENT.GET_PERF_INTERFACE:
					return false;
				case LibRetro.RETRO_ENVIRONMENT.GET_LOCATION_INTERFACE:
					return false;
				case LibRetro.RETRO_ENVIRONMENT.GET_CORE_ASSETS_DIRECTORY:
					return false;
				case LibRetro.RETRO_ENVIRONMENT.GET_SAVE_DIRECTORY:
					//this will suffice for now. if we find evidence later it's needed we can stash a string with 
					//unmanagedResources and CoreFileProvider
					*((IntPtr*)data.ToPointer()) = IntPtr.Zero; 
					return false;
				case LibRetro.RETRO_ENVIRONMENT.SET_CONTROLLER_INFO:
					return true;
				default:
					Console.WriteLine("Unknkown retro_environment command {0}", (int)cmd);
					return false;
			}
		}
		void retro_input_poll()
		{
			IsLagFrame = false;
		}

		private bool GetButton(uint pnum, string type, string button)
		{
			string key = string.Format("P{0} {1} {2}", pnum, type, button);
			bool b = Controller[key];
			if (b == true)
			{
				return true; //debugging placeholder
			}
			else return false;
		}

		//port = console physical port?
		//device = logical device type
		//index = sub device index? (multitap?)
		//id = button id
		short retro_input_state(uint port, uint device, uint index, uint id)
		{
			switch ((LibRetro.RETRO_DEVICE)device)
			{
				case LibRetro.RETRO_DEVICE.JOYPAD:
					{
						//The JOYPAD is sometimes called RetroPad (and we'll call it that in user-facing stuff cos retroarch does)
						//It is essentially a Super Nintendo controller, but with additional L2/R2/L3/R3 buttons, similar to a PS1 DualShock.
					
						string button = "";
						switch ((LibRetro.RETRO_DEVICE_ID_JOYPAD)id)
						{
							case LibRetro.RETRO_DEVICE_ID_JOYPAD.A: button = "A"; break;
							case LibRetro.RETRO_DEVICE_ID_JOYPAD.B: button = "B"; break;
							case LibRetro.RETRO_DEVICE_ID_JOYPAD.X: button = "X"; break;
							case LibRetro.RETRO_DEVICE_ID_JOYPAD.Y: button = "Y"; break;
							case LibRetro.RETRO_DEVICE_ID_JOYPAD.UP: button = "Up"; break;
							case LibRetro.RETRO_DEVICE_ID_JOYPAD.DOWN: button = "Down"; break;
							case LibRetro.RETRO_DEVICE_ID_JOYPAD.LEFT: button = "Left"; break;
							case LibRetro.RETRO_DEVICE_ID_JOYPAD.RIGHT: button = "Right"; break;
							case LibRetro.RETRO_DEVICE_ID_JOYPAD.L: button = "L"; break;
							case LibRetro.RETRO_DEVICE_ID_JOYPAD.R: button = "R"; break;
							case LibRetro.RETRO_DEVICE_ID_JOYPAD.SELECT: button = "Select"; break;
							case LibRetro.RETRO_DEVICE_ID_JOYPAD.START: button = "Start"; break;
						}

						return (short)(GetButton(port+1, "RetroPad", button) ? 1 : 0);
					}
				default:
					return 0;
			}
		}

		LibRetro.retro_environment_t retro_environment_cb;
		LibRetro.retro_video_refresh_t retro_video_refresh_cb;
		LibRetro.retro_audio_sample_t retro_audio_sample_cb;
		LibRetro.retro_audio_sample_batch_t retro_audio_sample_batch_cb;
		LibRetro.retro_input_poll_t retro_input_poll_cb;
		LibRetro.retro_input_state_t retro_input_state_cb;

		#endregion

		private LibRetro retro;
		private UnmanagedResourceHeap unmanagedResources = new UnmanagedResourceHeap();

		//todo - make private
		public LibRetro.retro_system_info system_info = new LibRetro.retro_system_info();

		public struct RetroEnvironmentInfo
		{
			public bool SupportNoGame;
		}
		public RetroEnvironmentInfo EnvironmentInfo = new RetroEnvironmentInfo();

		string CoresDirectory;
		string SystemDirectory;
		IntPtr SystemDirectoryAtom;

		public LibRetroEmulator(CoreComm nextComm, string modulename)
		{
			CoresDirectory = Path.GetDirectoryName(new FileInfo(modulename).FullName);
			SystemDirectory = Path.Combine(CoresDirectory, "System");
			SystemDirectoryAtom = unmanagedResources.StringToHGlobalAnsi(SystemDirectory);

			ServiceProvider = new BasicServiceProvider(this);

			_SyncSettings = new SyncSettings();

			retro_environment_cb = new LibRetro.retro_environment_t(retro_environment);
			retro_video_refresh_cb = new LibRetro.retro_video_refresh_t(retro_video_refresh);
			retro_audio_sample_cb = new LibRetro.retro_audio_sample_t(retro_audio_sample);
			retro_audio_sample_batch_cb = new LibRetro.retro_audio_sample_batch_t(retro_audio_sample_batch);
			retro_input_poll_cb = new LibRetro.retro_input_poll_t(retro_input_poll);
			retro_input_state_cb = new LibRetro.retro_input_state_t(retro_input_state);

			retro = new LibRetro(modulename);
			try
			{
				CoreComm = nextComm;

				retro.retro_get_system_info(ref system_info);

				retro.retro_set_environment(retro_environment_cb);
				retro.retro_init();
				retro.retro_set_video_refresh(retro_video_refresh_cb);
				retro.retro_set_audio_sample(retro_audio_sample_cb);
				retro.retro_set_audio_sample_batch(retro_audio_sample_batch_cb);
				retro.retro_set_input_poll(retro_input_poll_cb);
				retro.retro_set_input_state(retro_input_state_cb);
			}
			catch
			{
				retro.Dispose();
				retro = null;
				throw;
			}
		}

		public IEmulatorServiceProvider ServiceProvider { get; private set; }



		public bool LoadData(byte[] data)
		{
			LibRetro.retro_game_info gi = new LibRetro.retro_game_info();
			fixed (byte* p = &data[0])
			{
				gi.data = (IntPtr)p;
				gi.meta = "";
				gi.path = "";
				gi.size = (uint)data.Length;
				return LoadWork(ref gi);
			}
		}

		public bool LoadPath(string path)
		{
			LibRetro.retro_game_info gi = new LibRetro.retro_game_info();
			gi.path = path; //is this the right encoding? seems to be ok
			return LoadWork(ref gi);
		}

		public bool LoadNoGame()
		{
			LibRetro.retro_game_info gi = new LibRetro.retro_game_info();
			return LoadWork(ref gi);
		}

		bool LoadWork(ref LibRetro.retro_game_info gi)
		{
			if (!retro.retro_load_game(ref gi))
			{
				Console.WriteLine("retro_load_game() failed");
				return false;
			}

			//TODO - libretro cores can return a varying serialize size over time. I tried to get them to write it in the docs...
			savebuff = new byte[retro.retro_serialize_size()];
			savebuff2 = new byte[savebuff.Length + 13];

			LibRetro.retro_system_av_info av = new LibRetro.retro_system_av_info();
			retro.retro_get_system_av_info(ref av);

			BufferWidth = (int)av.geometry.base_width;
			BufferHeight = (int)av.geometry.base_height;
			vidbuff = new int[av.geometry.max_width * av.geometry.max_height];
			dar = av.geometry.aspect_ratio;

			// TODO: more precise
			CoreComm.VsyncNum = (int)(10000000 * av.timing.fps);
			CoreComm.VsyncDen = 10000000;

			SetupResampler(av.timing.fps, av.timing.sample_rate);

			ControllerDefinition = CreateControllerDefinition(_SyncSettings);

			return true;
		}

		public static ControllerDefinition CreateControllerDefinition(SyncSettings syncSettings)
		{
			ControllerDefinition definition = new ControllerDefinition();
			definition.Name = "LibRetro Controls"; // <-- for compatibility

			foreach(var item in new[] {
					"P1 {0} Up", "P1 {0} Down", "P1 {0} Left", "P1 {0} Right", "P1 {0} Select", "P1 {0} Start", "P1 {0} Y", "P1 {0} B", "P1 {0} X", "P1 {0} A", "P1 {0} L", "P1 {0} R",
					"P2 {0} Up", "P2 {0} Down", "P2 {0} Left", "P2 {0} Right", "P2 {0} Select", "P2 {0} Start", "P2 {0} Y", "P2 {0} B", "P2 {0} X", "P2 {0} A", "P2 {0} L", "P2 {0} R",
			})
				definition.BoolButtons.Add(string.Format(item,"RetroPad"));

			return definition;
		}

		public ControllerDefinition ControllerDefinition { get; private set; }
		public IController Controller { get; set; }

		public void FrameAdvance(bool render, bool rendersound = true)
		{
			//TODO - consider changing directory and using Libretro subdir of bizhawk as a kind of sandbox, for the duration of the run?
			IsLagFrame = true;
			Frame++;
			nsamprecv = 0;
			retro.retro_run();
			//Console.WriteLine("[{0}]", nsamprecv);
		}

		public int Frame { get; private set; }

		public string SystemId
		{
			get { return "Libretro"; }
		}

		public bool DeterministicEmulation
		{
			// who knows
			get { return true; }
		}

		public string BoardName
		{
			get { return null; }
		}

		#region ISaveRam
		//TODO - terrible things will happen if this changes at runtime

		byte[] saverambuff = new byte[0];

		public byte[] CloneSaveRam()
		{
			int size = (int)retro.retro_get_memory_size(LibRetro.RETRO_MEMORY.SAVE_RAM);
			if (saverambuff.Length != size)
				saverambuff = new byte[size];

			IntPtr src = retro.retro_get_memory_data(LibRetro.RETRO_MEMORY.SAVE_RAM);
			if (src == IntPtr.Zero)
				return null;
			
			Marshal.Copy(src, saverambuff, 0, size);
			return (byte[])saverambuff.Clone();
		}

		public void StoreSaveRam(byte[] data)
		{
			int size = (int)retro.retro_get_memory_size(LibRetro.RETRO_MEMORY.SAVE_RAM);

			if (size == 0)
				return;

			IntPtr dst = retro.retro_get_memory_data(LibRetro.RETRO_MEMORY.SAVE_RAM);
			if (dst == IntPtr.Zero)
				throw new Exception("retro_get_memory_data(RETRO_MEMORY_SAVE_RAM) returned NULL");

			Marshal.Copy(data, 0, dst, size);
		}

		public bool SaveRamModified
		{
			[FeatureNotImplemented]
			get
			{
				//if we dont have saveram, it isnt modified. otherwise, assume iti s
				int size = (int)retro.retro_get_memory_size(LibRetro.RETRO_MEMORY.SAVE_RAM);
				if (size == 0)
					return false;
				return true;
			}

			[FeatureNotImplemented]
			set { throw new NotImplementedException(); }
		}

		#endregion

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		#region savestates

		private byte[] savebuff;
		private byte[] savebuff2;

		public void SaveStateText(System.IO.TextWriter writer)
		{
			var temp = SaveStateBinary();
			temp.SaveAsHex(writer);
		}

		public void LoadStateText(System.IO.TextReader reader)
		{
			string hex = reader.ReadLine();
			byte[] state = new byte[hex.Length / 2];
			state.ReadFromHex(hex);
			LoadStateBinary(new BinaryReader(new MemoryStream(state)));
		}

		public void SaveStateBinary(System.IO.BinaryWriter writer)
		{
			//is this the only way we know of to detect unavailable savestates?
			if (savebuff.Length > 0)
			{
				fixed (byte* ptr = &savebuff[0])
				{
					if (!retro.retro_serialize((IntPtr)ptr, (uint)savebuff.Length))
						throw new Exception("retro_serialize() failed");
				}
			}

			writer.Write(savebuff.Length);
			writer.Write(savebuff);
			// other variables
			writer.Write(Frame);
			writer.Write(LagCount);
			writer.Write(IsLagFrame);
		}

		public void LoadStateBinary(System.IO.BinaryReader reader)
		{
			int newlen = reader.ReadInt32();
			if (newlen > savebuff.Length)
				throw new Exception("Unexpected buffer size");
			reader.Read(savebuff, 0, newlen);
			if (savebuff.Length > 0)
			{
				fixed (byte* ptr = &savebuff[0])
				{
					if (!retro.retro_unserialize((IntPtr)ptr, (uint)newlen))
						throw new Exception("retro_unserialize() failed");
				}
			}
			// other variables
			Frame = reader.ReadInt32();
			LagCount = reader.ReadInt32();
			IsLagFrame = reader.ReadBoolean();
		}

		public byte[] SaveStateBinary()
		{
			var ms = new System.IO.MemoryStream(savebuff2, true);
			var bw = new System.IO.BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			ms.Close();
			return savebuff2;
		}

		public bool BinarySaveStatesPreferred { get { return true; } }

		#endregion

		public CoreComm CoreComm
		{
			get;
			private set;
		}

		#region memory access

		void SetupDebuggingStuff()
		{
		}

		public MemoryDomainList MemoryDomains { get; private set; }

		#endregion

		public void Dispose()
		{
			if (resampler != null)
			{
				resampler.Dispose();
				resampler = null;
			}
			if (retro != null)
			{
				retro.Dispose();
				retro = null;
			}
			unmanagedResources.Dispose();
			unmanagedResources = null;
		}

		#region ISoundProvider

		public ISoundProvider SoundProvider { get { return null; } }
		public ISyncSoundProvider SyncSoundProvider { get { return resampler; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }

		SpeexResampler resampler;

		short[] sampbuff = new short[0];

		// debug
		int nsamprecv = 0;

		void SetupResampler(double fps, double sps)
		{
			Console.WriteLine("FPS {0} SPS {1}", fps, sps);

			// todo: more precise?
			uint spsnum = (uint)sps * 1000;
			uint spsden = (uint)1000;

			resampler = new SpeexResampler(5, 44100 * spsden, spsnum, (uint)sps, 44100, null, null);
		}

		void retro_audio_sample(short left, short right)
		{
			resampler.EnqueueSample(left, right);
			nsamprecv++;
		}

		uint retro_audio_sample_batch(IntPtr data, uint frames)
		{
			if (sampbuff.Length < frames * 2)
				sampbuff = new short[frames * 2];
			Marshal.Copy(data, sampbuff, 0, (int)(frames * 2));
			resampler.EnqueueSamples(sampbuff, (int)frames);
			nsamprecv += (int)frames;
			// what is the return from this used for?
			return frames;
		}

		#endregion

		#region IVideoProvider

		float dar;
		int[] vidbuff;
		LibRetro.RETRO_PIXEL_FORMAT pixelfmt = LibRetro.RETRO_PIXEL_FORMAT.XRGB1555;

		void Blit555(short* src, int* dst, int width, int height, int pitch)
		{
			for (int j = 0; j < height; j++)
			{
				short* row = src;
				for (int i = 0; i < width; i++)
				{
					short ci = *row;
					int r = ci & 0x001f;
					int g = ci & 0x03e0;
					int b = ci & 0x7c00;

					r = (r << 3) | (r >> 2);
					g = (g >> 2) | (g >> 7);
					b = (b >> 7) | (b >> 12);
					int co = r | g | b | unchecked((int)0xff000000);

					*dst = co;
					dst++;
					row++;
				}
				src += pitch;
			}
		}

		void Blit565(short* src, int* dst, int width, int height, int pitch)
		{
			for (int j = 0; j < height; j++)
			{
				short* row = src;
				for (int i = 0; i < width; i++)
				{
					short ci = *row;
					int r = ci & 0x001f;
					int g = (ci & 0x07e0)>>5;
					int b = (ci & 0xf800)>>11;

					r = (r << 3) | (r >> 2);
					g = (g << 2) | (g >> 4);
					b = (b << 3) | (b >> 2);
					int co = (b<<16) | (g<<8) | r;

					*dst = co;
					dst++;
					row++;
				}
				src += pitch;
			}
		}

		void Blit888(int* src, int* dst, int width, int height, int pitch)
		{
			for (int j = 0; j < height; j++)
			{
				int* row = src;
				for (int i = 0; i < width; i++)
				{
					int ci = *row;
					int co = ci | unchecked((int)0xff000000);
					*dst = co;
					dst++;
					row++;
				}
				src += pitch;
			}
		}

		void retro_video_refresh(IntPtr data, uint width, uint height, uint pitch)
		{
			if (data == IntPtr.Zero) // dup frame
				return;
			if (width * height > vidbuff.Length)
			{
				Console.WriteLine("Unexpected libretro video buffer overrun?");
				return;
			}
			fixed (int* dst = &vidbuff[0])
			{
				if (pixelfmt == LibRetro.RETRO_PIXEL_FORMAT.XRGB8888)
					Blit888((int*)data, dst, (int)width, (int)height, (int)pitch / 4);
				else if (pixelfmt == LibRetro.RETRO_PIXEL_FORMAT.RGB565)
					Blit565((short*)data, dst, (int)width, (int)height, (int)pitch / 2);
				else
					Blit555((short*)data, dst, (int)width, (int)height, (int)pitch / 2);
			}
		}


		public int[] GetVideoBuffer()
		{
			return vidbuff;
		}

		public int VirtualWidth
		{
			get
			{
				if(dar==0)
					return BufferWidth;
				else if (dar > 1.0f)
					return (int)(BufferHeight * dar);
				else
					return BufferWidth;
			}
		}
		public int VirtualHeight
		{
			get
			{
				if(dar==0)
					return BufferHeight;
				if (dar < 1.0f)
					return (int)(BufferWidth / dar);
				else
					return BufferHeight;
			}
		}

		public int BufferWidth { get; private set; }
		public int BufferHeight { get; private set; }
		public int BackgroundColor { get { return unchecked((int)0xff000000); } }

		#endregion

		#region IInputPollable
		public int LagCount { get; set; }
		public bool IsLagFrame { get; private set; }
		public IInputCallbackSystem InputCallbacks
		{
			[FeatureNotImplemented]
			get
			{ throw new NotImplementedException(); }
		}
		#endregion
	}
}
