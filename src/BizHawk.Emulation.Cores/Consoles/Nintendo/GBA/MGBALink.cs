using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	[PortedCore(CoreNames.MgbaLink, "endrift", "0.8", "https://mgba.io/", isReleased: false)]
	[ServiceNotApplicable(new[] { typeof(IDriveLight), typeof(IRegionable) })]
	public partial class MGBALink
	{
		private readonly MGBAHawk[] _linkedCores;
		private readonly SaveController[] _linkedConts;
		private readonly LibmGBA.LinkCallback[] _linkedCallbacks;
		private int _numCores = 0;

		private readonly BasicServiceProvider _serviceProvider;
		private readonly ArmV4Disassembler _disassembler;

		public enum ConnectionStatus : int
		{
			NO_CONNECTION = -1,
			CONNECTED_TO_P1 = 0,
			CONNECTED_TO_P2 = 1,
			CONNECTED_TO_P3 = 2,
			CONNECTED_TO_P4 = 3,
		}

		private ConnectionStatus _p1ConnectionStatus;
		private ConnectionStatus _p2ConnectionStatus;
		private ConnectionStatus _p3ConnectionStatus;
		private ConnectionStatus _p4ConnectionStatus;

		private int[] _frameOverflow = new int[4] { 0, 0, 0, 0 };
		private int[] _stepOverflow = new int[4] { 0, 0, 0, 0 };

		const int CyclesPerFrame = 280896;
		const int StepLength = 1024;

		[CoreConstructor("GBALink")]
		public MGBALink(CoreLoadParameters<MGBALinkSettings, MGBALinkSyncSettings> lp)
		{
			if (lp.Roms.Count < 2 || lp.Roms.Count > 4)
			{
				throw new InvalidOperationException("Wrong number of ROMs!");
			}

			_numCores = lp.Roms.Count;

			_serviceProvider = new BasicServiceProvider(this);
			MGBALinkSettings settings = lp.Settings ?? new MGBALinkSettings();
			MGBALinkSyncSettings syncSettings = lp.SyncSettings ?? new MGBALinkSyncSettings();

			_linkedCores = new MGBAHawk[_numCores];
			_linkedConts = new SaveController[_numCores];
			_linkedCallbacks = new LibmGBA.LinkCallback[4] { P1LinkCallback, P2LinkCallback, P3LinkCallback, P4LinkCallback };

			for (int i = 0; i < _numCores; i++)
			{
				_linkedCores[i] = new MGBAHawk(lp.Roms[i].RomData, lp.Comm, syncSettings._linkedSyncSettings[i], settings._linkedSettings[i], lp.DeterministicEmulationRequested, lp.Roms[i].Game);
				_linkedConts[i] = new SaveController(MGBAHawk.GBAController);
				MGBAHawk.LibmGBA.BizConnectLinkCable(_linkedCores[i].Core, _linkedCallbacks[i]);
			}

			_p1ConnectionStatus = ConnectionStatus.CONNECTED_TO_P2;
			_p2ConnectionStatus = ConnectionStatus.CONNECTED_TO_P1;

			if (_numCores == 4)
			{
				_p3ConnectionStatus = ConnectionStatus.CONNECTED_TO_P4;
				_p4ConnectionStatus = ConnectionStatus.CONNECTED_TO_P3;
			}
			else
			{
				_p3ConnectionStatus = ConnectionStatus.NO_CONNECTION;
				_p4ConnectionStatus = ConnectionStatus.NO_CONNECTION;
			}

			_disassembler = new ArmV4Disassembler();
			_serviceProvider.Register<IDisassemblable>(_disassembler);

			_videobuff = new int[240 * _numCores * 160];

			GBALinkController = CreateControllerDefinition();
			SetMemoryDomains();
		}

		private ushort P1LinkCallback(IntPtr driver, uint address, ushort value)
		{
			Console.WriteLine("P1 Link CB called");
			return value;
		}

		private ushort P2LinkCallback(IntPtr driver, uint address, ushort value)
		{
			Console.WriteLine("P2 Link CB called");
			return value;
		}

		private ushort P3LinkCallback(IntPtr driver, uint address, ushort value)
		{
			Console.WriteLine("P3 Link CB called");
			return value;
		}

		private ushort P4LinkCallback(IntPtr driver, uint address, ushort value)
		{
			Console.WriteLine("P4 Link CB called");
			return value;
		}
	}
}
