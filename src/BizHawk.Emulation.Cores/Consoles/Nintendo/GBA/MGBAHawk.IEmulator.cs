using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBAHawk : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => GBAController;

		public bool FrameAdvance(IController controller, bool render, bool renderSound = true)
		{
			_controller = controller;

			if (controller.IsPressed("Power"))
			{
				LibmGBA.BizReset(Core);

				// BizReset caused memorydomain pointers to change.
				WireMemoryDomainPointers();
			}

			LibmGBA.BizSetTraceCallback(Core, Tracer.IsEnabled() ? _tracecb : null);

			IsLagFrame = LibmGBA.BizAdvance(
				Core,
				LibmGBA.GetButtons(controller),
				render ? _videobuff : _dummyvideobuff,
				ref _nsamp,
				renderSound ? _soundbuff : _dummysoundbuff,
				RTCTime(),
				(short)controller.AxisValue("Tilt X"),
				(short)controller.AxisValue("Tilt Y"),
				(short)controller.AxisValue("Tilt Z"),
				(byte)(255 - controller.AxisValue("Light Sensor")));

			if (IsLagFrame)
			{
				LagCount++;
			}

			// this should be called in hblank on the appropriate line, but until we implement that, just do it here
			_scanlinecb?.Invoke();

			Frame++;

			return true;
		}

		public int Frame { get; private set; }

		public string SystemId => VSystemID.Raw.GBA;

		public bool DeterministicEmulation { get; }

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		public void Dispose()
		{
			if (Core != IntPtr.Zero)
			{
				LibmGBA.BizDestroy(Core);
				Core = IntPtr.Zero;
				_memoryCallbacks.Dispose();
			}
		}

		public static readonly ControllerDefinition GBAController = new ControllerDefinition("GBA Controller")
		{
			BoolButtons = { "Up", "Down", "Left", "Right", "Start", "Select", "B", "A", "L", "R", "Power" },
			HapticsChannels = { "Rumble" }
		}.AddXYZTriple("Tilt {0}", (-32767).RangeTo(32767), 0)
			.AddAxis("Light Sensor", 0.RangeTo(255), 0)
			.MakeImmutable();
	}
}
