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

		const int NONE = -1;
		const int P1 = 0;
		const int P2 = 1;
		const int P3 = 2;
		const int P4 = 3;

		private readonly int[] _connectedTo;
		private readonly bool[] _clockTrigger;
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
			_connectedTo = new int[4];
			_clockTrigger = new bool[_numCores];

			for (int i = 0; i < _numCores; i++)
			{
				_linkedCores[i] = new MGBAHawk(lp.Roms[i].RomData, lp.Comm, syncSettings._linkedSyncSettings[i], settings._linkedSettings[i], lp.DeterministicEmulationRequested, lp.Roms[i].Game);
				_linkedConts[i] = new SaveController(MGBAHawk.GBAController);
				_frameOverflow[i] = 0;
				_stepOverflow[i] = 0;
				_clockTrigger[i] = false;
				MGBAHawk.LibmGBA.BizConnectLinkCable(_linkedCores[i].Core, _linkedCallbacks[i]);
			}

			_connectedTo[P1] = P2;
			_connectedTo[P2] = P1;

			if (_numCores == 4)
			{
				_connectedTo[P3] = P4;
				_connectedTo[P4] = P3;
			}
			else
			{
				_connectedTo[P3] = NONE;
				_connectedTo[P4] = NONE;
			}

			_disassembler = new ArmV4Disassembler();
			_serviceProvider.Register<IDisassemblable>(_disassembler);

			_videobuff = new int[240 * _numCores * 160];

			GBALinkController = CreateControllerDefinition();
			SetMemoryDomains();
		}

		private void P1LinkCallback() => _clockTrigger[P1] = _connectedTo[P1] != NONE;

		private void P2LinkCallback() => _clockTrigger[P2] = _connectedTo[P2] != NONE;

		private void P3LinkCallback() => _clockTrigger[P3] = _connectedTo[P3] != NONE;

		private void P4LinkCallback() => _clockTrigger[P4] = _connectedTo[P4] != NONE;
	}
}
