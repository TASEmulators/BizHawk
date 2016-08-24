using System;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	[CoreAttributes(
		"Genplus-gx",
		"",
		isPorted: true,
		isReleased: true,
		portedVersion: "r874",
		portedUrl: "https://code.google.com/p/genplus-gx/",
		singleInstance: true
		)]
	public partial class GPGX : IEmulator, ISyncSoundProvider, IVideoProvider, ISaveRam, IStatable, IRegionable,
		IInputPollable, IDebuggable, IDriveLight, ICodeDataLogger, IDisassemblable
    {
		static GPGX AttachedCore = null;

		DiscSystem.Disc CD;
		DiscSystem.DiscSectorReader DiscSectorReader;
		byte[] romfile;

		bool disposed = false;

		LibGPGX.load_archive_cb LoadCallback = null;

		LibGPGX.InputData input = new LibGPGX.InputData();

		public enum ControlType
		{
			None,
			OnePlayer,
			Normal,
			Xea1p,
			Activator,
			Teamplayer,
			Wayplay,
			Mouse
		};

		[CoreConstructor("GEN")]
		public GPGX(CoreComm comm, byte[] file, object Settings, object SyncSettings)
			: this(comm, file, null, Settings, SyncSettings)
		{
		}

		public GPGX(CoreComm comm, byte[] rom, DiscSystem.Disc CD, object Settings, object SyncSettings)
		{
			ServiceProvider = new BasicServiceProvider(this);
			// this can influence some things internally
			string romextension = "GEN";

			// three or six button?
			// http://www.sega-16.com/forum/showthread.php?4398-Forgotten-Worlds-giving-you-GAME-OVER-immediately-Fix-inside&highlight=forgotten%20worlds

			//hack, don't use
			if (rom != null && rom.Length > 32 * 1024 * 1024)
			{
				throw new InvalidOperationException("ROM too big!  Did you try to load a CD as a ROM?");
			}

			try
			{
				_syncSettings = (GPGXSyncSettings)SyncSettings ?? new GPGXSyncSettings();
				_settings = (GPGXSettings)Settings ?? new GPGXSettings();

				CoreComm = comm;
				if (AttachedCore != null)
				{
					AttachedCore.Dispose();
					AttachedCore = null;
				}
				AttachedCore = this;

				LoadCallback = new LibGPGX.load_archive_cb(load_archive);

				this.romfile = rom;
				this.CD = CD;
				this.DiscSectorReader = new DiscSystem.DiscSectorReader(CD);

				LibGPGX.INPUT_SYSTEM system_a = LibGPGX.INPUT_SYSTEM.SYSTEM_NONE;
				LibGPGX.INPUT_SYSTEM system_b = LibGPGX.INPUT_SYSTEM.SYSTEM_NONE;

				switch (_syncSettings.ControlType)
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
					case ControlType.Mouse:
						system_a = LibGPGX.INPUT_SYSTEM.SYSTEM_MD_GAMEPAD;
						// seems like mouse in port 1 would be supported, but not both at the same time
						system_b = LibGPGX.INPUT_SYSTEM.SYSTEM_MOUSE;
						break;
				}


				if (!LibGPGX.gpgx_init(romextension, LoadCallback, _syncSettings.UseSixButton, system_a, system_b, _syncSettings.Region, _settings.GetNativeSettings()))
					throw new Exception("gpgx_init() failed");

				{
					int fpsnum = 60;
					int fpsden = 1;
					LibGPGX.gpgx_get_fps(ref fpsnum, ref fpsden);
					CoreComm.VsyncNum = fpsnum;
					CoreComm.VsyncDen = fpsden;
					Region = CoreComm.VsyncRate > 55 ? DisplayType.NTSC : DisplayType.PAL;
				}

				// compute state size
				{
					byte[] tmp = new byte[LibGPGX.gpgx_state_max_size()];
					int size = LibGPGX.gpgx_state_size(tmp, tmp.Length);
					if (size <= 0)
						throw new Exception("Couldn't Determine GPGX internal state size!");
					_savebuff = new byte[size];
					_savebuff2 = new byte[_savebuff.Length + 13];
					Console.WriteLine("GPGX Internal State Size: {0}", size);
				}

				SetControllerDefinition();

				// pull the default video size from the core
				UpdateVideoInitial();

				SetMemoryDomains();

				InputCallback = new LibGPGX.input_cb(input_callback);
				LibGPGX.gpgx_set_input_callback(InputCallback);

				if (CD != null)
					DriveLightEnabled = true;

				// process the non-init settings now
				PutSettings(_settings);

				//TODO - this hits performance, we need to make it controllable
				CDCallback = new LibGPGX.CDCallback(CDCallbackProc);

				InitMemCallbacks();
				KillMemCallbacks();

				ConnectTracer();
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
					// this path will be the most common PEBKAC error, so be a bit more vocal about the problem
					srcdata = CoreComm.CoreFileProvider.GetFirmware("GEN", firmwareID, false, "GPGX firmwares are usually required.");
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
				if (lba < CD.Session1.LeadoutLBA)
				{
					DiscSectorReader.ReadLBA_2352(lba, data, 0);
				}
				else
				{
					// audio seems to read slightly past the end of disks; probably innoculous
					// just send back 0s.
					// Console.WriteLine("!!{0} >= {1}", lba, CD.LBACount);
				}
				Marshal.Copy(data, 0, dest, 2352);
			}
			else
			{
				byte[] data = new byte[2048];
				DiscSectorReader.ReadLBA_2048(lba, data, 0);
				Marshal.Copy(data, 0, dest, 2048);
				_drivelight = true;
			}
		}

		LibGPGX.cd_read_cb cd_callback_handle;

		unsafe byte[] GetCDData()
		{
			LibGPGX.CDData ret = new LibGPGX.CDData();
			int size = Marshal.SizeOf(ret);

			ret.readcallback = cd_callback_handle = new LibGPGX.cd_read_cb(CDRead);

			var ses = CD.Session1;
			int ntrack = ses.InformationTrackCount;

			// bet you a dollar this is all wrong
			//zero 07-jul-2015 - throws a dollar in the pile, since he probably messed it up worse
			for (int i = 0; i < LibGPGX.CD_MAX_TRACKS; i++)
			{
				if (i < ntrack)
				{
					ret.tracks[i].start = ses.Tracks[i + 1].LBA;
					ret.tracks[i].end = ses.Tracks[i + 2].LBA;
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

		/// <summary>
		/// size of native input struct
		/// </summary>
		int inputsize;

		GPGXControlConverter ControlConverter;

		private void SetControllerDefinition()
		{
			inputsize = Marshal.SizeOf(typeof(LibGPGX.InputData));
			if (!LibGPGX.gpgx_get_control(input, inputsize))
				throw new Exception("gpgx_get_control() failed");

			ControlConverter = new GPGXControlConverter(input);
			ControllerDefinition = ControlConverter.ControllerDef;
		}

		public LibGPGX.INPUT_DEVICE[] GetDevices()
		{
			return (LibGPGX.INPUT_DEVICE[])input.dev.Clone();
		}

		public bool IsSegaCD { get { return CD != null; } }

		public void UpdateVDPViewContext(LibGPGX.VDPView view)
		{
			LibGPGX.gpgx_get_vdp_view(view);
			LibGPGX.gpgx_flush_vram(); // fully regenerate internal caches as needed
		}

		short[] samples = new short[4096];
		int nsamp = 0;

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

		public DisplayType Region { get; private set; }
	}
}
