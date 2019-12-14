namespace BizHawk.Emulation.Common
{
	[Core("NullHawk", "", false, true)]
	[ServiceNotApplicable(typeof(IStatable), typeof(ISaveRam), typeof(IDriveLight), typeof(ICodeDataLogger), typeof(IMemoryDomains), typeof(ISettable<,>),
		typeof(IDebuggable), typeof(IDisassemblable), typeof(IInputPollable), typeof(IRegionable), typeof(ITraceable), typeof(IBoardInfo), typeof(ISoundProvider))]
	public class NullEmulator : IEmulator, IVideoProvider
	{
		private readonly int[] _frameBuffer = new int[NullVideo.DefaultWidth * NullVideo.DefaultHeight];

		public NullEmulator(CoreComm comm)
		{
			ServiceProvider = new BasicServiceProvider(this);
			CoreComm = comm;
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

		public CoreComm CoreComm { get; }

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
