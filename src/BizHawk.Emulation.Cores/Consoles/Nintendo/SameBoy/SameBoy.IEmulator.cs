using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Sameboy
{
	public partial class Sameboy : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider => _serviceProvider;

		public ControllerDefinition ControllerDefinition { get; }

		private IController _controller = NullController.Instance;

		private static readonly IReadOnlyList<string> GB_BUTTON_ORDER_IN_BITMASK = new[] { "Start", "Select", "B", "A", "Down", "Up", "Left", "Right", };

		private readonly int _firstTrack;
		private readonly int _lastTrack;
		private int _curTrack = 0;
		private bool _switchingTrack = false;

		private LibSameboy.Buttons FrameAdvancePrep(IController controller)
		{
			_controller = controller;

			uint b = 0;
			for (var i = 0; i < 8; i++)
			{
				b <<= 1;
				if (controller.IsPressed(GB_BUTTON_ORDER_IN_BITMASK[i])) b |= 1;
			}

			if (controller.IsPressed("Power"))
			{
				LibSameboy.sameboy_reset(SameboyState);
			}

			var prevTrack = controller.IsPressed("Previous Track");
			var nextTrack = controller.IsPressed("Next Track");

			if (!_switchingTrack)
			{
				if (prevTrack)
				{
					if (_curTrack != _firstTrack)
					{
						_curTrack--;
						LibSameboy.sameboy_switchgbstrack(SameboyState, _curTrack);
						Comm.Notify($"Switching to Track {_curTrack}", null);
					}
				}
				else if (nextTrack)
				{
					if (_curTrack != _lastTrack)
					{
						_curTrack++;
						LibSameboy.sameboy_switchgbstrack(SameboyState, _curTrack);
						Comm.Notify($"Switching to Track {_curTrack}", null);
					}
				}
			}

			_switchingTrack = prevTrack || nextTrack;

			IsLagFrame = true;

			LibSameboy.sameboy_settracecallback(SameboyState, Tracer.IsEnabled() ? _tracecb : null);

			return (LibSameboy.Buttons)b;
		}

		// copy pasting GBHawk here...

		private readonly bool _hasAcc;
		private double theta, phi, theta_prev, phi_prev, phi_prev_2;

		private ushort GetAccX(IController c)
		{
			if (!_hasAcc)
			{
				return 0;
			}

			theta_prev = theta;
			phi_prev_2 = phi_prev;
			phi_prev = phi;

			theta = c.AxisValue("Tilt Y") * Math.PI / 180.0;
			phi = c.AxisValue("Tilt X") * Math.PI / 180.0;

			double temp = (double)(Math.Cos(theta) * Math.Sin(phi));

			double temp2 = (double)((phi - 2 * phi_prev + phi_prev_2) * 59.7275 * 59.7275 * 0.1);

			return (ushort)(0x8370 - Math.Floor(temp * 216) - temp2);
		}

		private ushort GetAccY()
		{
			if (!_hasAcc)
			{
				return 0;
			}

			double temp = (double)Math.Sin(theta);

			double temp2 = (double)(Math.Pow((theta - theta_prev) * 59.7275, 2) * 0.15);

			return (ushort)(0x8370 - Math.Floor(temp * 216) + temp2);
		}

		public bool FrameAdvance(IController controller, bool render, bool rendersound)
		{
			var buttons = FrameAdvancePrep(controller);

			LibSameboy.sameboy_frameadvance(SameboyState, buttons, GetAccX(controller), GetAccY(), _soundoutbuff, ref _soundoutbuffcontains, VideoBuffer, render, _settings.ShowBorder);

			if (!rendersound)
			{
				DiscardSamples();
			}

			FrameAdvancePost();

			return true;
		}

		private void FrameAdvancePost()
		{
			if (IsLagFrame)
			{
				LagCount++;
			}

			Frame++;

			if (_scanlinecbline == -1)
			{
				_scanlinecb?.Invoke(LibSameboy.sameboy_cpuread(SameboyState, 0xFF40));
			}
		}

		public int Frame { get; private set; } = 0;

		public string SystemId => VSystemID.Raw.GB;

		public bool DeterministicEmulation { get; }

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
			CycleCount = 0;
		}

		public void Dispose()
		{
			if (SameboyState != IntPtr.Zero)
			{
				LibSameboy.sameboy_destroy(SameboyState);
				SameboyState = IntPtr.Zero;
			}
		}
	}
}
