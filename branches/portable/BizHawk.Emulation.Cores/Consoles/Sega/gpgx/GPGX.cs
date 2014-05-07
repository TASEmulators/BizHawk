using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Common;
using BizHawk.Emulation.Common;

using System.Runtime.InteropServices;

using System.IO;

using System.ComponentModel;


namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	[CoreAttributes(
		"Genplus-gx",
		"ekeeke31",
		isPorted: true,
		isReleased: true
		)]
	public class GPGX : IEmulator, ISyncSoundProvider, IVideoProvider
	{
		static GPGX AttachedCore = null;

		DiscSystem.Disc CD;
		byte[] romfile;
		bool drivelight;

		bool disposed = false;

		LibGPGX.load_archive_cb LoadCallback = null;
		LibGPGX.input_cb InputCallback = null;

		LibGPGX.InputData input = new LibGPGX.InputData();

		// still working out what all the possibilities are here
		public enum ControlType
		{
			None,
			OnePlayer,
			Normal,
			Xea1p,
			Activator,
			Teamplayer,
			Wayplay
		};

		public GPGX(CoreComm NextComm, byte[] romfile, DiscSystem.Disc CD, string romextension, object SyncSettings)
		{
			// three or six button?
			// http://www.sega-16.com/forum/showthread.php?4398-Forgotten-Worlds-giving-you-GAME-OVER-immediately-Fix-inside&highlight=forgotten%20worlds

			//hack, don't use
			//romfile = File.ReadAllBytes(@"D:\encodes\bizhawksrc\output\SANIC CD\PierSolar (E).bin");
			if (romfile != null && romfile.Length > 16 * 1024 * 1024)
			{
				throw new InvalidOperationException("ROM too big!  Did you try to load a CD as a ROM?");
			}

			try
			{
				this.SyncSettings = (GPGXSyncSettings)SyncSettings ?? GPGXSyncSettings.GetDefaults();

				CoreComm = NextComm;
				if (AttachedCore != null)
				{
					AttachedCore.Dispose();
					AttachedCore = null;
				}
				AttachedCore = this;

				LoadCallback = new LibGPGX.load_archive_cb(load_archive);

				this.romfile = romfile;
				this.CD = CD;

				LibGPGX.INPUT_SYSTEM system_a = LibGPGX.INPUT_SYSTEM.SYSTEM_NONE;
				LibGPGX.INPUT_SYSTEM system_b = LibGPGX.INPUT_SYSTEM.SYSTEM_NONE;

				switch (this.SyncSettings.ControlType)
				{
					case ControlType.None:
					default:
						break;
					case ControlType.Activator:
						system_a = LibGPGX.INPUT_SYSTEM.SYSTEM_ACTIVATOR;
						system_b = LibGPGX.INPUT_SYSTEM.SYSTEM_ACTIVATOR;
						break;
					case ControlType.Normal:
						system_a = LibGPGX.INPUT_SYSTEM.SYSTEM_MD_GAMEPAD;
						system_b = LibGPGX.INPUT_SYSTEM.SYSTEM_MD_GAMEPAD;
						break;
					case ControlType.OnePlayer:
						system_a = LibGPGX.INPUT_SYSTEM.SYSTEM_MD_GAMEPAD;
						break;
					case ControlType.Xea1p:
						system_a = LibGPGX.INPUT_SYSTEM.SYSTEM_XE_A1P;
						break;
					case ControlType.Teamplayer:
						system_a = LibGPGX.INPUT_SYSTEM.SYSTEM_TEAMPLAYER;
						system_b = LibGPGX.INPUT_SYSTEM.SYSTEM_TEAMPLAYER;
						break;
					case ControlType.Wayplay:
						system_a = LibGPGX.INPUT_SYSTEM.SYSTEM_WAYPLAY;
						system_b = LibGPGX.INPUT_SYSTEM.SYSTEM_WAYPLAY;
						break;
				}


				if (!LibGPGX.gpgx_init(romextension, LoadCallback, this.SyncSettings.UseSixButton, system_a, system_b, this.SyncSettings.Region))
					throw new Exception("gpgx_init() failed");

				{
					int fpsnum = 60;
					int fpsden = 1;
					LibGPGX.gpgx_get_fps(ref fpsnum, ref fpsden);
					CoreComm.VsyncNum = fpsnum;
					CoreComm.VsyncDen = fpsden;
					DisplayType = CoreComm.VsyncRate > 55 ? DisplayType.NTSC : DisplayType.PAL;
				}

				// compute state size
				{
					byte[] tmp = new byte[LibGPGX.gpgx_state_max_size()];
					int size = LibGPGX.gpgx_state_size(tmp, tmp.Length);
					if (size <= 0)
						throw new Exception("Couldn't Determine GPGX internal state size!");
					savebuff = new byte[size];
					savebuff2 = new byte[savebuff.Length + 13];
					Console.WriteLine("GPGX Internal State Size: {0}", size);
				}

				SetControllerDefinition();

				// pull the default video size from the core
				update_video();

				SetMemoryDomains();

				InputCallback = new LibGPGX.input_cb(input_callback);
				LibGPGX.gpgx_set_input_callback(InputCallback);

				if (CD != null)
					CoreComm.UsesDriveLed = true;
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		/// <summary>
		/// core callback for file loading
		/// </summary>
		/// <param name="filename">string identifying file to be loaded</param>
		/// <param name="buffer">buffer to load file to</param>
		/// <param name="maxsize">maximum length buffer can hold</param>
		/// <returns>actual size loaded, or 0 on failure</returns>
		int load_archive(string filename, IntPtr buffer, int maxsize)
		{
			byte[] srcdata = null;

			if (buffer == IntPtr.Zero)
			{
				Console.WriteLine("Couldn't satisfy firmware request {0} because buffer == NULL", filename);
				return 0;
			}

			if (filename == "PRIMARY_ROM")
			{
				if (romfile == null)
				{
					Console.WriteLine("Couldn't satisfy firmware request PRIMARY_ROM because none was provided.");
					return 0;
				}
				srcdata = romfile;
			}
			else if (filename == "PRIMARY_CD" || filename == "SECONDARY_CD")
			{
				if (filename == "PRIMARY_CD" && romfile != null)
				{
					Console.WriteLine("Declined to satisfy firmware request PRIMARY_CD because PRIMARY_ROM was provided.");
					return 0;
				}
				else
				{
					if (CD == null)
					{
						Console.WriteLine("Couldn't satisfy firmware request {0} because none was provided.", filename);
						return 0;
					}
					srcdata = GetCDData();
					if (srcdata.Length != maxsize)
					{
						Console.WriteLine("Couldn't satisfy firmware request {0} because of struct size.", filename);
						return 0;
					}
				}
			}
			else
			{
				// use fromtend firmware interface

				string firmwareID = null;
				switch (filename)
				{
					case "CD_BIOS_EU": firmwareID = "CD_BIOS_EU"; break;
					case "CD_BIOS_JP": firmwareID = "CD_BIOS_JP"; break;
					case "CD_BIOS_US": firmwareID = "CD_BIOS_US"; break;
					default:
						break;
				}
				if (firmwareID != null)
				{
					srcdata = CoreComm.CoreFileProvider.GetFirmware("GEN", firmwareID, false);
					if (srcdata == null)
					{
						Console.WriteLine("Frontend couldn't satisfy firmware request GEN:{0}", firmwareID);
						return 0;
					}
				}
				else
				{
					Console.WriteLine("Unrecognized firmware request {0}", filename);
					return 0;
				}
			}

			if (srcdata != null)
			{
				if (srcdata.Length > maxsize)
				{
					Console.WriteLine("Couldn't satisfy firmware request {0} because {1} > {2}", filename, srcdata.Length, maxsize);
					return 0;
				}
				else
				{
					Marshal.Copy(srcdata, 0, buffer, srcdata.Length);
					Console.WriteLine("Firmware request {0} satisfied at size {1}", filename, srcdata.Length);
					return srcdata.Length;
				}
			}
			else
			{
				throw new Exception();
				//Console.WriteLine("Couldn't satisfy firmware request {0} for unknown reasons", filename);
				//return 0;
			}

		}

		void CDRead(int lba, IntPtr dest, bool audio)
		{
			if (audio)
			{
				byte[] data = new byte[2352];
				CD.ReadLBA_2352(lba, data, 0);
				Marshal.Copy(data, 0, dest, 2352);
			}
			else
			{
				byte[] data = new byte[2048];
				CD.ReadLBA_2048(lba, data, 0);
				Marshal.Copy(data, 0, dest, 2048);
				drivelight = true;
			}
		}

		LibGPGX.cd_read_cb cd_callback_handle;

		unsafe byte[] GetCDData()
		{
			LibGPGX.CDData ret = new LibGPGX.CDData();
			int size = Marshal.SizeOf(ret);

			ret.readcallback = cd_callback_handle = new LibGPGX.cd_read_cb(CDRead);

			var ses = CD.TOC.Sessions[0];
			int ntrack = ses.Tracks.Count;

			// bet you a dollar this is all wrong
			for (int i = 0; i < LibGPGX.CD_MAX_TRACKS; i++)
			{
				if (i < ntrack)
				{
					ret.tracks[i].start = ses.Tracks[i].Indexes[1].aba - 150;
					ret.tracks[i].end = ses.Tracks[i].length_aba + ret.tracks[i].start;
					if (i == ntrack - 1)
					{
						ret.end = ret.tracks[i].end;
						ret.last = ntrack;
					}
				}
				else
				{
					ret.tracks[i].start = 0;
					ret.tracks[i].end = 0;
				}
			}

			byte[] retdata = new byte[size];

			fixed (byte* p = &retdata[0])
			{
				Marshal.StructureToPtr(ret, (IntPtr)p, false);
			}
			return retdata;
		}


		#region controller

		/// <summary>
		/// size of native input struct
		/// </summary>
		int inputsize;

		GPGXControlConverter ControlConverter;

		public ControllerDefinition ControllerDefinition { get; private set; }
		public IController Controller { get; set; }

		void SetControllerDefinition()
		{
			inputsize = Marshal.SizeOf(typeof(LibGPGX.InputData));
			if (!LibGPGX.gpgx_get_control(input, inputsize))
				throw new Exception("gpgx_get_control() failed");

			ControlConverter = new GPGXControlConverter(input);
			ControllerDefinition = ControlConverter.ControllerDef;
		}

		// core callback for input
		void input_callback()
		{
			CoreComm.InputCallback.Call();
			IsLagFrame = false;
		}

		#endregion

		// TODO: use render and rendersound
		public void FrameAdvance(bool render, bool rendersound = true)
		{
			if (Controller["Reset"])
				LibGPGX.gpgx_reset(false);
			if (Controller["Power"])
				LibGPGX.gpgx_reset(true);

			// do we really have to get each time?  nothing has changed
			if (!LibGPGX.gpgx_get_control(input, inputsize))
				throw new Exception("gpgx_get_control() failed!");

			ControlConverter.Convert(Controller, input);

			if (!LibGPGX.gpgx_put_control(input, inputsize))
				throw new Exception("gpgx_put_control() failed!");

			IsLagFrame = true;
			Frame++;
			drivelight = false;
			LibGPGX.gpgx_advance();
			update_video();
			update_audio();

			if (IsLagFrame)
				LagCount++;

			if (CD != null)
				CoreComm.DriveLED = drivelight;
		}

		public int Frame { get; private set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; private set; }

		public string SystemId { get { return "GEN"; } }
		public bool DeterministicEmulation { get { return true; } }
		public string BoardName { get { return null; } }

		public CoreComm CoreComm { get; private set; }

		#region saveram

		byte[] DisposedSaveRam = null;

		public byte[] ReadSaveRam()
		{
			if (disposed)
			{
				return DisposedSaveRam ?? new byte[0];
			}
			else
			{
				int size = 0;
				IntPtr area = IntPtr.Zero;
				LibGPGX.gpgx_get_sram(ref area, ref size);
				if (size <= 0 || area == IntPtr.Zero)
					return new byte[0];
				LibGPGX.gpgx_sram_prepread();

				byte[] ret = new byte[size];
				Marshal.Copy(area, ret, 0, size);
				return ret;
			}
		}

		public void StoreSaveRam(byte[] data)
		{
			if (disposed)
			{
				throw new ObjectDisposedException(typeof(GPGX).ToString());
			}
			else
			{
				int size = 0;
				IntPtr area = IntPtr.Zero;
				LibGPGX.gpgx_get_sram(ref area, ref size);
				if (size <= 0 || area == IntPtr.Zero)
					return;
				if (size != data.Length)
					throw new Exception("Unexpected saveram size");

				Marshal.Copy(data, 0, area, size);
				LibGPGX.gpgx_sram_commitwrite();
			}
		}

		public void ClearSaveRam()
		{
			if (disposed)
			{
				throw new ObjectDisposedException(typeof(GPGX).ToString());
			}
			else
			{
				LibGPGX.gpgx_clear_sram();
			}
		}

		public bool SaveRamModified
		{
			get
			{
				if (disposed)
				{
					return DisposedSaveRam != null;
				}
				else
				{
					int size = 0;
					IntPtr area = IntPtr.Zero;
					LibGPGX.gpgx_get_sram(ref area, ref size);
					return size > 0 && area != IntPtr.Zero;
				}
			}
			set
			{
				throw new Exception();
			}
		}

		#endregion

		public void ResetCounters()
		{
			Frame = 0;
			IsLagFrame = false;
			LagCount = 0;
		}

		#region savestates

		private byte[] savebuff;
		private byte[] savebuff2;

		public void SaveStateText(System.IO.TextWriter writer)
		{
			var temp = SaveStateBinary();
			temp.SaveAsHexFast(writer);
			// write extra copy of stuff we don't use
			writer.WriteLine("Frame {0}", Frame);
		}

		public void LoadStateText(System.IO.TextReader reader)
		{
			string hex = reader.ReadLine();
			byte[] state = new byte[hex.Length / 2];
			state.ReadFromHexFast(hex);
			LoadStateBinary(new System.IO.BinaryReader(new System.IO.MemoryStream(state)));
		}

		public void SaveStateBinary(System.IO.BinaryWriter writer)
		{
			if (!LibGPGX.gpgx_state_save(savebuff, savebuff.Length))
				throw new Exception("gpgx_state_save() returned false");

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
			if (newlen != savebuff.Length)
				throw new Exception("Unexpected state size");
			reader.Read(savebuff, 0, savebuff.Length);
			if (!LibGPGX.gpgx_state_load(savebuff, savebuff.Length))
				throw new Exception("gpgx_state_load() returned false");
			// other variables
			Frame = reader.ReadInt32();
			LagCount = reader.ReadInt32();
			IsLagFrame = reader.ReadBoolean();
			update_video();
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

		#region debugging tools

		public MemoryDomainList MemoryDomains { get; private set; }

		unsafe void SetMemoryDomains()
		{
			var mm = new List<MemoryDomain>();
			for (int i = LibGPGX.MIN_MEM_DOMAIN; i <= LibGPGX.MAX_MEM_DOMAIN; i++)
			{
				IntPtr area = IntPtr.Zero;
				int size = 0;
				IntPtr pname = LibGPGX.gpgx_get_memdom(i, ref area, ref size);
				if (area == IntPtr.Zero || pname == IntPtr.Zero || size == 0)
					continue;
				string name = Marshal.PtrToStringAnsi(pname);
				byte* p = (byte*)area;

				mm.Add(new MemoryDomain(name, size, MemoryDomain.Endian.Unknown,
					delegate(int addr)
					{
						if (addr < 0 || addr >= size)
							throw new ArgumentOutOfRangeException();
						return p[addr];
					},
					delegate(int addr, byte val)
					{
						if (addr < 0 || addr >= size)
							throw new ArgumentOutOfRangeException();
						p[addr] = val;
					}));
			}

			MemoryDomains = new MemoryDomainList(mm, 0);
		}


		public Dictionary<string, int> GetCpuFlagsAndRegisters()
		{
			LibGPGX.RegisterInfo[] regs = new LibGPGX.RegisterInfo[LibGPGX.gpgx_getmaxnumregs()];

			int n = LibGPGX.gpgx_getregs(regs);
			if (n > regs.Length)
				throw new InvalidOperationException("A buffer overrun has occured!");
			var ret = new Dictionary<string, int>();
			for (int i = 0; i < n; i++)
				ret[Marshal.PtrToStringAnsi(regs[i].Name)] = regs[i].Value;
			return ret;
		}

		public void UpdateVDPViewContext(LibGPGX.VDPView view)
		{
			LibGPGX.gpgx_get_vdp_view(view);
		}

		#endregion

		public void Dispose()
		{
			if (!disposed)
			{
				if (AttachedCore != this)
					throw new Exception();
				if (SaveRamModified)
					DisposedSaveRam = ReadSaveRam();
				AttachedCore = null;
				disposed = true;
			}
		}

		#region SoundProvider

		short[] samples = new short[4096];
		int nsamp = 0;

		public ISoundProvider SoundProvider { get { return null; } }
		public ISyncSoundProvider SyncSoundProvider { get { return this; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }

		public void GetSamples(out short[] samples, out int nsamp)
		{
			nsamp = this.nsamp;
			samples = this.samples;
			this.nsamp = 0;
		}

		public void DiscardSamples()
		{
			this.nsamp = 0;
		}

		void update_audio()
		{
			IntPtr src = IntPtr.Zero;
			LibGPGX.gpgx_get_audio(ref nsamp, ref src);
			if (src != IntPtr.Zero)
			{
				Marshal.Copy(src, samples, 0, nsamp * 2);
			}
		}

		#endregion

		#region VideoProvider

		public DisplayType DisplayType { get; private set; }

		public IVideoProvider VideoProvider { get { return this; } }

		int[] vidbuff = new int[0];
		int vwidth;
		int vheight;
		public int[] GetVideoBuffer() { return vidbuff; }
		public int VirtualWidth { get { return BufferWidth; } } // TODO
		public int VirtualHeight { get { return BufferHeight; } } // TODO
		public int BufferWidth { get { return vwidth; } }
		public int BufferHeight { get { return vheight; } }
		public int BackgroundColor { get { return unchecked((int)0xff000000); } }

		unsafe void update_video()
		{
			int pitch = 0;
			IntPtr src = IntPtr.Zero;

			LibGPGX.gpgx_get_video(ref vwidth, ref vheight, ref pitch, ref src);

			if (vidbuff.Length < vwidth * vheight)
				vidbuff = new int[vwidth * vheight];

			int rinc = (pitch / 4) - vwidth;
			fixed (int* pdst_ = &vidbuff[0])
			{
				int* pdst = pdst_;
				int* psrc = (int*)src;

				for (int j = 0; j < vheight; j++)
				{
					for (int i = 0; i < vwidth; i++)
						*pdst++ = *psrc++;// | unchecked((int)0xff000000);
					psrc += rinc;
				}
			}
		}

		#endregion

		#region Settings

		GPGXSyncSettings SyncSettings;

		public object GetSettings() { return null; }
		public object GetSyncSettings() { return SyncSettings.Clone(); }
		public bool PutSettings(object o) { return false; }
		public bool PutSyncSettings(object o)
		{
			bool ret;
			var n = (GPGXSyncSettings)o;
			ret = GPGXSyncSettings.NeedsReboot(SyncSettings, n);
			SyncSettings = n;
			return ret;
		}

		public class GPGXSyncSettings
		{
			[Description("Controls the type of any attached normal controllers; six button controllers are used if true, otherwise three button controllers.  Some games don't work correctly with six button controllers.  Not relevant if other controller types are connected.")]
			[DefaultValue(true)]
			public bool UseSixButton { get; set; }
			[Description("Sets the type of controls that are plugged into the console.  Some games will automatically load with a different control type.")]
			[DefaultValue(ControlType.Normal)]
			public ControlType ControlType { get; set; }
			[Description("Sets the region of the emulated console.  Many games can run on multiple regions and will behave differently on different ones.  Some games may require a particular region.")]
			[DefaultValue(LibGPGX.Region.Autodetect)]
			public LibGPGX.Region Region { get; set; }

			public GPGXSyncSettings()
			{
				UseSixButton = true;
				ControlType = ControlType.Normal;
				Region = LibGPGX.Region.Autodetect;
			}

			public static GPGXSyncSettings GetDefaults()
			{
				return new GPGXSyncSettings();
			}

			public GPGXSyncSettings Clone()
			{
				return (GPGXSyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(GPGXSyncSettings x, GPGXSyncSettings y)
			{
				return x.UseSixButton != y.UseSixButton || x.ControlType != y.ControlType || x.Region != y.Region;
			}
		}

		#endregion
	}
}
