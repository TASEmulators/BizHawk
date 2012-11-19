using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace BizHawk.Emulation.Consoles.Nintendo.GBA
{
	public class GBA : IEmulator, IVideoProvider, ISyncSoundProvider
	{
		public static readonly ControllerDefinition GBAController =
		new ControllerDefinition
		{
			Name = "GBA Controller",
			BoolButtons =
			{					
				"Up", "Down", "Left", "Right", "Select", "Start", "B", "A", "L", "R"//, "Reset", "Power",		
			}
		};
		public ControllerDefinition ControllerDefinition { get { return GBAController; } }
		public IController Controller { get; set; }

		public void Load(byte[] rom)
		{
			Init();
			LibMeteor.libmeteor_reset();
			LibMeteor.libmeteor_loadbios(File.ReadAllBytes("gbabios.rom"), 16384);
			LibMeteor.libmeteor_loadrom(rom, (uint)rom.Length);
		}

		public void FrameAdvance(bool render, bool rendersound = true)
		{
			LibMeteor.libmeteor_frameadvance();
		}

		public int Frame
		{
			get { return 0; }
		}

		public int LagCount
		{
			get;
			set;
		}

		public bool IsLagFrame
		{
			get { return false; }
		}

		public string SystemId
		{
			get { return "GBA"; }
		}

		public bool DeterministicEmulation
		{
			get { return true; }
		}

		public byte[] ReadSaveRam()
		{
			return new byte[0];
		}

		public void StoreSaveRam(byte[] data)
		{
		}

		public void ClearSaveRam()
		{
		}

		public bool SaveRamModified { get { return false; } set { } }

		public void ResetFrameCounter()
		{
		}

		public void SaveStateText(System.IO.TextWriter writer)
		{
		}

		public void LoadStateText(System.IO.TextReader reader)
		{
		}

		public void SaveStateBinary(System.IO.BinaryWriter writer)
		{
		}

		public void LoadStateBinary(System.IO.BinaryReader reader)
		{
		}

		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		public CoreInputComm CoreInputComm { get; set; }

		CoreOutputComm _CoreOutputComm = new CoreOutputComm
		{
			VsyncNum = 262144,
			VsyncDen = 4389
		};

		public CoreOutputComm CoreOutputComm { get { return _CoreOutputComm; } }

		public IList<MemoryDomain> MemoryDomains
		{
			get { return null; }
		}

		public MemoryDomain MainMemory
		{
			get { return null; }
		}

		static GBA attachedcore;

		void Init()
		{
			if (attachedcore != null)
				attachedcore.Dispose();

			LibMeteor.libmeteor_init();
			videobuffer = new int[240 * 160];
			videohandle = GCHandle.Alloc(videobuffer, GCHandleType.Pinned);
			soundbuffer = new short[2048];
			soundhandle = GCHandle.Alloc(soundbuffer, GCHandleType.Pinned);

			if (!LibMeteor.libmeteor_setbuffers
				(videohandle.AddrOfPinnedObject(), (uint)(sizeof(int) * videobuffer.Length),
				soundhandle.AddrOfPinnedObject(), (uint)(sizeof(short) * soundbuffer.Length)))
				throw new Exception("libmeteor_setbuffers() returned false!");

			attachedcore = this;
		}

		bool disposed = false;
		public void Dispose()
		{
			if (!disposed)
			{
				disposed = true;
				videohandle.Free();
				soundhandle.Free();
			}
		}

		#region IVideoProvider

		public IVideoProvider VideoProvider { get { return this; } }

		int[] videobuffer;
		GCHandle videohandle;

		public int[] GetVideoBuffer() { return videobuffer; }
		public int VirtualWidth { get { return 240; } }
		public int BufferWidth { get { return 240; } }
		public int BufferHeight { get { return 160; } }
		public int BackgroundColor { get { return unchecked((int)0xff000000); } }

		#endregion

		#region ISoundProvider

		short[] soundbuffer;
		GCHandle soundhandle;

		public ISoundProvider SoundProvider { get { return null; } }
		public ISyncSoundProvider SyncSoundProvider { get { return this; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }

		public void GetSamples(out short[] samples, out int nsamp)
		{
			uint nbytes = LibMeteor.libmeteor_emptysound();
			samples = soundbuffer;
			nsamp = (int)(nbytes / 4);

		}

		public void DiscardSamples()
		{
			LibMeteor.libmeteor_emptysound();
		}

		#endregion
	}
}
