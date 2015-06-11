using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using System.Runtime.InteropServices;
using System.IO;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	[CoreAttributes("mGBA", "endrift", true, false, "NOT DONE", "NOT DONE", false)]
	public class MGBAHawk : IEmulator, IVideoProvider, ISyncSoundProvider, IGBAGPUViewable, ISaveRam, IStatable, IInputPollable
	{
		IntPtr core;

		[CoreConstructor("GBA")]
		public MGBAHawk(byte[] file, CoreComm comm)
		{
			byte[] bios = null;
			if (true) // TODO: config me
			{
				bios = comm.CoreFileProvider.GetFirmware("GBA", "Bios", true);
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

				var ser = new BasicServiceProvider(this);
				ser.Register<IDisassemblable>(new ArmV4Disassembler());
				ser.Register<IMemoryDomains>(CreateMemoryDomains(file.Length));

				ServiceProvider = ser;
				CoreComm = comm;

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
				0, // TODO RTC hookup
				(short)Controller.GetFloat("Tilt X"),
				(short)Controller.GetFloat("Tilt Y"),
				(short)Controller.GetFloat("Tilt Z"),
				(byte)(255 - Controller.GetFloat("Light Sensor")));
			if (IsLagFrame)
				_lagCount++;
		}

		public int Frame { get; private set; }

		public string SystemId { get { return "GBA"; } }

		public bool DeterministicEmulation { get { return true; } }

		public string BoardName { get { return null; } }

		public void ResetCounters()
		{
			Frame = 0;
			_lagCount = 0;
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

		private GBAGPUMemoryAreas _gpumem;

		public GBAGPUMemoryAreas GetMemoryAreas()
		{
			return _gpumem;
		}

		[FeatureNotImplemented]
		public void SetScanlineCallback(Action callback, int scanline)
		{
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
			_lagCount = reader.ReadInt32();
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

		public int LagCount
		{
			get { return _lagCount; }
		}

		private int _lagCount;

		public bool IsLagFrame { get; private set; }

		[FeatureNotImplemented]
		public IInputCallbackSystem InputCallbacks
		{
			get { throw new NotImplementedException(); }
		}
	}
}
