using System;
using System.Collections.Generic;
using System.Linq;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.FairchildF8;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	[Core(CoreNames.ChannelFHawk, "Asnivor", isReleased: false)]
	public partial class ChannelF : IDriveLight
	{
		[CoreConstructor(VSystemID.Raw.ChannelF)]
		public ChannelF(CoreLoadParameters<ChannelFSettings, ChannelFSyncSettings> lp)
		{
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;
			CoreComm = lp.Comm;
			_gameInfo = lp.Roms.Select(r => r.Game).ToList();
			_files = lp.Roms.Select(r => r.RomData).ToList();
			

			var settings = lp.Settings ?? new ChannelFSettings();
			var syncSettings = lp.SyncSettings ?? new ChannelFSyncSettings();

			region = syncSettings.Region;


			MemoryCallbacks = new MemoryCallbackSystem(new[] { "System Bus" });

			ControllerDefinition = ChannelFControllerDefinition;

			var bios01 = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("ChannelF", "ChannelF_sl131253"));
			var bios02 = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("ChannelF", "ChannelF_sl131254"));
			//var bios02 = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("ChannelF", "ChannelF_sl90025"));

			Cartridge = VesCartBase.Configure(_gameInfo.First(), _files.First());

			BIOS01 = bios01;
			BIOS02 = bios02;			

			CPU = new F3850
			{
				ReadMemory = ReadBus,
				WriteMemory = WriteBus,
				ReadHardware = ReadPort,
				WriteHardware = WritePort,
				DummyReadMemory = ReadBus
			};

			_tracer = new TraceBuffer(CPU.TraceHeader);			

			//var rom = _files.First();
			//Array.Copy(rom, 0, Rom, 0, rom.Length);

			CalcClock();

			ser.Register<IVideoProvider>(this);
			ser.Register<ITraceable>(_tracer);
			ser.Register<IDisassemblable>(CPU);
			ser.Register<ISoundProvider>(this);
			ser.Register<IStatable>(new StateSerializer(SyncState));
			SetupMemoryDomains();
		}

		internal CoreComm CoreComm { get; }

		public List<GameInfo> _gameInfo;
		private readonly List<byte[]> _files;

		public F3850 CPU;
		private readonly TraceBuffer _tracer;
		public IController _controller;

		public VesCartBase Cartridge;
		public RegionType region;

		public bool DriveLightEnabled => true;

		public bool DriveLightOn => !Cartridge.ActivityLED;
	}
}
