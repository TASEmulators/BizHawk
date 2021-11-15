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
			NONE = -1,
			P1 = 0,
			P2 = 1,
			P3 = 2,
			P4 = 3,
		}

		private readonly ConnectionStatus[] _connectionStatus;
		private readonly int[] _linkData;
		private readonly int[] _stepTransferCount;
		private readonly int[] _frameOverflow;
		private readonly int[] _stepOverflow;

		const int CyclesPerFrame = 280896;
		const int StepLength = 64;

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
			_frameOverflow = new int[_numCores];
			_stepOverflow = new int[_numCores];
			_connectionStatus = new ConnectionStatus[4];
			_stepTransferCount = new int[_numCores];
			_linkData = new int[_numCores];

			for (int i = 0; i < _numCores; i++)
			{
				_linkedCores[i] = new MGBAHawk(lp.Roms[i].RomData, lp.Comm, syncSettings._linkedSyncSettings[i], settings._linkedSettings[i], lp.DeterministicEmulationRequested, lp.Roms[i].Game);
				_linkedConts[i] = new SaveController(MGBAHawk.GBAController);
				_frameOverflow[i] = 0;
				_stepOverflow[i] = 0;
				_stepTransferCount[i] = 0;
				MGBAHawk.LibmGBA.BizConnectLinkCable(_linkedCores[i].Core, _linkedCallbacks[i]);
			}

			_connectionStatus[(int)ConnectionStatus.P1] = ConnectionStatus.P2;
			_connectionStatus[(int)ConnectionStatus.P2] = ConnectionStatus.P1;

			if (_numCores == 4)
			{
				_connectionStatus[(int)ConnectionStatus.P3] = ConnectionStatus.P4;
				_connectionStatus[(int)ConnectionStatus.P4] = ConnectionStatus.P3;
			}
			else
			{
				_connectionStatus[(int)ConnectionStatus.P3] = ConnectionStatus.NONE;
				_connectionStatus[(int)ConnectionStatus.P4] = ConnectionStatus.NONE;
			}

			_disassembler = new ArmV4Disassembler();
			_serviceProvider.Register<IDisassemblable>(_disassembler);

			_videobuff = new int[240 * _numCores * 160];

			GBALinkController = CreateControllerDefinition();
			SetMemoryDomains();
		}

		private ushort P1LinkCallback(IntPtr driver, uint address, ushort value)
		{
			if (_connectionStatus[(int)ConnectionStatus.P1] != ConnectionStatus.NONE)
			{
				value = MGBAHawk.LibmGBA.BizWriteLinkRegister(driver, address, value, ref _stepTransferCount[(int)ConnectionStatus.P1], ConnectionStatus.P1 < _connectionStatus[(int)ConnectionStatus.P1]);
				if (_stepTransferCount[(int)ConnectionStatus.P1] != 0)
				{
					MGBAHawk.LibmGBA.BizStartLinkTransfer(driver, _linkData, (int)ConnectionStatus.P1, _linkedCores[(int)_connectionStatus[(int)ConnectionStatus.P1]].Core, (int)_connectionStatus[(int)ConnectionStatus.P1]);
				}
			}

			return value;
		}

		private ushort P2LinkCallback(IntPtr driver, uint address, ushort value)
		{
			if (_connectionStatus[(int)ConnectionStatus.P2] != ConnectionStatus.NONE)
			{
				value = MGBAHawk.LibmGBA.BizWriteLinkRegister(driver, address, value, ref _stepTransferCount[(int)ConnectionStatus.P2], ConnectionStatus.P2 < _connectionStatus[(int)ConnectionStatus.P2]);
				if (_stepTransferCount[(int)ConnectionStatus.P2] != 0)
				{
					MGBAHawk.LibmGBA.BizStartLinkTransfer(driver, _linkData, (int)ConnectionStatus.P2, _linkedCores[(int)_connectionStatus[(int)ConnectionStatus.P2]].Core, (int)_connectionStatus[(int)ConnectionStatus.P2]);
				}
			}

			return value;
		}

		private ushort P3LinkCallback(IntPtr driver, uint address, ushort value)
		{
			if (_connectionStatus[(int)ConnectionStatus.P3] != ConnectionStatus.NONE)
			{
				value = MGBAHawk.LibmGBA.BizWriteLinkRegister(driver, address, value, ref _stepTransferCount[(int)ConnectionStatus.P3], ConnectionStatus.P3 < _connectionStatus[(int)ConnectionStatus.P3]);
				if (_stepTransferCount[(int)ConnectionStatus.P3] != 0)
				{
					MGBAHawk.LibmGBA.BizStartLinkTransfer(driver, _linkData, (int)ConnectionStatus.P3, _linkedCores[(int)_connectionStatus[(int)ConnectionStatus.P3]].Core, (int)_connectionStatus[(int)ConnectionStatus.P3]);
				}
			}

			return value;
		}

		private ushort P4LinkCallback(IntPtr driver, uint address, ushort value)
		{
			if (_connectionStatus[(int)ConnectionStatus.P4] != ConnectionStatus.NONE)
			{
				value = MGBAHawk.LibmGBA.BizWriteLinkRegister(driver, address, value, ref _stepTransferCount[(int)ConnectionStatus.P4], ConnectionStatus.P4 < _connectionStatus[(int)ConnectionStatus.P4]);
				if (_stepTransferCount[(int)ConnectionStatus.P4] != 0)
				{
					MGBAHawk.LibmGBA.BizStartLinkTransfer(driver, _linkData, (int)ConnectionStatus.P4, _linkedCores[(int)_connectionStatus[(int)ConnectionStatus.P4]].Core, (int)_connectionStatus[(int)ConnectionStatus.P4]);
				}
			}

			return value;
		}
	}
}
