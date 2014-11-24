using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sony.PSP
{
	[CoreAttributes(
		"PPSSPP",
		"hrydgard",
		isPorted: true,
		isReleased: false
		)]
	public class PSP : IEmulator, IVideoProvider, ISyncSoundProvider, IMemoryDomains
	{
		public static readonly ControllerDefinition PSPController = new ControllerDefinition
		{
			Name = "PSP Controller",
			BoolButtons =
			{					
				"Up", "Down", "Left", "Right", "Select", "Start", "L", "R", "Square", "Triangle", "Circle", "Cross", "Power"
			},
			FloatControls =
			{
				"Stick X", "Stick Y"
			},
			FloatRanges = // TODO
			{
				new[] {-1.0f, 0.0f, 1.0f},
				new[] {-1.0f, 0.0f, 1.0f},
			}
		};

		public IVideoProvider VideoProvider { get { return this; } }
		public ISoundProvider SoundProvider { get { return null; } }
		public ISyncSoundProvider SyncSoundProvider { get { return this; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }
		public ControllerDefinition ControllerDefinition { get { return PSPController; } }
		public IController Controller { get; set; }
		public bool DeterministicEmulation { get { return true; } }
		public string SystemId { get { return "PSP"; } }
		public bool BinarySaveStatesPreferred { get { return true; } }
		public CoreComm CoreComm { get; private set; }

		public string BoardName { get { return null; } }

		PPSSPPDll.LogCB logcallback = null;
		Queue<string> debugmsgs = new Queue<string>();
		void LogCallbackFunc(char type, string message)
		{
			debugmsgs.Enqueue(string.Format("PSP: {0} {1}", type, message));
		}
		void LogFlush()
		{
			while (debugmsgs.Count > 0)
			{
				Console.WriteLine(debugmsgs.Dequeue());
			}
		}

		bool disposed = false;
		static PSP attachedcore = null;
		GCHandle vidhandle;

		public PSP(CoreComm comm, string isopath)
		{
			if (attachedcore != null)
			{
				attachedcore.Dispose();
				attachedcore = null;
			}
			CoreComm = comm;

			logcallback = new PPSSPPDll.LogCB(LogCallbackFunc);

			bool good = PPSSPPDll.init(isopath, logcallback);
			LogFlush();
			if (!good)
				throw new Exception("PPSSPP Init failed!");
			vidhandle = GCHandle.Alloc(screenbuffer, GCHandleType.Pinned);
			PPSSPPDll.setvidbuff(vidhandle.AddrOfPinnedObject());

			CoreComm.VsyncDen = 1;
			CoreComm.VsyncNum = 60;
			CoreComm.RomStatusDetails = "It puts the scythe in the chicken or it gets the abyss again!";

			attachedcore = this;
		}

		public void Dispose()
		{
			if (!disposed)
			{
				vidhandle.Free();
				PPSSPPDll.setvidbuff(IntPtr.Zero);
				PPSSPPDll.die();
				logcallback = null;
				disposed = true;
				LogFlush();
				Console.WriteLine("PSP Core Disposed.");
			}
		}



		public void FrameAdvance(bool render, bool rendersound = true)
		{
			PPSSPPDll.advance();
			// problem 1: audio can be 48khz, if a particular core parameter is set.  we're not accounting for that.
			// problem 2: we seem to be getting approximately the right amount of output, but with
			// a lot of jitter on the per-frame buffer size
			nsampavail = PPSSPPDll.mixsound(audiobuffer, audiobuffer.Length / 2);
			LogFlush();
			//Console.WriteLine("Audio Service: {0}", nsampavail);
		}

		public int Frame
		{
			[FeatureNotImplemented]
			get { return 0; }
		}

		public int LagCount
		{
			[FeatureNotImplemented]
			get
			{
				return 0;
			}

			[FeatureNotImplemented]
			set
			{
			}
		}

		public bool IsLagFrame
		{
			[FeatureNotImplemented]
			get { return true; }
		}

		[FeatureNotImplemented]
		public byte[] CloneSaveRam()
		{
			return new byte[0];
		}

		[FeatureNotImplemented]
		public void StoreSaveRam(byte[] data)
		{
		}

		public bool SaveRamModified
		{
			[FeatureNotImplemented]
			get
			{
				return false;
			}

			[FeatureNotImplemented]
			set
			{
			}
		}

		[FeatureNotImplemented]
		public void ResetCounters()
		{
		}

		[FeatureNotImplemented]
		public void SaveStateText(System.IO.TextWriter writer)
		{
		}

		[FeatureNotImplemented]
		public void LoadStateText(System.IO.TextReader reader)
		{
		}

		[FeatureNotImplemented]
		public void SaveStateBinary(System.IO.BinaryWriter writer)
		{
		}

		[FeatureNotImplemented]
		public void LoadStateBinary(System.IO.BinaryReader reader)
		{
		}

		[FeatureNotImplemented]
		public byte[] SaveStateBinary()
		{
			return new byte[0];
		}

		[FeatureNotImplemented]
		public MemoryDomainList MemoryDomains
		{
			get { throw new NotImplementedException(); }
		}

		const int screenwidth = 480;
		const int screenheight = 272;
		readonly int[] screenbuffer = new int[screenwidth * screenheight];
		public int[] GetVideoBuffer() { return screenbuffer; }
		public int VirtualWidth { get { return screenwidth; } }
		public int VirtualHeight { get { return screenheight; } }
		public int BufferWidth { get { return screenwidth; } }
		public int BufferHeight { get { return screenheight; } }
		public int BackgroundColor { get { return unchecked((int)0xff000000); } }

		readonly short[] audiobuffer = new short[2048 * 2];
		int nsampavail = 0;
		public void GetSamples(out short[] samples, out int nsamp)
		{			
			samples = audiobuffer;
			nsamp = nsampavail;
		}
		public void DiscardSamples()
		{
		}
	}
}
