using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.FairchildF8;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	[Core(CoreNames.ChannelFHawk, "Asnivor", isReleased: true)]
	public partial class ChannelF : IDriveLight
	{
		[CoreConstructor(VSystemID.Raw.ChannelF)]
		public ChannelF(CoreLoadParameters<object, ChannelFSyncSettings> lp)
		{
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;
			CoreComm = lp.Comm;
			var gameInfo = lp.Roms[0].Game;
			var rom = lp.Roms[0].RomData;

			_syncSettings = lp.SyncSettings ?? new ChannelFSyncSettings();
			_region = _syncSettings.Region;
			_version = _syncSettings.Version;

			MemoryCallbacks = new MemoryCallbackSystem([ "System Bus" ]);

			ControllerDefinition = _channelFControllerDefinition.Value;

			if (_version == ConsoleVersion.ChannelF)
			{
				_bios01 = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("ChannelF", "ChannelF_sl131253"));
				_bios02 = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("ChannelF", "ChannelF_sl131254"));
			}
			else
			{
				_bios01 = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("ChannelF", "ChannelF_sl90025"));
				_bios02 = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("ChannelF", "ChannelF_sl131254"));
			}

			if (_bios01.Length != 1024 || _bios02.Length != 1024)
			{
				throw new InvalidOperationException("BIOS must be exactly 1024 bytes!");
			}

			_cartridge = VesCartBase.Configure(gameInfo, rom);

			_cpu = new F3850<CpuLink>(new CpuLink(this));
			_tracer = new TraceBuffer(_cpu.TraceHeader);

			CalcClock();
			SetupVideo();

			ser.Register<ITraceable>(_tracer);
			ser.Register<IDisassemblable>(_cpu);
			ser.Register<IStatable>(new StateSerializer(SyncState));
			SetupMemoryDomains();
		}

		private CoreComm CoreComm { get; }

		private readonly F3850<CpuLink> _cpu;
		private readonly TraceBuffer _tracer;
		private IController _controller;

		private readonly VesCartBase _cartridge;
		private readonly RegionType _region;
		private readonly ConsoleVersion _version;

		public bool DriveLightEnabled => _cartridge.HasActivityLED;

		public bool DriveLightOn => _cartridge.ActivityLED;

		public string DriveLightIconDescription => "Computer thinking activity";
	}
}
