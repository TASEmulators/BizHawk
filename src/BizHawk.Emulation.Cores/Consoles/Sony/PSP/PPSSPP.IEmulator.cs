using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sony.PSP
{
	public partial class PPSSPP : IEmulator
	{
		private readonly BasicServiceProvider _serviceProvider;

		public IEmulatorServiceProvider ServiceProvider => _serviceProvider;

		public ControllerDefinition ControllerDefinition { get; private set; }

		public int Frame { get; set; }

		public string SystemId => VSystemID.Raw.PSP;

		public bool DeterministicEmulation { get; }

		protected LibPPSSPP.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			var fi = new LibPPSSPP.FrameInfo();

			// Disc management
			if (_isMultidisc)
			{
				if (controller.IsPressed("Next Disc")) SelectNextDisc();
				if (controller.IsPressed("Prev Disc")) SelectPrevDisc();
			}

			fi.input.Up = controller.IsPressed($"P1 {JoystickButtons.Up}") ? 1 : 0;
			fi.input.Down = controller.IsPressed($"P1 {JoystickButtons.Down}") ? 1 : 0;
			fi.input.Left = controller.IsPressed($"P1 {JoystickButtons.Left}") ? 1 : 0;
			fi.input.Right = controller.IsPressed($"P1 {JoystickButtons.Right}") ? 1 : 0;
			fi.input.Start = controller.IsPressed($"P1 {JoystickButtons.Start}") ? 1 : 0;
			fi.input.Select = controller.IsPressed($"P1 {JoystickButtons.Select}") ? 1 : 0;
			fi.input.ButtonSquare = controller.IsPressed($"P1 {JoystickButtons.ButtonSquare}") ? 1 : 0;
			fi.input.ButtonTriangle = controller.IsPressed($"P1 {JoystickButtons.ButtonTriangle}") ? 1 : 0;
			fi.input.ButtonCircle = controller.IsPressed($"P1 {JoystickButtons.ButtonCircle}") ? 1 : 0;
			fi.input.ButtonCross = controller.IsPressed($"P1 {JoystickButtons.ButtonCross}") ? 1 : 0;
			fi.input.ButtonLTrigger = controller.IsPressed($"P1 {JoystickButtons.ButtonLTrigger}") ? 1 : 0;
			fi.input.ButtonRTrigger = controller.IsPressed($"P1 {JoystickButtons.ButtonRTrigger}") ? 1 : 0;
			fi.input.RightAnalogX = controller.AxisValue($"P1 {JoystickAxes.RightAnalogX}");
			fi.input.RightAnalogY = controller.AxisValue($"P1 {JoystickAxes.RightAnalogY}");
			fi.input.LeftAnalogX = controller.AxisValue($"P1 {JoystickAxes.LeftAnalogX}");
			fi.input.LeftAnalogY = controller.AxisValue($"P1 {JoystickAxes.LeftAnalogY}");

			DriveLightOn = false;

			return fi;
		}

		public bool FrameAdvance(IController controller, bool render, bool renderSound = true)
		{

			_controller = controller;
			IsLagFrame = true;

			/*if (_controller.IsPressed("Reset"))
			{
				_core.Encore_Reset(_context);
				// memory domain pointers are no longer valid, reset them
				WireMemoryDomains();
			}*/

			var f = FrameAdvancePrep(controller, render, renderSound);
			_libPPSSPP.FrameAdvance(f);

			OnVideoRefresh();

			if (renderSound)
			{
				ProcessSound();
			}

			Frame++;
			if (IsLagFrame)
			{
				LagCount++;
			}



			return true;
		}

		private void OnVideoRefresh()
		{
			VideoDirty = true;
			/*
			_core.Encore_GetVideoBufferDimensions(_context, out _encoreVideoProvider.BW, out _encoreVideoProvider.BH);
			_encoreVideoProvider.VideoDirty = true;

			_core.Encore_GetTouchScreenLayout(_context, out var x, out var y, out var width, out var height, out var rotated, out var enabled);
			TouchScreenRectangle = new(x, y, width, height);
			TouchScreenRotated = rotated;
			TouchScreenEnabled = enabled;
			*/
		}

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		private bool _disposed;

		public void Dispose()
		{
			_libPPSSPP.Deinit();

			if (_disposed)
			{
				return;
			}

			_disposed = true;
		}
	}
}
