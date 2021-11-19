using System;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	[PortedCore(CoreNames.DualGambatte, "sinamas/natt")]
	[ServiceNotApplicable(new[] { typeof(IDriveLight) })]
	public partial class GambatteLink : ILinkable, IRomInfo
	{
		[CoreConstructor(VSystemID.Raw.DGB)]
		public GambatteLink(CoreLoadParameters<GambatteLinkSettings, GambatteLinkSyncSettings> lp)
		{
			if (lp.Roms.Count < MIN_PLAYERS || lp.Roms.Count > MAX_PLAYERS)
				throw new InvalidOperationException("Wrong number of roms");

			_numCores = lp.Roms.Count;

			_serviceProvider = new BasicServiceProvider(this);
			_settings = lp.Settings ?? new GambatteLinkSettings();
			_syncSettings = lp.SyncSettings ?? new GambatteLinkSyncSettings();

			_linkedCores = new Gameboy[_numCores];
			_linkedConts = new SaveController[_numCores];
			_linkedBlips = new BlipBuffer[_numCores];
			_linkedLatches = new int[_numCores];
			_linkedOverflow = new int[_numCores];

			RomDetails = "";
			_memoryCallbacks = new MemoryCallbackSystem(new[] { "System Bus" });

			for (int i = 0; i < _numCores; i++)
			{
				_linkedCores[i] = new Gameboy(lp.Comm, lp.Roms[i].Game, lp.Roms[i].RomData, _settings._linkedSettings[i], _syncSettings._linkedSyncSettings[i], lp.DeterministicEmulationRequested);
				LibGambatte.gambatte_linkstatus(_linkedCores[i].GambatteState, 259); // connect link cable
				_linkedCores[i].ConnectInputCallbackSystem(_inputCallbacks);
				_linkedCores[i].ConnectMemoryCallbackSystem(_memoryCallbacks);
				_linkedConts[i] = new SaveController(Gameboy.CreateControllerDefinition(false, false));
				_linkedBlips[i] = new BlipBuffer(150000);
				_linkedBlips[i].SetRates(2097152 * 2, 44100);
				_linkedOverflow[i] = 0;
				_linkedLatches[i] = 0;
				RomDetails += $"P{i + 1}:\r\n" + _linkedCores[i].RomDetails;
			}

			LinkConnected = true;

			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;

			SoundBuffer = new short[MaxSampsPerFrame * _numCores];

			FrameBuffer = CreateVideoBuffer();
			VideoBuffer = CreateVideoBuffer();

			GBLinkController = CreateControllerDefinition();

			_linkedSaveRam = new LinkedSaveRam(_linkedCores, _numCores);
			_serviceProvider.Register<ISaveRam>(_linkedSaveRam);

			_linkedMemoryDomains = new LinkedMemoryDomains(_linkedCores, _numCores);
			_serviceProvider.Register<IMemoryDomains>(_linkedMemoryDomains);

			_linkedDebuggable = new LinkedDebuggable(_linkedCores, _numCores, _memoryCallbacks);
			_serviceProvider.Register<IDebuggable>(_linkedDebuggable);
		}

		private readonly BasicServiceProvider _serviceProvider;

		private readonly MemoryCallbackSystem _memoryCallbacks;

		public string RomDetails { get; }

		public bool LinkConnected
		{
			get => _cableconnected;
			set => _cableconnected = value;
		}

		private int _numCores = 0;
		private readonly Gameboy[] _linkedCores;

		private readonly LinkedSaveRam _linkedSaveRam;
		private readonly LinkedMemoryDomains _linkedMemoryDomains;
		private readonly LinkedDebuggable _linkedDebuggable;

		// counters to ensure we do 35112 samples per frame
		private readonly int[] _linkedOverflow;

		// if true, the link cable is currently connected
		private bool _cableconnected = true;

		// if true, the link cable toggle signal is currently asserted
		private bool _cablediscosignal = false;

		private const int SampPerFrame = 35112;
		private const int MaxSampsPerFrame = (SampPerFrame + 2064) * 2;

		private readonly SaveController[] _linkedConts;

		public bool IsCGBMode(int which)
		{
			return which < _numCores && _linkedCores[which].IsCGBMode();
		}

		private ControllerDefinition GBLinkController { get; }

		private ControllerDefinition CreateControllerDefinition()
		{
			var ret = new ControllerDefinition { Name = $"GB Link {_numCores}x Controller" };
			for (int i = 0; i < _numCores; i++)
			{
				ret.BoolButtons.AddRange(
					new[] { "Up", "Down", "Left", "Right", "A", "B", "Select", "Start", "Power" }
						.Select(s => $"P{i + 1} {s}"));
			}
			ret.BoolButtons.Add("Toggle Cable");
			return ret;
		}

		private const int P1 = 0;
		private const int P2 = 1;
		private const int P3 = 2;
		private const int P4 = 3;

		private const int MIN_PLAYERS = 2;
		private const int MAX_PLAYERS = 4;
	}
}
