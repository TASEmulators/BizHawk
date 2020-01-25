using BizHawk.Emulation.Common;
using System;

namespace BizHawk.Emulation.Cores.Computers.MSX
{
	public partial class MSX : IEmulator, ISoundProvider, IVideoProvider
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition
		{
			get
			{
				return GGController;
			}
		}

		public bool FrameAdvance(IController controller, bool render, bool rendersound)
		{
			_controller = controller;

			byte ctrl1_byte = 0;
			if (_controller.IsPressed("P1 Up")) ctrl1_byte |= 0x01;
			if (_controller.IsPressed("P1 Down")) ctrl1_byte |= 0x02;
			if (_controller.IsPressed("P1 Left")) ctrl1_byte |= 0x04;
			if (_controller.IsPressed("P1 Right")) ctrl1_byte |= 0x08;
			if (_controller.IsPressed("P1 B1")) ctrl1_byte |= 0x10;
			if (_controller.IsPressed("P1 B2")) ctrl1_byte |= 0x20;

			if (_controller.IsPressed("P1 Start")) ctrl1_byte |= 0x80;

			_frame++;
			
			if (Tracer.Enabled)
			{
				tracecb = MakeTrace;
			}
			else
			{
				tracecb = null;
			}

			LibMSX.MSX_settracecallback(MSX_Pntr, tracecb);

			LibMSX.MSX_frame_advance(MSX_Pntr, ctrl1_byte, 0, true, true);

			return true;
		}

		public int Frame => _frame;

		public string SystemId => "MSX";

		public bool DeterministicEmulation => true;

		public void ResetCounters()
		{
			_frame = 0;
			_lagCount = 0;
			_isLag = false;
		}

		public CoreComm CoreComm { get; }

		public void Dispose()
		{
			if (MSX_Pntr != IntPtr.Zero)
			{
				LibMSX.MSX_destroy(MSX_Pntr);
				MSX_Pntr = IntPtr.Zero;
			}

			if (blip != null)
			{
				blip.Dispose();
				blip = null;
			}
		}

		#region Audio

		public BlipBuffer blip = new BlipBuffer(4500);

		public uint[] Aud = new uint [9000];
		public uint num_samp;

		const int blipbuffsize = 4500;

		public bool CanProvideAsync { get { return false; } }

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Sync)
			{
				throw new NotSupportedException("Only sync mode is supported");
			}
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new NotSupportedException("Async not supported");
		}

		public SyncSoundMode SyncMode
		{
			get { return SyncSoundMode.Sync; }
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			uint f_clock = LibMSX.MSX_get_audio(MSX_Pntr, Aud, ref num_samp);

			for (int i = 0; i < num_samp;i++)
			{
				blip.AddDelta(Aud[i * 2], (int)Aud[i * 2 + 1]);
			}

			blip.EndFrame(f_clock);

			nsamp = blip.SamplesAvailable();
			samples = new short[nsamp * 2];

			blip.ReadSamples(samples, nsamp, true);
		}

		public void DiscardSamples()
		{
			blip.Clear();
		}

		#endregion

		#region Video
		public int _frameHz = 60;

		public int[] _vidbuffer = new int[192 * 256];

		public int[] GetVideoBuffer()
		{
			LibMSX.MSX_get_video(MSX_Pntr, _vidbuffer);
			return _vidbuffer;
		}

		public int VirtualWidth => 256;
		public int VirtualHeight => 192;
		public int BufferWidth => 256;
		public int BufferHeight => 192;
		public int BackgroundColor => unchecked((int)0xFF000000);
		public int VsyncNumerator => _frameHz;
		public int VsyncDenominator => 1;

		#endregion
	}
}
