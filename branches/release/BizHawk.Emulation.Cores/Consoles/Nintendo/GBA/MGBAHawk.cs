using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using System.Runtime.InteropServices;
using System.IO;
using System.ComponentModel;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	[CoreAttributes("mGBA", "endrift", true, false, "NOT DONE", "NOT DONE", false)]
	public class MGBAHawk : IEmulator, IVideoProvider, ISyncSoundProvider, IGBAGPUViewable, ISaveRam, IStatable, IInputPollable, ISettable<object, MGBAHawk.SyncSettings>
	{
		IntPtr core;

		[CoreConstructor("GBA")]
		public MGBAHawk(byte[] file, CoreComm comm, SyncSettings syncSettings, bool deterministic)
		{
			_syncSettings = syncSettings ?? new SyncSettings();
			DeterministicEmulation = deterministic;

			byte[] bios = comm.CoreFileProvider.GetFirmware("GBA", "Bios", false);
			DeterministicEmulation &= bios != null;

			if (DeterministicEmulation != deterministic)
			{
				throw new InvalidOperationException("A BIOS is required for deterministic recordings!");
			}
			if (!DeterministicEmulation && bios != null && !_syncSettings.RTCUseRealTime && !_syncSettings.SkipBios)
			{
				// in these situations, this core is deterministic even though it wasn't asked to be
				DeterministicEmulation = true;
			}

			if (bios != null && bios.Length != 16384)
			{
				throw new InvalidOperationException("BIOS must be exactly 16384 bytes!");
			}
			core = LibmGBA.BizCreate(bios);
			if (core == IntPtr.Zero)
			{
				throw new InvalidOperationException("BizCreate() returned NULL!  Bad BIOS?");
			}
			try
			{
				if (!LibmGBA.BizLoad(core, file, file.Length))
				{
					throw new InvalidOperationException("BizLoad() returned FALSE!  Bad ROM?");
				}

				if (!DeterministicEmulation && _syncSettings.SkipBios)
				{
					LibmGBA.BizSkipBios(core);
				}

				var ser = new BasicServiceProvider(this);
				ser.Register<IDisassemblable>(new ArmV4Disassembler());
				ser.Register<IMemoryDomains>(CreateMemoryDomains(file.Length));

				ServiceProvider = ser;
				CoreComm = comm;

				CoreComm.VsyncNum = 262144;
				CoreComm.VsyncDen = 4389;
				CoreComm.NominalWidth = 240;
				CoreComm.NominalHeight = 160;

				InitStates();
			}
			catch
			{
				LibmGBA.BizDestroy(core);
				throw;
			}
		}

		public IEmulatorServiceProvider ServiceProvider { get; private set; }
		public ControllerDefinition ControllerDefinition { get { return GBA.GBAController; } }
		public IController Controller { get; set; }

		public void FrameAdvance(bool render, bool rendersound = true)
		{
			Frame++;
			if (Controller["Power"])
				LibmGBA.BizReset(core);

			IsLagFrame = LibmGBA.BizAdvance(core, VBANext.GetButtons(Controller), videobuff, ref nsamp, soundbuff,
				RTCTime(),
				(short)Controller.GetFloat("Tilt X"),
				(short)Controller.GetFloat("Tilt Y"),
				(short)Controller.GetFloat("Tilt Z"),
				(byte)(255 - Controller.GetFloat("Light Sensor")));

			if (IsLagFrame)
				LagCount++;
			// this should be called in hblank on the appropriate line, but until we implement that, just do it here
			if (_scanlinecb != null)
				_scanlinecb();
		}

		public int Frame { get; private set; }

		public string SystemId { get { return "GBA"; } }

		public bool DeterministicEmulation { get; private set; }

		public string BoardName { get { return null; } }

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		public CoreComm CoreComm { get; private set; }

		public void Dispose()
		{
			if (core != IntPtr.Zero)
			{
				LibmGBA.BizDestroy(core);
				core = IntPtr.Zero;
			}
		}

		#region IVideoProvider
		public int VirtualWidth { get { return 240; } }
		public int VirtualHeight { get { return 160; } }
		public int BufferWidth { get { return 240; } }
		public int BufferHeight { get { return 160; } }
		public int BackgroundColor
		{
			get { return unchecked((int)0xff000000); }
		}
		public int[] GetVideoBuffer()
		{
			return videobuff;
		}
		private readonly int[] videobuff = new int[240 * 160];
		#endregion

		#region ISoundProvider
		private readonly short[] soundbuff = new short[2048];
		private int nsamp;
		public void GetSamples(out short[] samples, out int nsamp)
		{
			nsamp = this.nsamp;
			samples = soundbuff;
			Console.WriteLine(nsamp);
			DiscardSamples();
		}
		public void DiscardSamples()
		{
			nsamp = 0;
		}
		public ISoundProvider SoundProvider { get { throw new InvalidOperationException(); } }
		public ISyncSoundProvider SyncSoundProvider { get { return this; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }
		#endregion

		#region IMemoryDomains

		private MemoryDomainList CreateMemoryDomains(int romsize)
		{
			var s = new LibmGBA.MemoryAreas();
			var mm = new List<MemoryDomain>();
			LibmGBA.BizGetMemoryAreas(core, s);

			var l = MemoryDomain.Endian.Little;
			mm.Add(MemoryDomain.FromIntPtr("IWRAM", 32 * 1024, l, s.iwram, true, 4));
			mm.Add(MemoryDomain.FromIntPtr("EWRAM", 256 * 1024, l, s.wram, true, 4));
			mm.Add(MemoryDomain.FromIntPtr("BIOS", 16 * 1024, l, s.bios, false, 4));
			mm.Add(MemoryDomain.FromIntPtr("PALRAM", 1024, l, s.palram, false, 4));
			mm.Add(MemoryDomain.FromIntPtr("VRAM", 96 * 1024, l, s.vram, true, 4));
			mm.Add(MemoryDomain.FromIntPtr("OAM", 1024, l, s.oam, false, 4));
			mm.Add(MemoryDomain.FromIntPtr("ROM", romsize, l, s.rom, false, 4));

			_gpumem = new GBAGPUMemoryAreas
			{
				mmio = s.mmio,
				oam = s.oam,
				palram = s.palram,
				vram = s.vram
			};

			return new MemoryDomainList(mm);

		}

		#endregion

		private Action _scanlinecb;

		private GBAGPUMemoryAreas _gpumem;

		public GBAGPUMemoryAreas GetMemoryAreas()
		{
			return _gpumem;
		}

		[FeatureNotImplemented]
		public void SetScanlineCallback(Action callback, int scanline)
		{
			_scanlinecb = callback;
		}

		#region ISaveRam

		public byte[] CloneSaveRam()
		{
			byte[] ret = new byte[LibmGBA.BizGetSaveRamSize(core)];
			if (ret.Length > 0)
			{
				LibmGBA.BizGetSaveRam(core, ret);
				return ret;
			}
			else
			{
				return null;
			}
		}

		private static byte[] LegacyFix(byte[] saveram)
		{
			// at one point vbanext-hawk had a special saveram format which we want to load.
			var br = new BinaryReader(new MemoryStream(saveram, false));
			br.ReadBytes(8); // header;
			int flashSize = br.ReadInt32();
			int eepromsize = br.ReadInt32();
			byte[] flash = br.ReadBytes(flashSize);
			byte[] eeprom = br.ReadBytes(eepromsize);
			if (flash.Length == 0)
				return eeprom;
			else if (eeprom.Length == 0)
				return flash;
			else
			{
				// well, isn't this a sticky situation!
				return flash; // woops
			}
		}

		public void StoreSaveRam(byte[] data)
		{
			if (data.Take(8).SequenceEqual(Encoding.ASCII.GetBytes("GBABATT\0")))
			{
				data = LegacyFix(data);
			}

			int len = LibmGBA.BizGetSaveRamSize(core);
			if (len > data.Length)
			{
				byte[] _tmp = new byte[len];
				Array.Copy(data, _tmp, data.Length);
				for (int i = data.Length; i < len; i++)
					_tmp[i] = 0xff;
				data = _tmp;
			}
			else if (len < data.Length)
			{
				// we could continue from this, but we don't expect it
				throw new InvalidOperationException("Saveram will be truncated!");
			}
			LibmGBA.BizPutSaveRam(core, data);
		}

		public bool SaveRamModified
		{
			get { return LibmGBA.BizGetSaveRamSize(core) > 0; }
		}

		#endregion

		private void InitStates()
		{
			savebuff = new byte[LibmGBA.BizGetStateSize()];
			savebuff2 = new byte[savebuff.Length + 13];
		}

		private byte[] savebuff;
		private byte[] savebuff2;

		public bool BinarySaveStatesPreferred
		{
			get { return true; }
		}

		public void SaveStateText(TextWriter writer)
		{
			var tmp = SaveStateBinary();
			BizHawk.Common.BufferExtensions.BufferExtensions.SaveAsHexFast(tmp, writer);
		}
		public void LoadStateText(TextReader reader)
		{
			string hex = reader.ReadLine();
			byte[] state = new byte[hex.Length / 2];
			BizHawk.Common.BufferExtensions.BufferExtensions.ReadFromHexFast(state, hex);
			LoadStateBinary(new BinaryReader(new MemoryStream(state)));
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			LibmGBA.BizGetState(core, savebuff);
			writer.Write(savebuff.Length);
			writer.Write(savebuff);

			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			int length = reader.ReadInt32();
			if (length != savebuff.Length)
				throw new InvalidOperationException("Save buffer size mismatch!");
			reader.Read(savebuff, 0, length);
			LibmGBA.BizPutState(core, savebuff);

			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
		}

		public byte[] SaveStateBinary()
		{
			var ms = new MemoryStream(savebuff2, true);
			var bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			if (ms.Position != savebuff2.Length)
				throw new InvalidOperationException();
			ms.Close();
			return savebuff2;
		}

		public int LagCount { get; private set; }
		public bool IsLagFrame { get; private set; }

		[FeatureNotImplemented]
		public IInputCallbackSystem InputCallbacks
		{
			get { throw new NotImplementedException(); }
		}

		private long RTCTime()
		{
			if (!DeterministicEmulation && _syncSettings.RTCUseRealTime)
			{
				return (long)DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
			}
			long basetime = (long)_syncSettings.RTCInitialTime.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
			long increment = Frame * 4389L >> 18;
			return basetime + increment;
		}

		public object GetSettings()
		{
			return null;
		}

		public SyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public bool PutSettings(object o)
		{
			return false;
		}

		public bool PutSyncSettings(SyncSettings o)
		{
			bool ret = SyncSettings.NeedsReboot(o, _syncSettings);
			_syncSettings = o;
			return ret;
		}

		private SyncSettings _syncSettings;

		public class SyncSettings
		{
			[DisplayName("Skip BIOS")]
			[Description("Skips the BIOS intro.  Not applicable when a BIOS is not provided.")]
			[DefaultValue(true)]
			public bool SkipBios { get; set; }

			[DisplayName("RTC Use Real Time")]
			[Description("Causes the internal clock to reflect your system clock.  Only relevant when a game has an RTC chip.  Forced to false for movie recording.")]
			[DefaultValue(true)]
			public bool RTCUseRealTime { get; set; }

			[DisplayName("RTC Initial Time")]
			[Description("The initial time of emulation.  Only relevant when a game has an RTC chip and \"RTC Use Real Time\" is false.")]
			[DefaultValue(typeof(DateTime), "2010-01-01")]
			public DateTime RTCInitialTime { get; set; }

			public SyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}

			public SyncSettings Clone()
			{
				return (SyncSettings)MemberwiseClone();
			}
		}
	}
}
