using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Tests.Client.Common.Movie
{
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

#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
		public IInputCallbackSystem InputCallbacks => throw new NotImplementedException();
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations

		public FakeEmulator()
		{
			_serviceProvider = new(this);
		}

		public void Dispose() { }
		public bool FrameAdvance(IController controller, bool render, bool renderSound = true)
		{
			Frame++;
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
