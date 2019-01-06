using System;

using BizHawk.Emulation.Common;

using BizHawk.Emulation.Cores.Nintendo.NES;

namespace BizHawk.Emulation.Cores.Nintendo.SubNESHawk
{
	[Core(
		"SubNESHawk",
		"",
		isPorted: false,
		isReleased: false)]
	[ServiceNotApplicable(typeof(IDriveLight))]
	public partial class SubNESHawk : IEmulator, ISaveRam, IDebuggable, IStatable, IInputPollable, IRegionable,
	ISettable<SubNESHawk.SubNESHawkSettings, SubNESHawk.SubNESHawkSyncSettings>
	{
		public NES.NES subnes;

		[CoreConstructor("NES")]
		public SubNESHawk(CoreComm comm, GameInfo game, byte[] rom, /*string gameDbFn,*/ object settings, object syncSettings)
		{
			var ser = new BasicServiceProvider(this);

			subnesSettings = (SubNESHawkSettings)settings ?? new SubNESHawkSettings();
			subnesSyncSettings = (SubNESHawkSyncSettings)syncSettings ?? new SubNESHawkSyncSettings();
			_controllerDeck = new SubNESHawkControllerDeck(SubNESHawkControllerDeck.DefaultControllerName, SubNESHawkControllerDeck.DefaultControllerName);

			CoreComm = comm;

			var temp_set = new NES.NES.NESSettings();

			var temp_sync = new NES.NES.NESSyncSettings();

			subnes = new NES.NES(new CoreComm(comm.ShowMessage, comm.Notify) { CoreFileProvider = comm.CoreFileProvider },
				game, rom, temp_set, temp_sync);

			ser.Register<IVideoProvider>(subnes.videoProvider);
			ser.Register<ISoundProvider>(subnes.magicSoundProvider); 

			_tracer = new TraceBuffer { Header = subnes.cpu.TraceHeader };
			ser.Register<ITraceable>(_tracer);

			ServiceProvider = ser;

			SetupMemoryDomains();

			HardReset();

			// input override for subframe input
			subnes.use_sub_input = true;
		}

		public void HardReset()
		{
			subnes.HardReset();
		}

		public void SoftReset()
		{
			subnes.Board.NESSoftReset();
			subnes.cpu.NESSoftReset();
			subnes.apu.NESSoftReset();
			subnes.ppu.NESSoftReset();
		}

		public DisplayType Region => DisplayType.NTSC;

		public int _frame = 0;

		private readonly SubNESHawkControllerDeck _controllerDeck;

		private readonly ITraceable _tracer;

		private void ExecFetch(ushort addr)
		{
			MemoryCallbacks.CallExecutes(addr, "System Bus");
		}
	}
}
