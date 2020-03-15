namespace BizHawk.Emulation.Common
{
	[Core("NullHawk", "", false, true)]
	[ServiceNotApplicable(new[] {
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
	public class NullEmulator : IEmulator, IVideoProvider
	{
		private readonly int[] _frameBuffer = new int[NullVideo.DefaultWidth * NullVideo.DefaultHeight];

		public NullEmulator()
		{
			ServiceProvider = new BasicServiceProvider(this);
		}

		#region IEmulator

		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => NullController.Instance.Definition;

		public bool FrameAdvance(IController controller, bool render, bool renderSound) => true;

		public int Frame => 0;

		public string SystemId => "NULL";

		public bool DeterministicEmulation => true;

		public void ResetCounters()
		{
		}

		public string BoardName => null;

		public void Dispose()
		{
		}

		#endregion

		#region IVideoProvider

		public int[] GetVideoBuffer() => _frameBuffer;

		public int VirtualWidth => NullVideo.DefaultWidth;

		public int VirtualHeight => NullVideo.DefaultHeight;

		public int BufferWidth => NullVideo.DefaultWidth;

		public int BufferHeight => NullVideo.DefaultHeight;

		public int BackgroundColor => NullVideo.DefaultBackgroundColor;

		public int VsyncNumerator => NullVideo.DefaultVsyncNum;

		public int VsyncDenominator => NullVideo.DefaultVsyncDen;

		#endregion
	}
}
