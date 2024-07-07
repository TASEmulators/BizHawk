using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.MSX
{
	public partial class MSX : IEmulator, ISoundProvider, IVideoProvider
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => current_controller;

		public bool FrameAdvance(IController controller, bool render, bool rendersound)
		{
			_controller = controller;

			byte ctrl1_byte = 0xFF;
			if (_controller.IsPressed("P1 Up")) ctrl1_byte -= 0x01;
			if (_controller.IsPressed("P1 Down")) ctrl1_byte -= 0x02;
			if (_controller.IsPressed("P1 Left")) ctrl1_byte -= 0x04;
			if (_controller.IsPressed("P1 Right")) ctrl1_byte -= 0x08;
			if (_controller.IsPressed("P1 B1")) ctrl1_byte -= 0x10;
			if (_controller.IsPressed("P1 B2")) ctrl1_byte -= 0x20;

			byte ctrl2_byte = 0xFF;
			if (_controller.IsPressed("P2 Up")) ctrl2_byte -= 0x01;
			if (_controller.IsPressed("P2 Down")) ctrl2_byte -= 0x02;
			if (_controller.IsPressed("P2 Left")) ctrl2_byte -= 0x04;
			if (_controller.IsPressed("P2 Right")) ctrl2_byte -= 0x08;
			if (_controller.IsPressed("P2 B1")) ctrl2_byte -= 0x10;
			if (_controller.IsPressed("P2 B2")) ctrl2_byte -= 0x20;

			if (current_controller == MSXControllerKB) { kb_rows_check(controller); }		
			
			if (Tracer.IsEnabled())
			{
				tracecb = MakeTrace;
			}
			else
			{
				tracecb = null;
			}
			
			LibMSX.MSX_settracecallback(MSX_Pntr, tracecb);
			
			LibMSX.MSX_frame_advance(MSX_Pntr, ctrl1_byte, ctrl2_byte, kb_rows, true, true);

			LibMSX.MSX_get_video(MSX_Pntr, _vidbuffer);

			/*
			int msg_l = LibMSX.MSX_getmessagelength(MSX_Pntr);
			StringBuilder new_msg = new StringBuilder(msg_l);
			LibMSX.MSX_getmessage(MSX_Pntr, new_msg, msg_l - 1);
			Console.WriteLine(new_msg);
			*/

			_frame++;

			return true;
		}

		public byte[] kb_rows = new byte[16];

		public void kb_rows_check(IController controller)
		{
			for(int i = 0; i < 9; i++) { kb_rows[i] = 0; }
			
			if (controller.IsPressed("7")) { kb_rows[0] |= 0x80; }
			if (controller.IsPressed("6")) { kb_rows[0] |= 0x40; }
			if (controller.IsPressed("5")) { kb_rows[0] |= 0x20; }
			if (controller.IsPressed("4")) { kb_rows[0] |= 0x10; }
			if (controller.IsPressed("3")) { kb_rows[0] |= 0x08; }
			if (controller.IsPressed("2")) { kb_rows[0] |= 0x04; }
			if (controller.IsPressed("1")) { kb_rows[0] |= 0x02; }
			if (controller.IsPressed("0")) { kb_rows[0] |= 0x01; }

			if (controller.IsPressed(";")) { kb_rows[1] |= 0x80; }
			if (controller.IsPressed("[")) { kb_rows[1] |= 0x40; }
			if (controller.IsPressed("@")) { kb_rows[1] |= 0x20; }
			if (controller.IsPressed("$")) { kb_rows[1] |= 0x10; }
			if (controller.IsPressed("^")) { kb_rows[1] |= 0x08; }
			if (controller.IsPressed("-")) { kb_rows[1] |= 0x04; }
			if (controller.IsPressed("9")) { kb_rows[1] |= 0x02; }
			if (controller.IsPressed("8")) { kb_rows[1] |= 0x01; }

			if (controller.IsPressed("B")) { kb_rows[2] |= 0x80; }
			if (controller.IsPressed("A")) { kb_rows[2] |= 0x40; }

			if (controller.IsPressed("/")) { kb_rows[2] |= 0x10; }
			if (controller.IsPressed(".")) { kb_rows[2] |= 0x08; }
			if (controller.IsPressed(",")) { kb_rows[2] |= 0x04; }
			if (controller.IsPressed("]")) { kb_rows[2] |= 0x02; }
			if (controller.IsPressed(":")) { kb_rows[2] |= 0x01; }

			if (controller.IsPressed("J")) { kb_rows[3] |= 0x80; }
			if (controller.IsPressed("I")) { kb_rows[3] |= 0x40; }
			if (controller.IsPressed("H")) { kb_rows[3] |= 0x20; }
			if (controller.IsPressed("G")) { kb_rows[3] |= 0x10; }
			if (controller.IsPressed("F")) { kb_rows[3] |= 0x08; }
			if (controller.IsPressed("E")) { kb_rows[3] |= 0x04; }
			if (controller.IsPressed("D")) { kb_rows[3] |= 0x02; }
			if (controller.IsPressed("C")) { kb_rows[3] |= 0x01; }

			if (controller.IsPressed("R")) { kb_rows[4] |= 0x80; }
			if (controller.IsPressed("Q")) { kb_rows[4] |= 0x40; }
			if (controller.IsPressed("P")) { kb_rows[4] |= 0x20; }
			if (controller.IsPressed("O")) { kb_rows[4] |= 0x10; }
			if (controller.IsPressed("N")) { kb_rows[4] |= 0x08; }
			if (controller.IsPressed("M")) { kb_rows[4] |= 0x04; }
			if (controller.IsPressed("L")) { kb_rows[4] |= 0x02; }
			if (controller.IsPressed("K")) { kb_rows[4] |= 0x01; }

			if (controller.IsPressed("Z")) { kb_rows[5] |= 0x80; }
			if (controller.IsPressed("Y")) { kb_rows[5] |= 0x40; }
			if (controller.IsPressed("X")) { kb_rows[5] |= 0x20; }
			if (controller.IsPressed("W")) { kb_rows[5] |= 0x10; }
			if (controller.IsPressed("V")) { kb_rows[5] |= 0x08; }
			if (controller.IsPressed("U")) { kb_rows[5] |= 0x04; }
			if (controller.IsPressed("T")) { kb_rows[5] |= 0x02; }
			if (controller.IsPressed("S")) { kb_rows[5] |= 0x01; }

			if (controller.IsPressed("F3")) { kb_rows[6] |= 0x80; }
			if (controller.IsPressed("F2")) { kb_rows[6] |= 0x40; }
			if (controller.IsPressed("F1")) { kb_rows[6] |= 0x20; }
			if (controller.IsPressed("KANA")) { kb_rows[6] |= 0x10; }
			if (controller.IsPressed("CAP")) { kb_rows[6] |= 0x08; }
			if (controller.IsPressed("GRAPH")) { kb_rows[6] |= 0x04; }
			if (controller.IsPressed("CTRL")) { kb_rows[6] |= 0x02; }
			if (controller.IsPressed("SHIFT")) { kb_rows[6] |= 0x01; }

			if (controller.IsPressed("RET")) { kb_rows[7] |= 0x80; }
			if (controller.IsPressed("SEL")) { kb_rows[7] |= 0x40; }
			if (controller.IsPressed("BACK")) { kb_rows[7] |= 0x20; }
			if (controller.IsPressed("STOP")) { kb_rows[7] |= 0x10; }
			if (controller.IsPressed("TAB")) { kb_rows[7] |= 0x08; }
			if (controller.IsPressed("ESC")) { kb_rows[7] |= 0x04; }
			if (controller.IsPressed("F5")) { kb_rows[7] |= 0x02; }
			if (controller.IsPressed("F4")) { kb_rows[7] |= 0x01; }

			if (controller.IsPressed("RIGHT")) { kb_rows[8] |= 0x80; }
			if (controller.IsPressed("DOWN")) { kb_rows[8] |= 0x40; }
			if (controller.IsPressed("UP")) { kb_rows[8] |= 0x20; }
			if (controller.IsPressed("LEFT")) { kb_rows[8] |= 0x10; }
			if (controller.IsPressed("DEL")) { kb_rows[8] |= 0x08; }
			if (controller.IsPressed("INS")) { kb_rows[8] |= 0x04; }
			if (controller.IsPressed("HOME")) { kb_rows[8] |= 0x02; }
			if (controller.IsPressed("SPACE")) { kb_rows[8] |= 0x01; }
		}

		public int Frame => _frame;

		public string SystemId => VSystemID.Raw.MSX;

		public bool DeterministicEmulation => true;

		public void ResetCounters()
		{
			_frame = 0;
			_lagCount = 0;
			_isLag = false;
		}

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

		public BlipBuffer blip = new BlipBuffer(4500);

		public int[] Aud = new int [9000];
		public uint num_samp;

		private const int blipbuffsize = 4500;

		public bool CanProvideAsync => false;

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

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			uint f_clock = LibMSX.MSX_get_audio(MSX_Pntr, Aud, ref num_samp);

			for (int i = 0; i < num_samp;i++)
			{
				blip.AddDelta((uint)Aud[i * 2], Aud[i * 2 + 1]);
			}

			blip.EndFrame(f_clock);

			nsamp = blip.SamplesAvailable();
			samples = new short[nsamp * 2];

			blip.ReadSamples(samples, nsamp, true);

			for (int i = 0; i < nsamp * 2; i += 2)
			{
				samples[i + 1] = samples[i];
			}
		}

		public void DiscardSamples()
		{
			blip.Clear();
		}

		public int _frameHz = 60;

		public int[] _vidbuffer = new int[192 * 256];

		public int[] GetVideoBuffer()
		{
			return _vidbuffer;
		}

		public int VirtualWidth => 256;
		public int VirtualHeight => 192;
		public int BufferWidth => 256;
		public int BufferHeight => 192;

		public int BackgroundColor => unchecked((int)0xFF000000);
		public int VsyncNumerator => _frameHz;
		public int VsyncDenominator => 1;
	}
}
