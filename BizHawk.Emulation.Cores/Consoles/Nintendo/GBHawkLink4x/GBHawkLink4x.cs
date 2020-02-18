using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink4x
{
	[Core(
		"GBHawkLink4x",
		"",
		isPorted: false,
		isReleased: false)]
	[ServiceNotApplicable(new[] { typeof(IDriveLight) })]
	public partial class GBHawkLink4x : IEmulator, ISaveRam, IDebuggable, IStatable, IInputPollable, IRegionable,
	ISettable<GBHawkLink4x.GBLink4xSettings, GBHawkLink4x.GBLink4xSyncSettings>
	{
		// we want to create two GBHawk instances that we will run concurrently
		public GBHawk.GBHawk A;
		public GBHawk.GBHawk B;
		public GBHawk.GBHawk C;
		public GBHawk.GBHawk D;

		// if true, the link cable is currently connected
		private bool _cableconnected_LR = false;
		private bool _cableconnected_UD = false;
		private bool _cableconnected_X = false;
		private bool _cableconnected_4x = true;

		private bool do_2_next = false;

		public byte A_controller, B_controller, C_controller, D_controller;

		public bool do_frame_fill;

		//[CoreConstructor("GB", "GBC")]
		public GBHawkLink4x(CoreComm comm, GameInfo game_A, byte[] rom_A, GameInfo game_B, byte[] rom_B, GameInfo game_C, byte[] rom_C, GameInfo game_D, byte[] rom_D, /*string gameDbFn,*/ object settings, object syncSettings)
		{
			var ser = new BasicServiceProvider(this);

			Link4xSettings = (GBLink4xSettings)settings ?? new GBLink4xSettings();
			Link4xSyncSettings = (GBLink4xSyncSettings)syncSettings ?? new GBLink4xSyncSettings();
			_controllerDeck = new GBHawkLink4xControllerDeck(GBHawkLink4xControllerDeck.DefaultControllerName, GBHawkLink4xControllerDeck.DefaultControllerName, 
															 GBHawkLink4xControllerDeck.DefaultControllerName, GBHawkLink4xControllerDeck.DefaultControllerName);

			CoreComm = comm;

			var temp_set_A = new GBHawk.GBHawk.GBSettings();
			var temp_set_B = new GBHawk.GBHawk.GBSettings();
			var temp_set_C = new GBHawk.GBHawk.GBSettings();
			var temp_set_D = new GBHawk.GBHawk.GBSettings();

			var temp_sync_A = new GBHawk.GBHawk.GBSyncSettings();
			var temp_sync_B = new GBHawk.GBHawk.GBSyncSettings();
			var temp_sync_C = new GBHawk.GBHawk.GBSyncSettings();
			var temp_sync_D = new GBHawk.GBHawk.GBSyncSettings();

			temp_sync_A.ConsoleMode = Link4xSyncSettings.ConsoleMode_A;
			temp_sync_B.ConsoleMode = Link4xSyncSettings.ConsoleMode_B;
			temp_sync_C.ConsoleMode = Link4xSyncSettings.ConsoleMode_C;
			temp_sync_D.ConsoleMode = Link4xSyncSettings.ConsoleMode_D;

			temp_sync_A.GBACGB = Link4xSyncSettings.GBACGB;
			temp_sync_B.GBACGB = Link4xSyncSettings.GBACGB;
			temp_sync_C.GBACGB = Link4xSyncSettings.GBACGB;
			temp_sync_D.GBACGB = Link4xSyncSettings.GBACGB;

			temp_sync_A.RTCInitialTime = Link4xSyncSettings.RTCInitialTime_A;
			temp_sync_B.RTCInitialTime = Link4xSyncSettings.RTCInitialTime_B;
			temp_sync_C.RTCInitialTime = Link4xSyncSettings.RTCInitialTime_C;
			temp_sync_D.RTCInitialTime = Link4xSyncSettings.RTCInitialTime_D;
			temp_sync_A.RTCOffset = Link4xSyncSettings.RTCOffset_A;
			temp_sync_B.RTCOffset = Link4xSyncSettings.RTCOffset_B;
			temp_sync_C.RTCOffset = Link4xSyncSettings.RTCOffset_C;
			temp_sync_D.RTCOffset = Link4xSyncSettings.RTCOffset_D;

			A = new GBHawk.GBHawk(new CoreComm(comm.ShowMessage, comm.Notify) { CoreFileProvider = comm.CoreFileProvider },
				game_A, rom_A, temp_set_A, temp_sync_A);

			B = new GBHawk.GBHawk(new CoreComm(comm.ShowMessage, comm.Notify) { CoreFileProvider = comm.CoreFileProvider },
				game_B, rom_B, temp_set_B, temp_sync_B);

			C = new GBHawk.GBHawk(new CoreComm(comm.ShowMessage, comm.Notify) { CoreFileProvider = comm.CoreFileProvider },
				game_C, rom_C, temp_set_C, temp_sync_C);

			D = new GBHawk.GBHawk(new CoreComm(comm.ShowMessage, comm.Notify) { CoreFileProvider = comm.CoreFileProvider },
				game_D, rom_D, temp_set_D, temp_sync_D);

			ser.Register<IVideoProvider>(this);
			ser.Register<ISoundProvider>(this); 

			_tracer = new TraceBuffer { Header = A.cpu.TraceHeader };
			ser.Register<ITraceable>(_tracer);

			ServiceProvider = ser;

			_aStates = A.ServiceProvider.GetService<IStatable>();
			_bStates = B.ServiceProvider.GetService<IStatable>();
			_cStates = C.ServiceProvider.GetService<IStatable>();
			_dStates = D.ServiceProvider.GetService<IStatable>();

			SetupMemoryDomains();

			HardReset();
		}

		public void HardReset()
		{
			A.HardReset();
			B.HardReset();
			C.HardReset();
			D.HardReset();
		}

		public DisplayType Region => DisplayType.NTSC;

		public int _frame = 0;

		private readonly GBHawkLink4xControllerDeck _controllerDeck;

		private readonly ITraceable _tracer;

		private void ExecFetch(ushort addr)
		{
			uint flags = (uint)(MemoryCallbackFlags.AccessExecute);
			MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
		}
	}
}
