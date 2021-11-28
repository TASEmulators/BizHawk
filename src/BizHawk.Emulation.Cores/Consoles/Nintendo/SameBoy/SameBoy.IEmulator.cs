using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Sameboy
{
	public partial class Sameboy : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider => _serviceProvider;

		public ControllerDefinition ControllerDefinition { get; } = Gameboy.Gameboy.CreateControllerDefinition(false, false);

		private static readonly IReadOnlyList<string> GB_BUTTON_ORDER_IN_BITMASK = new[] { "Start", "Select", "B", "A", "Down", "Up", "Left", "Right", };

		private LibSameboy.Buttons FrameAdvancePrep(IController controller)
		{
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

			IsLagFrame = true;

			LibSameboy.sameboy_settracecallback(SameboyState, Tracer.IsEnabled() ? _tracecb : null);

			return (LibSameboy.Buttons)b;
		}

		public bool FrameAdvance(IController controller, bool render, bool rendersound)
		{
			var input = FrameAdvancePrep(controller);

			LibSameboy.sameboy_frameadvance(SameboyState, input, VideoBuffer, render);

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

		public bool DeterministicEmulation => true;

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
