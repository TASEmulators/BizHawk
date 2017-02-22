using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.ComponentModel;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.DiscSystem;

using Newtonsoft.Json;

namespace BizHawk.Emulation.Cores.Sega.Saturn
{
	[CoreAttributes(
		"Yabause",
		"",
		isPorted: true,
		isReleased: true,
		portedVersion: "9.12",
		portedUrl: "http://yabause.org",
		singleInstance: true
		)]
	public partial class Yabause : IEmulator, IVideoProvider, ISoundProvider, ISaveRam, IStatable, IInputPollable,
		ISettable<object, Yabause.SaturnSyncSettings>, IDriveLight
	{
		public static ControllerDefinition SaturnController = new ControllerDefinition
		{
			Name = "Saturn Controller",
			BoolButtons =
			{	
				"Power", "Reset",
				"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Start", "P1 A", "P1 B", "P1 C", "P1 X", "P1 Y", "P1 Z", "P1 L", "P1 R",
				"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Start", "P2 A", "P2 B", "P2 C", "P2 X", "P2 Y", "P2 Z", "P2 L", "P2 R",
			}
		};

		static Yabause AttachedCore = null;
		GCHandle VideoHandle;
		Disc CD;
		DiscSectorReader DiscSectorReader;
		GCHandle SoundHandle;

		bool Disposed = false;
		byte[] DisposedSaveRam;

		LibYabause.CDInterface.Init InitH;
		LibYabause.CDInterface.DeInit DeInitH;
		LibYabause.CDInterface.GetStatus GetStatusH;
		LibYabause.CDInterface.ReadTOC ReadTOCH;
		LibYabause.CDInterface.ReadSectorFAD ReadSectorFADH;
		LibYabause.CDInterface.ReadAheadFAD ReadAheadFADH;

		LibYabause.InputCallback InputCallbackH;

		public Yabause(CoreComm CoreComm, DiscSystem.Disc CD, object syncSettings)
		{
			ServiceProvider = new BasicServiceProvider(this);
			byte[] bios = CoreComm.CoreFileProvider.GetFirmware("SAT", "J", true, "Saturn BIOS is required.");
			CoreComm.RomStatusDetails = string.Format("Disk partial hash:{0}", new DiscSystem.DiscHasher(CD).OldHash());
			this.CoreComm = CoreComm;
			this.CD = CD;
			DiscSectorReader = new DiscSystem.DiscSectorReader(CD);

			SyncSettings = (SaturnSyncSettings)syncSettings ?? new SaturnSyncSettings();

			if (this.SyncSettings.UseGL && glContext == null)
			{
				glContext = CoreComm.RequestGLContext(2,0,false);
			}

			ResetCounters();

			ActivateGL();
			Init(bios);

			InputCallbackH = new LibYabause.InputCallback(() => InputCallbacks.Call());
			LibYabause.libyabause_setinputcallback(InputCallbackH);
			ConnectTracer();
			DriveLightEnabled = true;

			DeactivateGL();
		}

		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		object glContext;

		void ActivateGL()
		{
			//if (!SyncSettings.UseGL) return; //not safe
			if (glContext == null) return;
			CoreComm.ActivateGLContext(glContext);
		}

		void DeactivateGL()
		{
			//if (!SyncSettings.UseGL) return; //not safe
			if (glContext == null) return;
			CoreComm.DeactivateGLContext();
		}

		void Init(byte[] bios)
		{
			bool GL = SyncSettings.UseGL;

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

			var fp = new FilePiping();
			string BiosPipe = fp.GetPipeNameNative();
			fp.Offer(bios);

			int basetime;
			if (SyncSettings.RealTimeRTC)
				basetime = 0;
			else
				basetime = (int)((SyncSettings.RTCInitialTime - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds);

			if (!LibYabause.libyabause_init
			(
				ref CDInt,
				BiosPipe,
				GL,
				SyncSettings.CartType,
				SyncSettings.SkipBios,
				!SyncSettings.RealTimeRTC,
				basetime
			))
				throw new Exception("libyabause_init() failed!");

			fp.Finish();

			LibYabause.libyabause_setvidbuff(VideoHandle.AddrOfPinnedObject());
			LibYabause.libyabause_setsndbuff(SoundHandle.AddrOfPinnedObject());
			AttachedCore = this;

			// with or without GL, this is the guaranteed frame -1 size; (unless you do a gl resize)
			BufferWidth = 320;
			BufferHeight = 224;

			InitMemoryDomains();

			GLMode = GL;
			// if in GL mode, this will trigger the initial GL resize
			PutSyncSettings(this.SyncSettings);
		}

		public ControllerDefinition ControllerDefinition
		{
			get { return SaturnController; }
		}

		public IController Controller { get; set; }

		public bool GLMode { get; private set; }

		public void SetGLRes(int factor, int width, int height)
		{
			if (!GLMode)
				return;

			if (factor < 0) factor = 0;
			if (factor > 4) factor = 4;

			int maxwidth, maxheight;

			if (factor == 0)
			{
				maxwidth = width;
				maxheight = height;
			}
			else
			{
				maxwidth = 704 * factor;
				maxheight = 512 * factor;
			}
			if (maxwidth * maxheight > VideoBuffer.Length)
			{
				VideoHandle.Free();
				VideoBuffer = new int[maxwidth * maxheight];
				VideoHandle = GCHandle.Alloc(VideoBuffer, GCHandleType.Pinned);
				LibYabause.libyabause_setvidbuff(VideoHandle.AddrOfPinnedObject());
			}
			LibYabause.libyabause_glsetnativefactor(factor);
			if (factor == 0)
				LibYabause.libyabause_glresize(width, height);
		}

		public void FrameAdvance(bool render, bool rendersound = true)
		{
			int w, h, nsamp;

			ActivateGL();

			LibYabause.Buttons1 p11 = (LibYabause.Buttons1)0xff;
			LibYabause.Buttons2 p12 = (LibYabause.Buttons2)0xff;
			LibYabause.Buttons1 p21 = (LibYabause.Buttons1)0xff;
			LibYabause.Buttons2 p22 = (LibYabause.Buttons2)0xff;

			if (Controller.IsPressed("P1 A"))
				p11 &= ~LibYabause.Buttons1.A;
			if (Controller.IsPressed("P1 B"))
				p11 &= ~LibYabause.Buttons1.B;
			if (Controller.IsPressed("P1 C"))
				p11 &= ~LibYabause.Buttons1.C;
			if (Controller.IsPressed("P1 Start"))
				p11 &= ~LibYabause.Buttons1.S;
			if (Controller.IsPressed("P1 Left"))
				p11 &= ~LibYabause.Buttons1.L;
			if (Controller.IsPressed("P1 Right"))
				p11 &= ~LibYabause.Buttons1.R;
			if (Controller.IsPressed("P1 Up"))
				p11 &= ~LibYabause.Buttons1.U;
			if (Controller.IsPressed("P1 Down"))
				p11 &= ~LibYabause.Buttons1.D;
			if (Controller.IsPressed("P1 L"))
				p12 &= ~LibYabause.Buttons2.L;
			if (Controller.IsPressed("P1 R"))
				p12 &= ~LibYabause.Buttons2.R;
			if (Controller.IsPressed("P1 X"))
				p12 &= ~LibYabause.Buttons2.X;
			if (Controller.IsPressed("P1 Y"))
				p12 &= ~LibYabause.Buttons2.Y;
			if (Controller.IsPressed("P1 Z"))
				p12 &= ~LibYabause.Buttons2.Z;

			if (Controller.IsPressed("P2 A"))
				p21 &= ~LibYabause.Buttons1.A;
			if (Controller.IsPressed("P2 B"))
				p21 &= ~LibYabause.Buttons1.B;
			if (Controller.IsPressed("P2 C"))
				p21 &= ~LibYabause.Buttons1.C;
			if (Controller.IsPressed("P2 Start"))
				p21 &= ~LibYabause.Buttons1.S;
			if (Controller.IsPressed("P2 Left"))
				p21 &= ~LibYabause.Buttons1.L;
			if (Controller.IsPressed("P2 Right"))
				p21 &= ~LibYabause.Buttons1.R;
			if (Controller.IsPressed("P2 Up"))
				p21 &= ~LibYabause.Buttons1.U;
			if (Controller.IsPressed("P2 Down"))
				p21 &= ~LibYabause.Buttons1.D;
			if (Controller.IsPressed("P2 L"))
				p22 &= ~LibYabause.Buttons2.L;
			if (Controller.IsPressed("P2 R"))
				p22 &= ~LibYabause.Buttons2.R;
			if (Controller.IsPressed("P2 X"))
				p22 &= ~LibYabause.Buttons2.X;
			if (Controller.IsPressed("P2 Y"))
				p22 &= ~LibYabause.Buttons2.Y;
			if (Controller.IsPressed("P2 Z"))
				p22 &= ~LibYabause.Buttons2.Z;


			if (Controller.IsPressed("Reset"))
				LibYabause.libyabause_softreset();
			if (Controller.IsPressed("Power"))
				LibYabause.libyabause_hardreset();

			LibYabause.libyabause_setpads(p11, p12, p21, p22);

			DriveLightOn = false;

			if (Tracer.Enabled)
				LibYabause.libyabause_settracecallback(trace_cb);
			else
				LibYabause.libyabause_settracecallback(null);

			IsLagFrame = LibYabause.libyabause_frameadvance(out w, out h, out nsamp);
			BufferWidth = w;
			BufferHeight = h;
			SoundNSamp = nsamp;
			Frame++;
			if (IsLagFrame)
				LagCount++;
			//Console.WriteLine(nsamp);

			//CheckStates();

			DeactivateGL();
		}

		public int Frame { get; private set; }

		public string SystemId { get { return "SAT"; } }
		public bool DeterministicEmulation { get { return true; } }

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
			if (!Disposed)
			{
				ActivateGL();
				if (SaveRamModified)
					DisposedSaveRam = CloneSaveRam();
				LibYabause.libyabause_setvidbuff(IntPtr.Zero);
				LibYabause.libyabause_setsndbuff(IntPtr.Zero);
				LibYabause.libyabause_deinit();
				VideoHandle.Free();
				SoundHandle.Free();
				CD.Dispose();
				Disposed = true;
				DeactivateGL();
				if (glContext != null)
					CoreComm.ReleaseGLContext(glContext);
			}
		}

		#region IVideoProvider

		int[] VideoBuffer = new int[704 * 512];
		int[] TextureIdBuffer = new int[1]; //todo
		public int[] GetVideoBuffer() {
			//doesn't work yet
			//if (SyncSettings.UseGL)
			//  return new[] { VideoBuffer[0] };
			//else
				return VideoBuffer;
		}
		public int VirtualWidth { get { return BufferWidth; } }
		public int VirtualHeight { get { return BufferHeight; } }
		public int BufferWidth { get; private set; }
		public int BufferHeight { get; private set; }
		public int BackgroundColor { get { return unchecked((int)0xff000000); } }

		#endregion

		#region ISyncSoundProvider

		private short[] SoundBuffer = new short[44100 * 2];
		private int SoundNSamp = 0;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			nsamp = SoundNSamp;
			samples = SoundBuffer;
		}

		public void DiscardSamples() { }

		public bool CanProvideAsync
		{
			get { return false; }
		}

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode == SyncSoundMode.Async)
			{
				throw new NotSupportedException("Async mode is not supported.");
			}
		}

		public SyncSoundMode SyncMode
		{
			get { return SyncSoundMode.Sync; }
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new InvalidOperationException("Async mode is not supported.");
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
			//TODO - we could just get this out of the actual TOC, it's the same thing

			int[] rTOC = new int[102];
			var ses = CD.Session1;
			int ntrk = ses.InformationTrackCount;

			for (int i = 0; i < 99; i++)
			{
				int tnum = i + 1;
				if (tnum <= ntrk)
				{
					var trk = ses.Tracks[tnum];

					uint t = (uint)trk.LBA + 150;

					if(trk.IsAudio)
						t |= 0x01000000;
					else 
						t |= 0x41000000;

					rTOC[i] = (int)t;
				}
				else
				{
					rTOC[i] = unchecked((int)0xffffffff);
				}
			}

			rTOC[99] = (int)(rTOC[0] & 0xff000000 | 0x010000);
			rTOC[100] = (int)(rTOC[ntrk - 1] & 0xff000000 | (uint)(ntrk << 16));
			rTOC[101] = (int)(rTOC[ntrk - 1] & 0xff000000 | (uint)(CD.TOC.LeadoutLBA)); //zero 03-jul-2014 - maybe off by 150
			

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
				//CD.ReadABA_2352(FAD, data, 0);
				DiscSectorReader.ReadLBA_2352(FAD - 150, data, 0); //zero 21-jun-2015 - did I adapt this right?
			}
			catch (Exception e)
			{
				Console.WriteLine("CD_ReadSectorFAD: Managed Exception:\n" + e.ToString());
				return 0; // failure
			}
			Marshal.Copy(data, 0, dest, 2352);
			DriveLightOn = true;
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
