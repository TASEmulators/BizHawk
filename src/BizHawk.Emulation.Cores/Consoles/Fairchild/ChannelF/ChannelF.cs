using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	[Core(CoreNames.ChannelFHawk, "Asnivor", isReleased: false)]
	[ServiceNotApplicable(new[] { typeof(IDriveLight) })]
	public partial class ChannelF
	{
		[CoreConstructor("ChannelF")]
		public ChannelF(CoreComm comm, GameInfo game, byte[] rom)
		{
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;
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
