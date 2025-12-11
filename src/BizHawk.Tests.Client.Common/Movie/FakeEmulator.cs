using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Tests.Client.Common.Movie
{
	[Core("Fake", "Author", false, false)]
	internal class FakeEmulator : IEmulator, IStatable, IInputPollable
	{
		private BasicServiceProvider _serviceProvider;
		public IEmulatorServiceProvider ServiceProvider => _serviceProvider;

		private static readonly ControllerDefinition _cd = new ControllerDefinition("fake controller")
		{
			BoolButtons = { "A", "B" },
		}
			.AddAxis("Stick", (-100).RangeTo(100), 0)
			.MakeImmutable();

		static FakeEmulator()
		{
			_cd.BuildMnemonicsCache("fake");
		}

		public ControllerDefinition ControllerDefinition => _cd;

		public int Frame { get; set; }

		public string SystemId => "fake";

		public bool DeterministicEmulation => true;

		public bool AvoidRewind => false;

		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }

		private InputCallbackSystem _inputCallbacks = new();
		public IInputCallbackSystem InputCallbacks => _inputCallbacks;

		public FakeEmulator()
		{
			_serviceProvider = new(this);
		}

		public bool PollInputOnFrameAdvance = true;

		public void Dispose() { }
		public bool FrameAdvance(IController controller, bool render, bool renderSound = true)
		{
			Frame++;
			if (PollInputOnFrameAdvance) InputCallbacks.Call();
			return true;
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			Frame = reader.ReadInt32();
			LagCount = reader.ReadInt32();
			IsLagFrame = reader.ReadBoolean();
		}

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			writer.Write(Frame);
			writer.Write(LagCount);
			writer.Write(IsLagFrame);
		}
	}
}
