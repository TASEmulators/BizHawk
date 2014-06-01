using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.ComponentModel;

using BizHawk.Common;
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
		portedUrl: "http://yabause.org"
		)]
	public class Yabause : IEmulator, IVideoProvider, ISyncSoundProvider
	{
		public static ControllerDefinition SaturnController = new ControllerDefinition
		{
			Name = "Saturn Controller",
			BoolButtons =
			{	
				"Power", "Reset",
				"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Start", "P1 Z", "P1 Y", "P1 X", "P1 C", "P1 B", "P1 A", "P1 L", "P1 R",
				"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Start", "P2 Z", "P2 Y", "P2 X", "P2 C", "P2 B", "P2 A", "P2 L", "P2 R",
			}
		};

		static Yabause AttachedCore = null;
		GCHandle VideoHandle;
		Disc CD;
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

		public Yabause(CoreComm CoreComm, DiscSystem.Disc CD, object SyncSettings)
		{
			byte[] bios = CoreComm.CoreFileProvider.GetFirmware("SAT", "J", true, "Saturn BIOS is required.");
			CoreComm.RomStatusDetails = string.Format("Disk partial hash:{0}", CD.GetHash());
			this.CoreComm = CoreComm;
			this.CD = CD;

			this.SyncSettings = (SaturnSyncSettings)SyncSettings ?? new SaturnSyncSettings();

			ResetCounters();
			Init(bios);

			InputCallbackH = new LibYabause.InputCallback(() => CoreComm.InputCallback.Call());
			LibYabause.libyabause_setinputcallback(InputCallbackH);
			CoreComm.UsesDriveLed = true;
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

		public Dictionary<string, int> GetCpuFlagsAndRegisters()
		{
			throw new NotImplementedException();
		}

		public void SetCpuRegister(string register, int value)
		{
			throw new NotImplementedException();
		}

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

			LibYabause.Buttons1 p11 = (LibYabause.Buttons1)0xff;
			LibYabause.Buttons2 p12 = (LibYabause.Buttons2)0xff;
			LibYabause.Buttons1 p21 = (LibYabause.Buttons1)0xff;
			LibYabause.Buttons2 p22 = (LibYabause.Buttons2)0xff;

			if (Controller["P1 A"])
				p11 &= ~LibYabause.Buttons1.A;
			if (Controller["P1 B"])
				p11 &= ~LibYabause.Buttons1.B;
			if (Controller["P1 C"])
				p11 &= ~LibYabause.Buttons1.C;
			if (Controller["P1 Start"])
				p11 &= ~LibYabause.Buttons1.S;
			if (Controller["P1 Left"])
				p11 &= ~LibYabause.Buttons1.L;
			if (Controller["P1 Right"])
				p11 &= ~LibYabause.Buttons1.R;
			if (Controller["P1 Up"])
				p11 &= ~LibYabause.Buttons1.U;
			if (Controller["P1 Down"])
				p11 &= ~LibYabause.Buttons1.D;
			if (Controller["P1 L"])
				p12 &= ~LibYabause.Buttons2.L;
			if (Controller["P1 R"])
				p12 &= ~LibYabause.Buttons2.R;
			if (Controller["P1 X"])
				p12 &= ~LibYabause.Buttons2.X;
			if (Controller["P1 Y"])
				p12 &= ~LibYabause.Buttons2.Y;
			if (Controller["P1 Z"])
				p12 &= ~LibYabause.Buttons2.Z;

			if (Controller["P2 A"])
				p21 &= ~LibYabause.Buttons1.A;
			if (Controller["P2 B"])
				p21 &= ~LibYabause.Buttons1.B;
			if (Controller["P2 C"])
				p21 &= ~LibYabause.Buttons1.C;
			if (Controller["P2 Start"])
				p21 &= ~LibYabause.Buttons1.S;
			if (Controller["P2 Left"])
				p21 &= ~LibYabause.Buttons1.L;
			if (Controller["P2 Right"])
				p21 &= ~LibYabause.Buttons1.R;
			if (Controller["P2 Up"])
				p21 &= ~LibYabause.Buttons1.U;
			if (Controller["P2 Down"])
				p21 &= ~LibYabause.Buttons1.D;
			if (Controller["P2 L"])
				p22 &= ~LibYabause.Buttons2.L;
			if (Controller["P2 R"])
				p22 &= ~LibYabause.Buttons2.R;
			if (Controller["P2 X"])
				p22 &= ~LibYabause.Buttons2.X;
			if (Controller["P2 Y"])
				p22 &= ~LibYabause.Buttons2.Y;
			if (Controller["P2 Z"])
				p22 &= ~LibYabause.Buttons2.Z;


			if (Controller["Reset"])
				LibYabause.libyabause_softreset();
			if (Controller["Power"])
				LibYabause.libyabause_hardreset();

			LibYabause.libyabause_setpads(p11, p12, p21, p22);

			CoreComm.DriveLED = false;

			IsLagFrame = LibYabause.libyabause_frameadvance(out w, out h, out nsamp);
			BufferWidth = w;
			BufferHeight = h;
			SoundNSamp = nsamp;
			Frame++;
			if (IsLagFrame)
				LagCount++;
			//Console.WriteLine(nsamp);

			//CheckStates();
		}

		public int Frame { get; private set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; private set; }

		public string SystemId { get { return "SAT"; } }
		public bool DeterministicEmulation { get { return true; } }

		public string BoardName { get { return null; } }

		#region saveram

		public byte[] ReadSaveRam()
		{
			if (Disposed)
			{
				return DisposedSaveRam ?? new byte[0];
			}
			else
			{
				var ms = new MemoryStream();
				var fp = new FilePiping();
				fp.Get(ms);
				bool success = LibYabause.libyabause_savesaveram(fp.GetPipeNameNative());
				fp.Finish();
				if (!success)
					throw new Exception("libyabause_savesaveram() failed!");
				var ret = ms.ToArray();
				ms.Dispose();
				return ret;
			}

		}

		public void StoreSaveRam(byte[] data)
		{
			if (Disposed)
			{
				throw new Exception("It's a bit late for that");
			}
			else
			{
				var fp = new FilePiping();
				fp.Offer(data);
				bool success = LibYabause.libyabause_loadsaveram(fp.GetPipeNameNative());
				fp.Finish();
				if (!success)
					throw new Exception("libyabause_loadsaveram() failed!");
			}
		}

		public void ClearSaveRam()
		{
			if (Disposed)
			{
				throw new Exception("It's a bit late for that");
			}
			else
			{
				LibYabause.libyabause_clearsaveram();
			}
		}

		public bool SaveRamModified
		{
			get
			{
				if (Disposed)
					return DisposedSaveRam != null;
				else
					return LibYabause.libyabause_saveramodified();
			}
			set { throw new InvalidOperationException("No you may not!"); }
		}

		#endregion

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		#region savestates

		void LoadCoreBinary(byte[] data)
		{
			var fp = new FilePiping();
			fp.Offer(data);
			bool succeed = LibYabause.libyabause_loadstate(fp.GetPipeNameNative());
			fp.Finish();
			if (!succeed)
				throw new Exception("libyabause_loadstate() failed");
		}

		byte[] SaveCoreBinary()
		{
			var ms = new MemoryStream();
			var fp = new FilePiping();
			fp.Get(ms);
			bool succeed = LibYabause.libyabause_savestate(fp.GetPipeNameNative());
			fp.Finish();
			var ret = ms.ToArray();
			ms.Close();
			if (!succeed)
				throw new Exception("libyabause_savestate() failed");
			return ret;
		}

		// these next 5 functions are all exact copy paste from gambatte.
		// if something's wrong here, it's probably wrong there too

		public void SaveStateText(TextWriter writer)
		{
			var temp = SaveStateBinary();
			temp.SaveAsHexFast(writer);
			// write extra copy of stuff we don't use
			writer.WriteLine("Frame {0}", Frame);
		}

		public void LoadStateText(TextReader reader)
		{
			string hex = reader.ReadLine();
			byte[] state = new byte[hex.Length / 2];
			state.ReadFromHexFast(hex);
			LoadStateBinary(new BinaryReader(new MemoryStream(state)));
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			byte[] data = SaveCoreBinary();

			writer.Write(data.Length);
			writer.Write(data);

			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			int length = reader.ReadInt32();
			byte[] data = reader.ReadBytes(length);

			LoadCoreBinary(data);

			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
		}

		public bool BinarySaveStatesPreferred { get { return true; } }

		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		/// <summary>
		/// does a save, load, save combo, and checks the two saves for identicalness.
		/// </summary>
		void CheckStates()
		{
			byte[] s1 = SaveStateBinary();
			LoadStateBinary(new BinaryReader(new MemoryStream(s1, false)));
			byte[] s2 = SaveStateBinary();
			if (s1.Length != s2.Length)
				throw new Exception(string.Format("CheckStates: Length {0} != {1}", s1.Length, s2.Length));
			unsafe
			{
				fixed (byte* b1 = &s1[0], b2 = &s2[0])
				{
					for (int i = 0; i < s1.Length; i++)
					{
						if (b1[i] != b2[i])
						{
							File.WriteAllBytes("save1.raw", s1);
							File.WriteAllBytes("save2.raw", s2);
							throw new Exception(string.Format("CheckStates s1[{0}] = {1}, s2[{0}] = {2}", i, b1[i], b2[i]));
						}
					}
				}
			}
		}


		#endregion

		public CoreComm CoreComm { get; private set; }

		#region memorydomains

		void InitMemoryDomains()
		{
			var ret = new List<MemoryDomain>();
			var nmds = LibYabause.libyabause_getmemoryareas_ex();
			foreach (var nmd in nmds)
			{
				int l = nmd.length;
				IntPtr d = nmd.data;
				ret.Add(new MemoryDomain(
					nmd.name,
					nmd.length,
					MemoryDomain.Endian.Little,
					delegate(int addr)
					{
						if (addr < 0 || addr >= l)
							throw new ArgumentOutOfRangeException();
						unsafe
						{
							byte* p = (byte*)d;
							return p[addr];
						}
					},
					delegate(int addr, byte val)
					{
						if (addr < 0 || addr >= l)
							throw new ArgumentOutOfRangeException();
						unsafe
						{
							byte* p = (byte*)d;
							p[addr] = val;
						}
					}
				));
			}
			// fulfill the prophecy of MainMemory always being MemoryDomains[0]
			var tmp = ret[2];
			ret[2] = ret[0];
			ret[0] = tmp;
			MemoryDomains = new MemoryDomainList(ret);
		}

		public MemoryDomainList MemoryDomains { get; private set; }

		#endregion

		public void Dispose()
		{
			if (!Disposed)
			{
				if (SaveRamModified)
					DisposedSaveRam = ReadSaveRam();
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
		public int VirtualWidth { get { return BufferWidth; } }
		public int VirtualHeight { get { return BufferHeight; } }
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

		public void DiscardSamples() { }
		public ISoundProvider SoundProvider { get { return null; } }
		public ISyncSoundProvider SyncSoundProvider { get { return this; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }

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
			CoreComm.DriveLED = true;
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

		SaturnSyncSettings SyncSettings;

		public object GetSettings() { return null; }
		public object GetSyncSettings() { return SyncSettings.Clone(); }
		public bool PutSettings(object o) { return false; }
		public bool PutSyncSettings(object o)
		{
			var n = (SaturnSyncSettings)o;
			bool ret = SaturnSyncSettings.NeedsReboot(SyncSettings, n);

			SyncSettings = n;

			if (GLMode && SyncSettings.UseGL)
				if (SyncSettings.DispFree)
					SetGLRes(0, SyncSettings.GLW, SyncSettings.GLH);
				else
					SetGLRes(SyncSettings.DispFactor, 0, 0);
			return ret;
		}

		public class SaturnSyncSettings
		{
			[Description("Use OpenGL mode for rendering instead of software.")]
			[DefaultValue(false)]
			public bool UseGL { get; set; }

			[Description("In OpenGL mode, the internal resolution as a multiple of the normal internal resolution (1x, 2x, 3x, 4x).  Ignored in software mode or when a custom resolution is used.")]
			[DefaultValue(1)]
			public int DispFactor { get { return _DispFactor; } set { _DispFactor = Math.Max(1, Math.Min(value, 4)); } }
			[JsonIgnore]
			private int _DispFactor;

			[Description("In OpenGL mode, set to true to use a custom resolution and ignore DispFactor.")]
			[DefaultValue(false)]
			public bool DispFree { get; set; }

			[Description("In OpenGL mode and when DispFree is true, the width of the final resolution.")]
			[DefaultValue(640)]
			public int GLW { get { return _GLW; } set { _GLW = Math.Max(320, Math.Min(value, 2048)); } }
			[JsonIgnore]
			private int _GLW;

			[Description("In OpenGL mode and when DispFree is true, the height of the final resolution.")]
			[DefaultValue(480)]
			public int GLH { get { return _GLH; } set { _GLH = Math.Max(224, Math.Min(value, 1024)); } }
			[JsonIgnore]
			private int _GLH;

			[Description("The type of the attached RAM cart.  Most games will not use this.")]
			[DefaultValue(LibYabause.CartType.NONE)]
			public LibYabause.CartType CartType { get; set; }

			[Description("Skip the Bios Intro screen.")]
			[DefaultValue(false)]
			public bool SkipBios { get; set; }

			[Description("If true, the real time clock will reflect real time, instead of emulated time.  Ignored (forced to false) when a movie is recording.")]
			[DefaultValue(false)]
			public bool RealTimeRTC { get; set; }

			[Description("Set the initial RTC time.  Only used when RealTimeRTC is false.")]
			[DefaultValue(typeof(DateTime), "2010-01-01")]
			public DateTime RTCInitialTime { get; set; }

			public static bool NeedsReboot(SaturnSyncSettings x, SaturnSyncSettings y)
			{
				return x.UseGL != y.UseGL || x.CartType != y.CartType || x.SkipBios != y.SkipBios || x.RealTimeRTC != y.RealTimeRTC || x.RTCInitialTime != y.RTCInitialTime;
			}
			public SaturnSyncSettings Clone()
			{
				return (SaturnSyncSettings)MemberwiseClone();
			}
			public SaturnSyncSettings()
			{
				SettingsUtil.SetDefaultValues<SaturnSyncSettings>(this);
			}
		}
	}
}
