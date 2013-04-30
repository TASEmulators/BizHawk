using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Consoles.Sega.Saturn
{
	public class Yabause : IEmulator, IVideoProvider, ISyncSoundProvider
	{
		public static ControllerDefinition SaturnController = new ControllerDefinition
		{
			Name = "Saturn Controller",
			BoolButtons =
			{					
				"Up", "Down", "Left", "Right", "Start", "Z", "Y", "X", "B", "A", "L", "R"
			}
		};

		static Yabause AttachedCore = null;
		GCHandle VideoHandle;

		public Yabause(CoreComm CoreComm)
		{
			CoreComm.RomStatusDetails = "Yeh";
			this.CoreComm = CoreComm;
			Init();
		}

		void Init()
		{
			if (AttachedCore != null)
			{
				AttachedCore.Dispose();
				AttachedCore = null;
			}
			VideoHandle = GCHandle.Alloc(VideoBuffer, GCHandleType.Pinned);

			if (!LibYabause.libyabause_init())
				throw new Exception("libyabause_init() failed!");

			LibYabause.libyabause_setvidbuff(VideoHandle.AddrOfPinnedObject());
			AttachedCore = this;
		}

		public ControllerDefinition ControllerDefinition
		{
			get { return SaturnController; }
		}

		public IController Controller { get; set; }

		public void FrameAdvance(bool render, bool rendersound = true)
		{
			int w, h;
			LibYabause.libyabause_frameadvance(out w, out h);
			BufferWidth = w;
			BufferHeight = h;
			Frame++;
			LagCount++;
		}

		public int Frame
		{
			get;
			private set;
		}

		public int LagCount
		{
			get;
			set;
		}

		public bool IsLagFrame
		{
			get { return true; }
		}

		public string SystemId
		{
			get { return "SAT"; }
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

		public bool SaveRamModified
		{
			get;
			set;
		}

		public void ResetFrameCounter()
		{
			Frame = 0;
			LagCount = 0;
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
			return new byte[0];
		}

		public CoreComm CoreComm { get; private set; }

		public IList<MemoryDomain> MemoryDomains
		{
			get { throw new NotImplementedException(); }
		}

		public MemoryDomain MainMemory
		{
			get { throw new NotImplementedException(); }
		}

		public void Dispose()
		{
			LibYabause.libyabause_setvidbuff(IntPtr.Zero);
			LibYabause.libyabause_deinit();
			VideoHandle.Free();
		}

		#region IVideoProvider

		public IVideoProvider VideoProvider { get { return this; } }
		int[] VideoBuffer = new int[704 * 512];
		public int[] GetVideoBuffer() { return VideoBuffer; }
		public int VirtualWidth { get; private set; }
		public int BufferWidth { get; private set; }
		public int BufferHeight { get; private set; }
		public int BackgroundColor { get { return unchecked((int)0xff000000); } }

		#endregion

		#region ISyncSoundProvider

		short[] SoundBuffer = new short[735 * 2];

		public void GetSamples(out short[] samples, out int nsamp)
		{
			nsamp = 735;
			samples = SoundBuffer;
		}

		public void DiscardSamples()
		{
		}

		public ISoundProvider SoundProvider
		{
			get { return null; }
		}

		public ISyncSoundProvider SyncSoundProvider
		{
			get { return this; }
		}

		public bool StartAsyncSound()
		{
			return false;
		}

		public void EndAsyncSound()
		{

		}



		#endregion
	}
}
