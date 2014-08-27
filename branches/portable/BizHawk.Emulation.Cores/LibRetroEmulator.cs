using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores
{
	[CoreAttributes("DEBUG ONLY DON'T USE", "natt")]
	public unsafe class LibRetroEmulator : IEmulator, IVideoProvider
	{
		#region callbacks

		bool retro_environment(LibRetro.RETRO_ENVIRONMENT cmd, IntPtr data)
		{
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
					return false;
				case LibRetro.RETRO_ENVIRONMENT.SET_VARIABLES:
					return false;
				case LibRetro.RETRO_ENVIRONMENT.GET_VARIABLE_UPDATE:
					return false;
				case LibRetro.RETRO_ENVIRONMENT.SET_SUPPORT_NO_GAME:
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
				default:
					Console.WriteLine("Unknkown retro_environment command {0}", (int)cmd);
					return false;
			}
		}
		void retro_input_poll()
		{
			IsLagFrame = false;
		}
		short retro_input_state(uint port, uint device, uint index, uint id)
		{
			return 0;
		}

		LibRetro.retro_environment_t retro_environment_cb;
		LibRetro.retro_video_refresh_t retro_video_refresh_cb;
		LibRetro.retro_audio_sample_t retro_audio_sample_cb;
		LibRetro.retro_audio_sample_batch_t retro_audio_sample_batch_cb;
		LibRetro.retro_input_poll_t retro_input_poll_cb;
		LibRetro.retro_input_state_t retro_input_state_cb;

		#endregion

		private LibRetro retro;

		public static LibRetroEmulator CreateDebug(CoreComm nextComm, byte[] debugfile)
		{
			System.IO.TextReader tr = new System.IO.StreamReader(new System.IO.MemoryStream(debugfile, false));
			string modulename = tr.ReadLine();
			string romname = tr.ReadLine();

			byte[] romdata = System.IO.File.ReadAllBytes(romname);

			var emu = new LibRetroEmulator(nextComm, modulename);
			try
			{
				if (!emu.Load(romdata))
					throw new Exception("LibRetroEmulator.Load() failed");
				// ...
			}
			catch
			{
				emu.Dispose();
				throw;
			}
			return emu;
		}

		public LibRetroEmulator(CoreComm nextComm, string modulename)
		{
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

				LibRetro.retro_system_info sys = new LibRetro.retro_system_info();
				retro.retro_get_system_info(ref sys);

				if (sys.need_fullpath)
					throw new ArgumentException("This libretro core needs filepaths");
				if (sys.block_extract)
					throw new ArgumentException("This libretro needs non-blocked extract");

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
				throw;
			}
		}

		public bool Load(byte[] data)
		{
			LibRetro.retro_game_info gi = new LibRetro.retro_game_info();
			fixed (byte* p = &data[0])
			{
				gi.data = (IntPtr)p;
				gi.meta = "";
				gi.path = "";
				gi.size = (uint)data.Length;
				if (!retro.retro_load_game(ref gi))
				{
					Console.WriteLine("retro_load_game() failed");
					return false;
				}
				savebuff = new byte[retro.retro_serialize_size()];
				savebuff2 = new byte[savebuff.Length + 13];
			}

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

			return true;
		}


		public ControllerDefinition ControllerDefinition
		{
			get { return NullEmulator.NullController; }
		}

		public IController Controller { get; set; }

		public void FrameAdvance(bool render, bool rendersound = true)
		{
			IsLagFrame = true;
			Frame++;
			nsamprecv = 0;
			retro.retro_run();
			Console.WriteLine("[{0}]", nsamprecv);
		}

		public int Frame { get; private set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; private set; }

		public string SystemId
		{
			get { return "TEST"; }
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

		#region saveram

		byte[] saverambuff = new byte[0];

		public byte[] CloneSaveRam()
		{
			int size = (int)retro.retro_get_memory_size(LibRetro.RETRO_MEMORY.SAVE_RAM);
			if (saverambuff.Length != size)
				saverambuff = new byte[size];

			IntPtr src = retro.retro_get_memory_data(LibRetro.RETRO_MEMORY.SAVE_RAM);
			if (src == IntPtr.Zero)
				throw new Exception("retro_get_memory_data(RETRO_MEMORY_SAVE_RAM) returned NULL");

			Marshal.Copy(src, saverambuff, 0, size);
			return (byte[])saverambuff.Clone();
		}

		public void StoreSaveRam(byte[] data)
		{
			int size = (int)retro.retro_get_memory_size(LibRetro.RETRO_MEMORY.SAVE_RAM);
			if (data.Length != size)
				throw new Exception("Passed saveram does not match retro_get_memory_size(RETRO_MEMORY_SAVE_RAM");

			IntPtr dst = retro.retro_get_memory_data(LibRetro.RETRO_MEMORY.SAVE_RAM);
			if (dst == IntPtr.Zero)
				throw new Exception("retro_get_memory_data(RETRO_MEMORY_SAVE_RAM) returned NULL");

			Marshal.Copy(data, 0, dst, size);
		}

		public void ClearSaveRam()
		{
			// this is sort of wrong, because we should be clearing saveram to whatever the default state is
			// which may or may not be 0-fill
			int size = (int)retro.retro_get_memory_size(LibRetro.RETRO_MEMORY.SAVE_RAM);
			IntPtr dst = retro.retro_get_memory_data(LibRetro.RETRO_MEMORY.SAVE_RAM);
			if (dst == IntPtr.Zero)
				throw new Exception("retro_get_memory_data(RETRO_MEMORY_SAVE_RAM) returned NULL");

			byte* p = (byte*)dst;
			for (int i = 0; i < size; i++)
				p[i] = 0;
		}

		public bool SaveRamModified
		{
			get { return true; }
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
			throw new NotImplementedException();
		}

		public void LoadStateText(System.IO.TextReader reader)
		{
			throw new NotImplementedException();
		}

		public void SaveStateBinary(System.IO.BinaryWriter writer)
		{
			fixed (byte* ptr = &savebuff[0])
			{
				if (!retro.retro_serialize((IntPtr)ptr, (uint)savebuff.Length))
					throw new Exception("retro_serialize() failed");
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
			fixed (byte* ptr = &savebuff[0])
			{
				if (!retro.retro_unserialize((IntPtr)ptr, (uint)newlen))
					throw new Exception("retro_unserialize() failed");
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
			MemoryDomains = MemoryDomainList.GetDummyList();
		}

		public MemoryDomainList MemoryDomains { get; private set; }

		public Dictionary<string, int> GetCpuFlagsAndRegisters()
		{
			throw new NotImplementedException();
		}

		public void SetCpuRegister(string register, int value)
		{
			throw new NotImplementedException();
		}

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

		public IVideoProvider VideoProvider { get { return this; } }

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
					int g = ci & 0x07e0;
					int b = ci & 0xf800;

					r = (r << 3) | (r >> 2);
					g = (g >> 3) | (g >> 9);
					b = (b >> 8) | (b >> 13);
					int co = r | g | b | unchecked((int)0xff000000);

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
				if (dar > 1.0f)
					return (int)(BufferWidth * dar);
				else
					return BufferWidth;
			}
		}
		public int VirtualHeight
		{
			get
			{
				if (dar < 1.0f)
					return (int)(BufferHeight / dar);
				else
					return BufferHeight;
			}
		}

		public int BufferWidth { get; private set; }
		public int BufferHeight { get; private set; }
		public int BackgroundColor { get { return unchecked((int)0xff000000); } }

		#endregion

		public object GetSettings() { return null; }
		public object GetSyncSettings() { return null; }
		public bool PutSettings(object o) { return false; }
		public bool PutSyncSettings(object o) { return false; }
	}
}
