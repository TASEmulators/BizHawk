using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	public partial class VectrexHawk : IEmulator, IVideoProvider
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _controllerDeck.Definition;

		byte joy1_LR, joy2_LR, joy1_UD, joy2_UD;

		public bool FrameAdvance(IController controller, bool render, bool rendersound)
		{
			if (_tracer.Enabled)
			{
				cpu.TraceCallback = s => _tracer.Put(s);
			}
			else
			{
				cpu.TraceCallback = null;
			}

			_frame++;

			if (controller.IsPressed("Power"))
			{
				HardReset();
			}

			if (controller.IsPressed("Reset"))
			{
				SoftReset();
			}

			_islag = true;

			// button inputs go to port 14 in the audio registers
			audio.Register[14] = (byte)(_controllerDeck.ReadPort1(controller) & 0xF);
			audio.Register[14] |= (byte)(_controllerDeck.ReadPort2(controller) << 4);

			if (ControllerDefinition.Name == "Vectrex Analog Controller")
			{
				// joystick position is based on pot reading
				joy1_LR = (byte)(255 - (Math.Floor(controller.GetFloat("P1 Stick X")) + 128));
				joy1_UD = (byte)(Math.Floor(controller.GetFloat("P1 Stick Y")) + 128);
				joy2_LR = (byte)(255 - (Math.Floor(controller.GetFloat("P2 Stick X")) + 128));
				joy2_UD = (byte)(Math.Floor(controller.GetFloat("P2 Stick Y")) + 128);
			}
			else
			{
				// most games just use digital reading, so have a digital option for simplicity
				// On vectrex there is no such thing as pressing left + right or up + down
				// so convention will be up and right dominate
				joy1_UD = joy1_LR = joy2_UD = joy2_LR = 128;

				if (controller.IsPressed("P1 Down")) { joy1_UD = 0xFF; }
				if (controller.IsPressed("P1 Up")) { joy1_UD = 0; }
				if (controller.IsPressed("P1 Left")) { joy1_LR = 0xFF; }
				if (controller.IsPressed("P1 Right")) { joy1_LR = 0; }

				if (controller.IsPressed("P2 Down")) { joy2_UD = 0xFF; }
				if (controller.IsPressed("P2 Up")) { joy2_UD = 0; }
				if (controller.IsPressed("P2 Left")) { joy2_LR = 0xFF; }
				if (controller.IsPressed("P2 Right")) { joy2_LR = 0; }
			}

			frame_end = false;

			do_frame();

			if (_islag)
			{
				_lagcount++;
			}

			return true;
		}

		public void do_frame()
		{
			for (int i = 0; i < 30000; i++)
			//while (!frame_end)
			{
				internal_state_tick();
				audio.tick();
				ppu.tick();
				cpu.ExecuteOne();

				if (frame_end)
				{
					get_video_frame();
					frame_end = false;
				}
			}
		}

		public int Frame => _frame;

		public string SystemId => "VEC"; 

		public bool DeterministicEmulation { get; set; }

		public void ResetCounters()
		{
			_frame = 0;
			_lagcount = 0;
			_islag = false;
		}

		public void Dispose()
		{
			audio.DisposeSound();
		}

		#region Video provider

		public int _frameHz = 50;

		public int[] _vidbuffer;
		public int[] _framebuffer;

		public int[] GetVideoBuffer()
		{
			return _framebuffer;		
		}

		public void get_video_frame()
		{
			for (int i = 0; i < _vidbuffer.Length; i++)
			{
				_framebuffer[i] = _vidbuffer[i];
				_vidbuffer[i] = 0;
			}
			
		}

		public int VirtualWidth => 256 + 4;
		public int VirtualHeight => 384 + 4;
		public int BufferWidth => 256 + 4;
		public int BufferHeight => 384 + 4;
		public int BackgroundColor => unchecked((int)0xFF000000);
		public int VsyncNumerator => _frameHz;
		public int VsyncDenominator => 1;

		#endregion
	}
}
