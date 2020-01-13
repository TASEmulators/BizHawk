using System;

using BizHawk.Emulation.Common;

using BizHawk.Emulation.Cores.Nintendo.GBHawk;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink3x
{
	[Core(
		"GBHawkLink3x",
		"",
		isPorted: false,
		isReleased: true)]
	[ServiceNotApplicable(typeof(IDriveLight))]
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

			Link3xSettings = (GBLink3xSettings)settings ?? new GBLink3xSettings();
			Link3xSyncSettings = (GBLink3xSyncSettings)syncSettings ?? new GBLink3xSyncSettings();
			_controllerDeck = new GBHawkLink3xControllerDeck(GBHawkLink3xControllerDeck.DefaultControllerName, GBHawkLink3xControllerDeck.DefaultControllerName, GBHawkLink3xControllerDeck.DefaultControllerName);

			CoreComm = comm;

			var temp_set_L = new GBHawk.GBHawk.GBSettings();
			var temp_set_C = new GBHawk.GBHawk.GBSettings();
			var temp_set_R = new GBHawk.GBHawk.GBSettings();

			var temp_sync_L = new GBHawk.GBHawk.GBSyncSettings();
			var temp_sync_C = new GBHawk.GBHawk.GBSyncSettings();
			var temp_sync_R = new GBHawk.GBHawk.GBSyncSettings();

			temp_sync_L.ConsoleMode = Link3xSyncSettings.ConsoleMode_L;
			temp_sync_C.ConsoleMode = Link3xSyncSettings.ConsoleMode_C;
			temp_sync_R.ConsoleMode = Link3xSyncSettings.ConsoleMode_R;

			temp_sync_L.RTCInitialTime = Link3xSyncSettings.RTCInitialTime_L;
			temp_sync_C.RTCInitialTime = Link3xSyncSettings.RTCInitialTime_C;
			temp_sync_R.RTCInitialTime = Link3xSyncSettings.RTCInitialTime_R;
			temp_sync_L.RTCOffset = Link3xSyncSettings.RTCOffset_L;
			temp_sync_C.RTCOffset = Link3xSyncSettings.RTCOffset_C;
			temp_sync_R.RTCOffset = Link3xSyncSettings.RTCOffset_R;

			L = new GBHawk.GBHawk(new CoreComm(comm.ShowMessage, comm.Notify) { CoreFileProvider = comm.CoreFileProvider },
				game_L, rom_L, temp_set_L, temp_sync_L);

			C = new GBHawk.GBHawk(new CoreComm(comm.ShowMessage, comm.Notify) { CoreFileProvider = comm.CoreFileProvider },
				game_C, rom_C, temp_set_C, temp_sync_C);

			R = new GBHawk.GBHawk(new CoreComm(comm.ShowMessage, comm.Notify) { CoreFileProvider = comm.CoreFileProvider },
				game_R, rom_R, temp_set_R, temp_sync_R);

			ser.Register<IVideoProvider>(this);
			ser.Register<ISoundProvider>(this); 

			_tracer = new TraceBuffer { Header = L.cpu.TraceHeader };
			ser.Register<ITraceable>(_tracer);

			ServiceProvider = ser;

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

		private void ExecFetch(ushort addr)
		{
			uint flags = (uint)(MemoryCallbackFlags.AccessExecute);
			MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
		}
	}
}
