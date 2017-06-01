using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Sound;
using BizHawk.Emulation.Cores.Waterbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Consoles.SNK
{
	[CoreAttributes("Dual NeoPop", "Thomas Klausner and natt", true, false, "0.9.44.1",
		"https://mednafen.github.io/releases/", false)]
	public class DualNeoGeoPort : IEmulator
	{
		private NeoGeoPort _left;
		private NeoGeoPort _right;
		private readonly BasicServiceProvider _serviceProvider;
		private bool _disposed = false;
		private readonly DualSyncSound _soundProvider;
		private readonly SideBySideVideo _videoProvider;

		[CoreConstructor("DNGP")]
		public DualNeoGeoPort(CoreComm comm, byte[] rom, bool deterministic)
		{
			CoreComm = comm;
			_left = new NeoGeoPort(comm, rom, null, deterministic, PeRunner.CanonicalStart);
			_right = new NeoGeoPort(comm, rom, null, deterministic, PeRunner.AlternateStart);

			_serviceProvider = new BasicServiceProvider(this);
			_soundProvider = new DualSyncSound(_left, _right);
			_serviceProvider.Register<ISoundProvider>(_soundProvider);
			_videoProvider = new SideBySideVideo(_left, _right);
			_serviceProvider.Register<IVideoProvider>(_videoProvider);
		}

		public void FrameAdvance(IController controller, bool render, bool rendersound = true)
		{
			_left.FrameAdvance(new PrefixController(controller, "P1 "), render, rendersound);
			_right.FrameAdvance(new PrefixController(controller, "P2 "), render, rendersound);
			Frame++;
			_soundProvider.Fetch();
			_videoProvider.Fetch();
		}

		private class PrefixController : IController
		{
			public PrefixController(IController controller, string prefix)
			{
				_controller = controller;
				_prefix = prefix;
			}

			private readonly IController _controller;
			private readonly string _prefix;

			public ControllerDefinition Definition => null;

			public float GetFloat(string name)
			{
				return _controller.GetFloat(_prefix + name);
			}

			public bool IsPressed(string button)
			{
				return _controller.IsPressed(_prefix + button);
			}
		}

		public ControllerDefinition ControllerDefinition => DualNeoGeoPortController;

		private static readonly ControllerDefinition DualNeoGeoPortController = new ControllerDefinition
		{
			BoolButtons =
			{
				"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 A", "P1 B", "P1 Option", "P1 Power",
				"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 A", "P2 B", "P2 Option", "P2 Power"
			},
			Name = "Dual NeoGeo Portable Controller"
		};

		public void ResetCounters()
		{
			Frame = 0;
		}

		public int Frame { get; private set; }

		public IEmulatorServiceProvider ServiceProvider => _serviceProvider;

		public CoreComm CoreComm { get; }

		public bool DeterministicEmulation => _left.DeterministicEmulation && _right.DeterministicEmulation;

		public string SystemId => "DNGP";

		public void Dispose()
		{
			if (!_disposed)
			{
				_left.Dispose();
				_right.Dispose();
				_left = null;
				_right = null;
				_disposed = true;
			}
		}
	}
}
