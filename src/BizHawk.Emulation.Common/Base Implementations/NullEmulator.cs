#nullable disable

using System.Threading;

namespace BizHawk.Emulation.Common
{
	[Core("NullHawk", "")]
	[ServiceNotApplicable(
		typeof(IVideoProvider),
		typeof(IBoardInfo),
		typeof(ICodeDataLogger),
		typeof(IDebuggable),
		typeof(IDisassemblable),
		typeof(IInputPollable),
		typeof(IMemoryDomains),
		typeof(IRegionable),
		typeof(ISaveRam),
		typeof(ISettable<,>),
		typeof(ISoundProvider),
		typeof(IStatable),
		typeof(ITraceable)
	)]
	public class NullEmulator : IEmulator
	{
		public NullEmulator()
		{
			ServiceProvider = new BasicServiceProvider(this);
		}

		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => NullController.Instance.Definition;

		public bool FrameAdvance(IController controller, bool render, bool renderSound)
		{
			// real cores wouldn't do something like this, but this just keeps speed reasonable
			// if all throttles are off
			Thread.Sleep(5);
			return true;
		}

		public int Frame => 0;

		public string SystemId => VSystemID.Raw.NULL;

		public bool DeterministicEmulation => true;

		public void ResetCounters()
		{
		}

		public void Dispose()
		{
		}
	}
}
