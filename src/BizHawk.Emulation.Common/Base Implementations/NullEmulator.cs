#nullable disable

using System.Threading;

namespace BizHawk.Emulation.Common
{
	[Core("NullHawk", "")]
	[ServiceNotApplicable(typeof(IVideoProvider))]
	[ServiceNotApplicable(typeof(IBoardInfo))]
	[ServiceNotApplicable(typeof(ICodeDataLogger))]
	[ServiceNotApplicable(typeof(IDebuggable))]
	[ServiceNotApplicable(typeof(IDisassemblable))]
	[ServiceNotApplicable(typeof(IInputPollable))]
	[ServiceNotApplicable(typeof(IMemoryDomains))]
	[ServiceNotApplicable(typeof(IRegionable))]
	[ServiceNotApplicable(typeof(ISaveRam))]
	[ServiceNotApplicable(typeof(ISettable<,>))]
	[ServiceNotApplicable(typeof(ISoundProvider))]
	[ServiceNotApplicable(typeof(IStatable))]
	[ServiceNotApplicable(typeof(ITraceable))]
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
