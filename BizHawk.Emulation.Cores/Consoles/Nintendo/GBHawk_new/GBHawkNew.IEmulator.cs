using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;
using System;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkNew
{
	public partial class GBHawkNew : IEmulator, IVideoProvider, ISoundProvider
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _controllerDeck.Definition;

		public byte controller_state;
		public ushort Acc_X_state;
		public ushort Acc_Y_state;
		public bool in_vblank_old;
		public bool in_vblank;
		public bool vblank_rise;

		public bool FrameAdvance(IController controller, bool render, bool rendersound)
		{
			//Console.WriteLine("-----------------------FRAME-----------------------");

			//Update the color palette if a setting changed
			if (_settings.Palette == GBSettings.PaletteType.BW)
			{
				color_palette[0] = color_palette_BW[0];
				color_palette[1] = color_palette_BW[1];
				color_palette[2] = color_palette_BW[2];
				color_palette[3] = color_palette_BW[3];
			}
			else
			{
				color_palette[0] = color_palette_Gr[0];
				color_palette[1] = color_palette_Gr[1];
				color_palette[2] = color_palette_Gr[2];
				color_palette[3] = color_palette_Gr[3];
			}

			if (Tracer.Enabled)
			{
				tracecb = MakeTrace;
			}
			else
			{
				tracecb = null;
			}

			LibGBHawk.GB_settracecallback(GB_Pntr, tracecb);

			_frame++;

			if (controller.IsPressed("P1 Power"))
			{
				HardReset();
			}

			_islag = true;

			do_frame(controller);

			if (_islag)
			{
				_lagcount++;
			}

			return true;
		}

		public void do_frame(IController controller)
		{
			LibGBHawk.GB_frame_advance(GB_Pntr, _controllerDeck.ReadPort1(controller),
												_controllerDeck.ReadAccX1(controller),
												_controllerDeck.ReadAccY1(controller), true, true);
		}

		public void do_single_step()
		{
			LibGBHawk.GB_do_single_step(GB_Pntr);
		}

		public void do_controller_check()
		{

		}

		public void GetControllerState(IController controller)
		{
			InputCallbacks.Call();
			controller_state = _controllerDeck.ReadPort1(controller);

			Acc_X_state = _controllerDeck.ReadAccX1(controller);
			Acc_Y_state = _controllerDeck.ReadAccY1(controller);
		}

		public int Frame => _frame;

		public string SystemId => "GB";

		public bool DeterministicEmulation { get; set; }

		public void ResetCounters()
		{
			_frame = 0;
			_lagcount = 0;
			_islag = false;
		}

		public void Dispose()
		{

		}

		#region Video provider

		public int[] _vidbuffer;

		public int[] frame_buffer;

		public int[] GetVideoBuffer()
		{
			return frame_buffer;
		}

		public void SendVideoBuffer()
		{

		}

		public int VirtualWidth => 160;
		public int VirtualHeight => 144;
		public int BufferWidth => 160;
		public int BufferHeight => 144;
		public int BackgroundColor => unchecked((int)0xFF000000);
		public int VsyncNumerator => 262144;
		public int VsyncDenominator => 4389;

		public static readonly uint[] color_palette_BW = { 0xFFFFFFFF , 0xFFAAAAAA, 0xFF555555, 0xFF000000 };
		public static readonly uint[] color_palette_Gr = { 0xFFA4C505, 0xFF88A905, 0xFF1D551D, 0xFF052505 };

		public uint[] color_palette = new uint[4];

		#endregion

		#region Audio

		public BlipBuffer blip = new BlipBuffer(4500);

		public int[] Aud = new int[9000];
		public uint num_samp;

		const int blipbuffsize = 4500;

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
			uint f_clock = LibGBHawk.GB_get_audio(GB_Pntr, Aud, ref num_samp);

			for (int i = 0; i < num_samp; i++)
			{
				blip.AddDelta((uint)Aud[i * 2], Aud[i * 2 + 1]);
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
	}
}
