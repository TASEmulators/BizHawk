using System;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.Components.LR35902;

using BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.GBHawk;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink
{
	[Core(
		"GBHawkLink",
		"",
		isPorted: false,
		isReleased: true)]
	[ServiceNotApplicable(typeof(IDriveLight))]
	public partial class GBHawkLink : IEmulator, ISaveRam, IDebuggable, IStatable, IInputPollable, IRegionable, ILinkable,
	ISettable<GBHawkLink.GBLinkSettings, GBHawkLink.GBLinkSyncSettings>
	{
		// we want to create two GBHawk instances that we will run concurrently
		// maybe up to 4 eventually?
		public GBHawk.GBHawk L;
		public GBHawk.GBHawk R;

		//[CoreConstructor("GB", "GBC")]
		public GBHawkLink(CoreComm comm, GameInfo game_L, byte[] rom_L, GameInfo game_R, byte[] rom_R, /*string gameDbFn,*/ object settings, object syncSettings)
		{
			var ser = new BasicServiceProvider(this);

			GBLinkSettings linkSettings = (GBLinkSettings)settings ?? new GBLinkSettings();
			GBLinkSyncSettings linkSyncSettings = (GBLinkSyncSettings)syncSettings ?? new GBLinkSyncSettings();
			_controllerDeck = new GBHawkLinkControllerDeck(GBHawkControllerDeck.DefaultControllerName, GBHawkControllerDeck.DefaultControllerName);

			CoreComm = comm;

			L = new GBHawk.GBHawk(new CoreComm(comm.ShowMessage, comm.Notify) { CoreFileProvider = comm.CoreFileProvider },
				game_L, rom_L, linkSettings.L, linkSyncSettings.L);

			R = new GBHawk.GBHawk(new CoreComm(comm.ShowMessage, comm.Notify) { CoreFileProvider = comm.CoreFileProvider },
				game_R, rom_R, linkSettings.R, linkSyncSettings.R);

			ser.Register<IVideoProvider>(this);
			ser.Register<ISoundProvider>(L.audio);

			_tracer = new TraceBuffer { Header = L.cpu.TraceHeader };
			ser.Register<ITraceable>(_tracer);

			ServiceProvider = ser;

			SetupMemoryDomains();

			HardReset();

			L.color_palette[0] = color_palette_BW[0];
			L.color_palette[1] = color_palette_BW[1];
			L.color_palette[2] = color_palette_BW[2];
			L.color_palette[3] = color_palette_BW[3];

			R.color_palette[0] = color_palette_BW[0];
			R.color_palette[1] = color_palette_BW[1];
			R.color_palette[2] = color_palette_BW[2];
			R.color_palette[3] = color_palette_BW[3];
		}

		public void HardReset()
		{
			L.HardReset();
			R.HardReset();
		}

		public DisplayType Region => DisplayType.NTSC;

		public int _frame = 0;

		private readonly GBHawkLinkControllerDeck _controllerDeck;

		private readonly ITraceable _tracer;

		bool ILinkable.LinkConnected { get; }

		private void ExecFetch(ushort addr)
		{
			MemoryCallbacks.CallExecutes(addr, "System Bus");
		}
	}
}
