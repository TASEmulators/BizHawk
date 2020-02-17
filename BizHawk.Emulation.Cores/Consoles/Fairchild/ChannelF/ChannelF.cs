using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	[Core(
		"ChannelFHawk",
		"Asnivor",
		isPorted: false,
		isReleased: false)]
	[ServiceNotApplicable(typeof(IDriveLight))]
	public partial class ChannelF
	{
		public ChannelF(CoreComm comm, GameInfo game, byte[] rom, object settings, object syncSettings)
		{
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;
			CoreComm = comm;
			MemoryCallbacks = new MemoryCallbackSystem(new[] { "System Bus" });

			ControllerDefinition = ChannelFControllerDefinition;

			CPU = new F3850
			{
				ReadMemory = ReadBus,
				WriteMemory = WriteBus,
				ReadHardware = ReadPort,
				WriteHardware = WritePort,
				DummyReadMemory = ReadBus
			};

			_tracer = new TraceBuffer { Header = CPU.TraceHeader };

			byte[] bios01 = comm.CoreFileProvider.GetFirmware("ChannelF", "ChannelF_sl131253", true);
			byte[] bios02 = comm.CoreFileProvider.GetFirmware("ChannelF", "ChannelF_sl131254", true);

			BIOS01 = bios01;
			BIOS02 = bios02;

			Array.Copy(rom, 0, Rom, 0, rom.Length);

			CalcClock();

			ser.Register<IVideoProvider>(this);
			ser.Register<ITraceable>(_tracer);
			ser.Register<IDisassemblable>(CPU);
			ser.Register<ISoundProvider>(this);
			ser.Register<IStatable>(new StateSerializer(SyncState));
			SetupMemoryDomains();
		}

		public F3850 CPU;
		private readonly TraceBuffer _tracer;
		public IController _controller;
	}
}
