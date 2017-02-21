using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sony.PSP
{
	[CoreAttributes(
		"PPSSPP",
		"hrydgard",
		isPorted: true,
		isReleased: false,
		singleInstance: true
		)]
	public class PSP : IEmulator, IVideoProvider, ISoundProvider
	{
		public static readonly ControllerDefinition PSPController = new ControllerDefinition
		{
			Name = "PSP Controller",
			BoolButtons =
			{					
				"Up", "Down", "Left", "Right", "Select", "Start", "L", "R", "Square", "Triangle", "Circle", "Cross", 
				"Menu", "Back",
				"Power"
			},
			FloatControls =
			{
				"Left Stick X", "Left Stick Y", "Right Stick X", "Right Stick Y", "Left Trigger", "Right Trigger"
			},
			FloatRanges = // TODO
			{
				new[] {-1.0f, 0.0f, 1.0f},
				new[] {-1.0f, 0.0f, 1.0f},
				new[] {-1.0f, 0.0f, 1.0f},
				new[] {-1.0f, 0.0f, 1.0f},
				new[] {-1.0f, 0.0f, 1.0f},
				new[] {-1.0f, 0.0f, 1.0f},
			}
		};

		public ControllerDefinition ControllerDefinition { get { return PSPController; } }
		public IController Controller { get; set; }
		public bool DeterministicEmulation { get { return true; } }
		public string SystemId { get { return "PSP"; } }
		public CoreComm CoreComm { get; private set; }

		public string BoardName { get { return null; } }

		PPSSPPDll.LogCB logcallback = null;
		Queue<string> debugmsgs = new Queue<string>();
		PPSSPPDll.Input input = new PPSSPPDll.Input();

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
		object glcontext;

		public PSP(CoreComm comm, string isopath)
		{
			ServiceProvider = new BasicServiceProvider(this);
			if (attachedcore != null)
			{
				attachedcore.Dispose();
				attachedcore = null;
			}
			CoreComm = comm;

			glcontext = CoreComm.RequestGLContext(3, 0, true);
			CoreComm.ActivateGLContext(glcontext);

			logcallback = new PPSSPPDll.LogCB(LogCallbackFunc);

			bool good = PPSSPPDll.BizInit(isopath, logcallback);
			LogFlush();
			if (!good)
				throw new Exception("PPSSPP Init failed!");

			CoreComm.VsyncDen = 1;
			CoreComm.VsyncNum = 60;
			CoreComm.RomStatusDetails = "It puts the scythe in the chicken or it gets the abyss again!";

			attachedcore = this;
		}

		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		public void Dispose()
		{
			if (!disposed)
			{
				PPSSPPDll.BizClose();
				logcallback = null;
				disposed = true;
				LogFlush();

				Console.WriteLine("PSP Core Disposed.");
			}
		}

		private void UpdateInput()
		{
			PPSSPPDll.Buttons b = 0;
			var c = Controller;
			if (c.IsPressed("Up")) b |= PPSSPPDll.Buttons.UP;
			if (c.IsPressed("Down")) b |= PPSSPPDll.Buttons.DOWN;
			if (c.IsPressed("Left")) b |= PPSSPPDll.Buttons.LEFT;
			if (c.IsPressed("Right")) b |= PPSSPPDll.Buttons.RIGHT;
			if (c.IsPressed("Select")) b |= PPSSPPDll.Buttons.SELECT;
			if (c.IsPressed("Start")) b |= PPSSPPDll.Buttons.START;
			if (c.IsPressed("L")) b |= PPSSPPDll.Buttons.LBUMPER;
			if (c.IsPressed("R")) b |= PPSSPPDll.Buttons.RBUMPER;
			if (c.IsPressed("Square")) b |= PPSSPPDll.Buttons.A;
			if (c.IsPressed("Triangle")) b |= PPSSPPDll.Buttons.B;
			if (c.IsPressed("Circle")) b |= PPSSPPDll.Buttons.X;
			if (c.IsPressed("Cross")) b |= PPSSPPDll.Buttons.Y;
			if (c.IsPressed("Menu")) b |= PPSSPPDll.Buttons.MENU;
			if (c.IsPressed("Back")) b |= PPSSPPDll.Buttons.BACK;

			input.SetButtons(b);

			input.LeftStickX = c.GetFloat("Left Stick X");
			input.LeftStickY = c.GetFloat("Left Stick Y");
			input.RightStickX = c.GetFloat("Right Stick X");
			input.RightStickY = c.GetFloat("Right Stick Y");
			input.LeftTrigger = c.GetFloat("Left Trigger");
			input.RightTrigger = c.GetFloat("Right Trigger");
		}


		public void FrameAdvance(bool render, bool rendersound = true)
		{
			Frame++;
			UpdateInput();
			PPSSPPDll.BizAdvance(screenbuffer, input);

			// problem 1: audio can be 48khz, if a particular core parameter is set.  we're not accounting for that.
			// problem 2: we seem to be getting approximately the right amount of output, but with
			// a lot of jitter on the per-frame buffer size

			nsampavail = PPSSPPDll.MixSound(audiobuffer, audiobuffer.Length / 2);
			//Console.WriteLine(nsampavail);

			//nsampavail = PPSSPPDll.mixsound(audiobuffer, audiobuffer.Length / 2);
			LogFlush();
			//Console.WriteLine("Audio Service: {0}", nsampavail);
		}

		public int Frame
		{
			get;
			private set;
		}

		public void ResetCounters()
		{
			Frame = 0;
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
		public void GetSamplesSync(out short[] samples, out int nsamp)
		{			
			samples = audiobuffer;
			nsamp = nsampavail;
			//nsamp = 735;
		}
		public void DiscardSamples()
		{
		}

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
	}
}
