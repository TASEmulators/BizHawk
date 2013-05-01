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
		DiscSystem.Disc CD;
		GCHandle SoundHandle;

		bool Disposed = false;

		LibYabause.CDInterface.Init InitH;
		LibYabause.CDInterface.DeInit DeInitH;
		LibYabause.CDInterface.GetStatus GetStatusH;
		LibYabause.CDInterface.ReadTOC ReadTOCH;
		LibYabause.CDInterface.ReadSectorFAD ReadSectorFADH;
		LibYabause.CDInterface.ReadAheadFAD ReadAheadFADH;

		public Yabause(CoreComm CoreComm, DiscSystem.Disc CD)
		{
			CoreComm.RomStatusDetails = "Yeh";
			this.CoreComm = CoreComm;
			this.CD = CD;
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
			SoundHandle = GCHandle.Alloc(SoundBuffer, GCHandleType.Pinned);

			LibYabause.CDInterface CDInt = new LibYabause.CDInterface();
			CDInt.InitFunc = InitH = new LibYabause.CDInterface.Init(CD_Init);
			CDInt.DeInitFunc = DeInitH = new LibYabause.CDInterface.DeInit(CD_DeInit);
			CDInt.GetStatusFunc = GetStatusH = new LibYabause.CDInterface.GetStatus(CD_GetStatus);
			CDInt.ReadTOCFunc = ReadTOCH = new LibYabause.CDInterface.ReadTOC(CD_ReadTOC);
			CDInt.ReadSectorFADFunc = ReadSectorFADH = new LibYabause.CDInterface.ReadSectorFAD(CD_ReadSectorFAD);
			CDInt.ReadAheadFADFunc = ReadAheadFADH = new LibYabause.CDInterface.ReadAheadFAD(CD_ReadAheadFAD);

			if (!LibYabause.libyabause_init(ref CDInt))
				throw new Exception("libyabause_init() failed!");

			LibYabause.libyabause_setvidbuff(VideoHandle.AddrOfPinnedObject());
			LibYabause.libyabause_setsndbuff(SoundHandle.AddrOfPinnedObject());
			AttachedCore = this;

			BufferWidth = 320;
			BufferHeight = 224;
		}

		public ControllerDefinition ControllerDefinition
		{
			get { return SaturnController; }
		}

		public IController Controller { get; set; }

		public void FrameAdvance(bool render, bool rendersound = true)
		{
			int w, h, nsamp;
			LibYabause.libyabause_frameadvance(out w, out h, out nsamp);
			BufferWidth = w;
			BufferHeight = h;
			SoundNSamp = nsamp;
			Frame++;
			LagCount++;
			//Console.WriteLine(nsamp);
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
			if (!Disposed)
			{
				LibYabause.libyabause_setvidbuff(IntPtr.Zero);
				LibYabause.libyabause_setsndbuff(IntPtr.Zero);
				LibYabause.libyabause_deinit();
				VideoHandle.Free();
				SoundHandle.Free();
				Disposed = true;
			}
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

		short[] SoundBuffer = new short[44100 * 2];
		int SoundNSamp = 0;

		public void GetSamples(out short[] samples, out int nsamp)
		{
			nsamp = SoundNSamp;
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

		#region CD

		/// <summary>
		/// init cd functions
		/// </summary>
		/// <param name="unused"></param>
		/// <returns>0 on success, -1 on failure</returns>
		int CD_Init(string unused)
		{
			return 0;
		}
		/// <summary>
		/// deinit cd functions
		/// </summary>
		void CD_DeInit()
		{
		}
		/// <summary>
		/// 0 = cd present, spinning
		/// 1 = cd present, not spinning
		/// 2 = no cd
		/// 3 = tray open
		/// </summary>
		/// <returns></returns>
		int CD_GetStatus()
		{
			return 0;
		}
		/// <summary>
		/// read all TOC entries
		/// </summary>
		/// <param name="dest">place to copy to</param>
		/// <returns>number of bytes written.  should be 408 (99 tracks, 3 specials)</returns>
		int CD_ReadTOC(IntPtr dest)
		{
			// this stuff from yabause's cdbase.c.  don't ask me to explain it

			var TOC = CD.ReadTOC();
			int[] rTOC = new int[102];
			var ses = TOC.Sessions[0];
			int ntrk = ses.Tracks.Count;

			for (int i = 0; i < 99; i++)
			{
				if (i < ntrk)
				{
					var trk = ses.Tracks[i];

					uint t = (uint)trk.Indexes[1].aba;

					switch (trk.TrackType)
					{
						case DiscSystem.ETrackType.Audio:
							t |= 0x01000000;
							break;
						case DiscSystem.ETrackType.Mode1_2048:
						case DiscSystem.ETrackType.Mode1_2352:
						case DiscSystem.ETrackType.Mode2_2352:
							t |= 0x41000000;
							break;
					}
					rTOC[i] = (int)t;
				}
				else
				{
					rTOC[i] = unchecked((int)0xffffffff);
				}
			}

			rTOC[99] = (int)(rTOC[0] & 0xff000000 | 0x010000);
			rTOC[100] = (int)(rTOC[ntrk - 1] & 0xff000000 | (uint)(ntrk << 16));
			rTOC[101] = (int)(rTOC[ntrk - 1] & 0xff000000 | (uint)(ses.length_aba));

			Marshal.Copy(rTOC, 0, dest, 102);
			return 408;
		}

		/// <summary>
		/// read a sector, should be 2352 bytes
		/// </summary>
		/// <param name="FAD"></param>
		/// <param name="dest"></param>
		/// <returns></returns>
		int CD_ReadSectorFAD(int FAD, IntPtr dest)
		{
			byte[] data = new byte[2352];
			try
			{
				CD.ReadABA_2352(FAD, data, 0);
			}
			catch (Exception e)
			{
				Console.WriteLine("CD_ReadSectorFAD: Managed Exception:\n" + e.ToString());
				return 0; // failure
			}
			Marshal.Copy(data, 0, dest, 2352);
			return 1; // success
		}
		/// <summary>
		/// hint the next sector, for async loading
		/// </summary>
		/// <param name="FAD"></param>
		void CD_ReadAheadFAD(int FAD)
		{
			// ignored for now
		}

		#endregion
	}
}
