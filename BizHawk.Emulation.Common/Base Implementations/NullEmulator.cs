namespace BizHawk.Emulation.Common
{
	[Core("NullHawk", "", false, true)]
	[ServiceNotApplicable(new[] {
		typeof(IVideoProvider),
		typeof(IBoardInfo),
		typeof(ICodeDataLogger),
		typeof(IDebuggable),
		typeof(IDisassemblable),
		typeof(IDriveLight),
		typeof(IInputPollable),
		typeof(IMemoryDomains),
		typeof(IRegionable),
		typeof(ISaveRam),
		typeof(ISettable<,>),
		typeof(ISoundProvider),
		typeof(IStatable),
		typeof(ITraceable)
	})]
	public class NullEmulator : IEmulator
	{
		public NullEmulator()
		{
			ServiceProvider = new BasicServiceProvider(this);
		}

		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => NullController.Instance.Definition;

		public bool FrameAdvance(IController controller, bool render, bool renderSound) => true;

		public int Frame => 0;

		public string SystemId => "NULL";

		public bool DeterministicEmulation => true;

		public void ResetCounters()
		{
		}

		public void Dispose()
		{
		}
	}
}
