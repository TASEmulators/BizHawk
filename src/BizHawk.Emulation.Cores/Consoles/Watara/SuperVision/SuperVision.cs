using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.vr6502;

namespace BizHawk.Emulation.Cores.Consoles.SuperVision
{
	[Core(CoreNames.SuperVisionHawk, "Asnivor", isReleased: false)]
	public partial class SuperVision
	{
		[CoreConstructor(VSystemID.Raw.SuperVision)]
		public SuperVision(CoreLoadParameters<object, SuperVisionSyncSettings> lp)
		{
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;
			CoreComm = lp.Comm;
			var gameInfo = lp.Roms[0].Game;
			var rom = lp.Roms[0].RomData;

			_syncSettings = lp.SyncSettings ?? new SuperVisionSyncSettings();
			_screenType = _syncSettings.ScreenType;

			//MemoryCallbacks = new MemoryCallbackSystem([ "System Bus" ]);

			ControllerDefinition = _superVisionControllerDefinition.Value;

			_cartridge = SVCart.Configure(gameInfo, rom);

			_cpu = new vr6502(vr6502.VrEmu6502Model.CPU_65C02, ReadMemory, WriteMemory);
			//_cpu = new MOS6502X<CpuLink>(new CpuLink(this));
			//_tracer = new TraceBuffer(_cpu.TraceHeader);
			_asic = new ASIC(this, _syncSettings);

			CalcClock();

			ser.Register<IVideoProvider>(_asic.Screen);
			ser.Register<ITraceable>(_tracer);
			//ser.Register<IDisassemblable>(_cpu);
			ser.Register<IStatable>(new StateSerializer(SyncState));
			SetupMemoryDomains();
		}

		private CoreComm CoreComm { get; }
		private IController _controller;
		private readonly ScreenType _screenType;
		//public readonly MOS6502X<CpuLink> _cpu;
		public readonly vr6502 _cpu;
		private readonly TraceBuffer _tracer;
		private ASIC _asic;
		private readonly SVCart _cartridge;
	}
}
