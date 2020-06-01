using BizHawk.API.ApiHawk;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SubGBHawk
{
	[Core(
		CoreNames.SubGbHawk,
		"",
		isPorted: false,
		isReleased: true)]
	[ServiceNotApplicable(new [] { typeof(IDriveLight) })]
	public partial class SubGBHawk : IEmulator, IStatable, IInputPollable,
		ISettable<GBHawk.GBHawk.GBSettings, GBHawk.GBHawk.GBSyncSettings>
	{
		[CoreConstructor(new[] { "GB", "GBC" })]
		public SubGBHawk(CoreComm comm, GameInfo game, byte[] rom, /*string gameDbFn,*/ object settings, object syncSettings)
		{
			
			var subGBSettings = (GBHawk.GBHawk.GBSettings)settings ?? new GBHawk.GBHawk.GBSettings();
			var subGBSyncSettings = (GBHawk.GBHawk.GBSyncSettings)syncSettings ?? new GBHawk.GBHawk.GBSyncSettings();

			_GBCore = new GBHawk.GBHawk(comm, game, rom, subGBSettings, subGBSyncSettings);

			HardReset();
			current_cycle = 0;
			Cycle_CNT = 0;

			_GBStatable = _GBCore.ServiceProvider.GetService<IStatable>();

			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;

			ser.Register(_GBCore.ServiceProvider.GetService<IVideoProvider>());
			ser.Register(_GBCore.ServiceProvider.GetService<ISoundProvider>());
			ser.Register(_GBCore.ServiceProvider.GetService<ITraceable>());
			ser.Register(_GBCore.ServiceProvider.GetService<IMemoryDomains>());
			ser.Register(_GBCore.ServiceProvider.GetService<ISaveRam>());
			ser.Register(_GBCore.ServiceProvider.GetService<IDebuggable>());
			ser.Register(_GBCore.ServiceProvider.GetService<IRegionable>());
			ser.Register(_GBCore.ServiceProvider.GetService<ICodeDataLogger>());

			_tracer = new TraceBuffer { Header = _GBCore.cpu.TraceHeader };
			ser.Register(_tracer);

			_GBCore.ControllerDefinition.AxisControls.Add("Input Cycle");
			_GBCore.ControllerDefinition.AxisRanges.Add(new ControllerDefinition.AxisRange(0, 70224, 70224));
		}

		public GBHawk.GBHawk _GBCore;

		// needed for movies to accurately calculate timing
		public long Cycle_CNT;

		public void HardReset() => _GBCore.HardReset();

		private int _frame;

		private readonly ITraceable _tracer;

		public GBHawk.GBHawk.GBSettings GetSettings() => _GBCore.GetSettings();
		public GBHawk.GBHawk.GBSyncSettings GetSyncSettings() => _GBCore.GetSyncSettings();
		public PutSettingsDirtyBits PutSettings(GBHawk.GBHawk.GBSettings o) => _GBCore.PutSettings(o);
		public PutSettingsDirtyBits PutSyncSettings(GBHawk.GBHawk.GBSyncSettings o) => _GBCore.PutSyncSettings(o);
	}
}
