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
		private int _numCores = 0;

		private readonly BasicServiceProvider _serviceProvider;
		private readonly ArmV4Disassembler _disassembler;

		private IntPtr _lockstep = IntPtr.Zero;
		private readonly LibmGBA.MasterWaitCallback _masterWaitCallback;
		private readonly LibmGBA.SlaveWaitCallback _slaveWaitCallback;
		private bool _masterIsWaiting = false;

		const int P1 = 0;
		const int P2 = 1;
		const int P3 = 2;
		const int P4 = 3;
		const int MIN_PLAYERS = 2;
		const int MAX_PLAYERS = 4;

		private readonly int[] _frameOverflow;
		private readonly int[] _stepOverflow;

		const int CyclesPerFrame = 280896;
		const int StepLength = 511;

		[CoreConstructor("GBALink")]
		public MGBALink(CoreLoadParameters<MGBALinkSettings, MGBALinkSyncSettings> lp)
		{
			if (lp.Roms.Count < MIN_PLAYERS || lp.Roms.Count > MAX_PLAYERS)
			{
				throw new InvalidOperationException("Wrong number of ROMs!");
			}

			_numCores = lp.Roms.Count;

			_serviceProvider = new BasicServiceProvider(this);
			_settings = lp.Settings ?? new MGBALinkSettings();
			_syncSettings = lp.SyncSettings ?? new MGBALinkSyncSettings();

			_linkedCores = new MGBAHawk[_numCores];
			_linkedConts = new SaveController[_numCores];
			_frameOverflow = new int[_numCores];
			_stepOverflow = new int[_numCores];
			_masterWaitCallback = MasterWaitCallback;
			_slaveWaitCallback = SlaveWaitCallback;
			_lockstep = MGBAHawk.LibmGBA.BizCreateLockstep(_masterWaitCallback, _slaveWaitCallback);

			for (int i = 0; i < _numCores; i++)
			{
				_linkedCores[i] = new MGBAHawk(lp.Roms[i].RomData, lp.Comm, _syncSettings._linkedSyncSettings[i], _settings._linkedSettings[i], lp.DeterministicEmulationRequested, lp.Roms[i].Game);
				_linkedConts[i] = new SaveController(MGBAHawk.GBAController);
				_frameOverflow[i] = 0;
				_stepOverflow[i] = 0;
				MGBAHawk.LibmGBA.BizConnectLinkCable(_linkedCores[i].Core, _lockstep);
			}

			_disassembler = new ArmV4Disassembler("P1 R15");
			_serviceProvider.Register<IDisassemblable>(_disassembler);

			_videobuff = new int[240 * _numCores * 160];

			GBALinkController = CreateControllerDefinition();
			SetMemoryDomains();
		}

		private unsafe bool MasterWaitCallback(IntPtr lockstep, int mask)
		{
			_masterIsWaiting = true;
			for (int i = 1; i < _numCores; i++)
			{
				long stepTarget = _linkedCores[P1].TotalExecutedCycles - _linkedCores[i].TotalExecutedCycles;
				if (stepTarget < 0)
				{
					continue; // we're already caught up
				}
				fixed (int* vbuff = &_videobuff[i * 240])
				{
					_stepOverflow[i] = MGBAHawk.LibmGBA.BizStep(_linkedCores[i].Core, (int)stepTarget, vbuff, _numCores);
					_frameOverflow[i] += (int)stepTarget + _stepOverflow[i];
				}
			}
			_masterIsWaiting = false;
			return true;
		}

		private int SlaveWaitCallback(IntPtr lockstep, int id) => _masterIsWaiting ? int.MaxValue : 0;
	}
}
