using BizHawk.API.ApiHawk;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink3x
{
	[Core(
		"GBHawkLink3x",
		"",
		isPorted: false,
		isReleased: true)]
	[ServiceNotApplicable(new[] { typeof(IDriveLight) })]
	public partial class GBHawkLink3x : IEmulator, ISaveRam, IDebuggable, IStatable, IInputPollable, IRegionable,
	ISettable<GBHawkLink3x.GBLink3xSettings, GBHawkLink3x.GBLink3xSyncSettings>
	{
		// we want to create two GBHawk instances that we will run concurrently
		// maybe up to 4 eventually?
		public GBHawk.GBHawk L;
		public GBHawk.GBHawk C;
		public GBHawk.GBHawk R;

		// if true, the link cable is currently connected
		private bool _cableconnected_LC = false;
		private bool _cableconnected_CR = false;
		private bool _cableconnected_RL = false;

		private bool do_2_next = false;

		public byte L_controller, C_controller, R_controller;

		public bool do_frame_fill;

		//[CoreConstructor("GB", "GBC")]
		public GBHawkLink3x(CoreComm comm, GameInfo game_L, byte[] rom_L, GameInfo game_C, byte[] rom_C, GameInfo game_R, byte[] rom_R, /*string gameDbFn,*/ object settings, object syncSettings)
		{
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;

			Link3xSettings = (GBLink3xSettings)settings ?? new GBLink3xSettings();
			Link3xSyncSettings = (GBLink3xSyncSettings)syncSettings ?? new GBLink3xSyncSettings();
			_controllerDeck = new GBHawkLink3xControllerDeck(GBHawkLink3xControllerDeck.DefaultControllerName, GBHawkLink3xControllerDeck.DefaultControllerName, GBHawkLink3xControllerDeck.DefaultControllerName);

			var tempSetL = new GBHawk.GBHawk.GBSettings();
			var tempSetC = new GBHawk.GBHawk.GBSettings();
			var tempSetR = new GBHawk.GBHawk.GBSettings();

			var tempSyncL = new GBHawk.GBHawk.GBSyncSettings();
			var tempSyncC = new GBHawk.GBHawk.GBSyncSettings();
			var tempSyncR = new GBHawk.GBHawk.GBSyncSettings();

			tempSyncL.ConsoleMode = Link3xSyncSettings.ConsoleMode_L;
			tempSyncC.ConsoleMode = Link3xSyncSettings.ConsoleMode_C;
			tempSyncR.ConsoleMode = Link3xSyncSettings.ConsoleMode_R;

			tempSyncL.GBACGB = Link3xSyncSettings.GBACGB;
			tempSyncC.GBACGB = Link3xSyncSettings.GBACGB;
			tempSyncR.GBACGB = Link3xSyncSettings.GBACGB;

			tempSyncL.RTCInitialTime = Link3xSyncSettings.RTCInitialTime_L;
			tempSyncC.RTCInitialTime = Link3xSyncSettings.RTCInitialTime_C;
			tempSyncR.RTCInitialTime = Link3xSyncSettings.RTCInitialTime_R;
			tempSyncL.RTCOffset = Link3xSyncSettings.RTCOffset_L;
			tempSyncC.RTCOffset = Link3xSyncSettings.RTCOffset_C;
			tempSyncR.RTCOffset = Link3xSyncSettings.RTCOffset_R;

			L = new GBHawk.GBHawk(comm, game_L, rom_L, tempSetL, tempSyncL);
			C = new GBHawk.GBHawk(comm, game_C, rom_C, tempSetC, tempSyncC);
			R = new GBHawk.GBHawk(comm, game_R, rom_R, tempSetR, tempSyncR);

			ser.Register<IVideoProvider>(this);
			ser.Register<ISoundProvider>(this); 

			_tracer = new TraceBuffer { Header = L.cpu.TraceHeader };
			ser.Register(_tracer);

			_lStates = L.ServiceProvider.GetService<IStatable>();
			_cStates = C.ServiceProvider.GetService<IStatable>();
			_rStates = R.ServiceProvider.GetService<IStatable>();

			SetupMemoryDomains();
			HardReset();
		}

		public void HardReset()
		{
			L.HardReset();
			C.HardReset();
			R.HardReset();
		}

		public DisplayType Region => DisplayType.NTSC;

		public int _frame = 0;

		private readonly GBHawkLink3xControllerDeck _controllerDeck;

		private readonly ITraceable _tracer;
	}
}
