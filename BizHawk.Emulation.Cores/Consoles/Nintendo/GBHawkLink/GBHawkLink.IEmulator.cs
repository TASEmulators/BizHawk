using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink
{
	public partial class GBHawkLink : IEmulator, IVideoProvider
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _controllerDeck.Definition;

		public byte controller_state;
		public bool in_vblank_old;
		public bool in_vblank;
		public bool vblank_rise;

		public void FrameAdvance(IController controller, bool render, bool rendersound)
		{
			//Console.WriteLine("-----------------------FRAME-----------------------");
			if (_tracer.Enabled)
			{
				L.cpu.TraceCallback = s => _tracer.Put(s);
			}
			else
			{
				L.cpu.TraceCallback = null;
			}

			_frame++;

			if (controller.IsPressed("Power"))
			{
				HardReset();
			}

			_islag = true;

			GetControllerState(controller);

			do_frame();

			_islag = L._islag;

			if (_islag)
			{
				_lagcount++;
			}
		}

		public void do_frame()
		{
			L.do_frame();
			R.do_frame();
		}

		public void GetControllerState(IController controller)
		{
			InputCallbacks.Call();
			L.controller_state = _controllerDeck.ReadPort1(controller);
			R.controller_state = _controllerDeck.ReadPort2(controller);
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

		public CoreComm CoreComm { get; }

		public void Dispose()
		{
			L.Dispose();
			R.Dispose();
		}

		#region Video provider

		public int _frameHz = 60;

		public int[] _vidbuffer = new int[160 * 2 * 144];
		public int[] buff_L;
		public int[] buff_R;

		public int[] GetVideoBuffer()
		{
			// combine the 2 video buffers from the instances
			buff_L = L.GetVideoBuffer();
			buff_R = R.GetVideoBuffer();

			for (int i = 0; i < 144; i++)
			{
				for (int j = 0; j < 160; j++)
				{
					_vidbuffer[i * 320 + j] = buff_L[i * 160 + j];
					_vidbuffer[i * 320 + j + 160] = buff_R[i * 160 + j];
				}				
			}

			return _vidbuffer;		
		}

		public int VirtualWidth => 160 * 2;
		public int VirtualHeight => 144;
		public int BufferWidth => 160 * 2;
		public int BufferHeight => 144;
		public int BackgroundColor => unchecked((int)0xFF000000);
		public int VsyncNumerator => _frameHz;
		public int VsyncDenominator => 1;

		public static readonly uint[] color_palette_BW = { 0xFFFFFFFF , 0xFFAAAAAA, 0xFF555555, 0xFF000000 };
		public static readonly uint[] color_palette_Gr = { 0xFFA4C505, 0xFF88A905, 0xFF1D551D, 0xFF052505 };

		public uint[] color_palette = new uint[4];

		#endregion
	}
}
