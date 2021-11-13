using System;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBALink : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider => _serviceProvider;

		public ControllerDefinition ControllerDefinition => GBALinkController;

		private static ControllerDefinition GBALinkController { get; set; }

		private ControllerDefinition CreateControllerDefinition()
		{
			var ret = new ControllerDefinition { Name = $"GBA Link {_numCores}x Controller" };
			for (int i = 1; i <= _numCores; i++)
			{
				ret.BoolButtons.AddRange(
					new[] { "Up", "Down", "Left", "Right", "Start", "Select", "B", "A", "L", "R", "Power" }
						.Select(s => $"P{i} {s}"));
				ret.AddXYZTriple($"P{i} " + "Tilt {0}", (-32767).RangeTo(32767), 0);
				ret.AddAxis($"P{i} " + "Light Sensor", 0.RangeTo(255), 0);
			}
			return ret;
		}

		public bool FrameAdvance(IController controller, bool render, bool rendersound = true)
		{
			for (int i = 0; i < _numCores; i++)
			{
				_linkedConts[i].Clear();
			}

			foreach (var s in GBALinkController.BoolButtons)
			{
				if (controller.IsPressed(s))
				{
					for (int i = 0; i < _numCores; i++)
					{
						if (s.Contains($"P{i + 1} "))
						{
							_linkedConts[i].Set(s.Replace($"P{i + 1} ", ""));
						}
					}
				}
			}

			foreach (var s in GBALinkController.Axes)
			{
				for (int i = 0; i < _numCores; i++)
				{
					if (s.Key.Contains($"P{i + 1} "))
					{
						_linkedConts[i].SetAxisValue(s.Key.Replace($"P{i + 1} ", ""), controller.AxisValue(s.Key));
					}
				}
			}

			// todo: actually step
			// todo: link!
			for (int i = 0; i < _numCores; i++)
			{
				_linkedCores[i].FrameAdvance(_linkedConts[i], render, rendersound);
			}

			IsLagFrame = false;
			for (int i = 0; i < _numCores; i++)
			{
				if (_linkedCores[i].IsLagFrame)
					IsLagFrame = true;
			}

			if (IsLagFrame)
			{
				LagCount++;
			}

			Frame++;

			if (rendersound)
			{
				_nsamp = 0;
			}
			else
			{
				_nsamp = 0;
			}

			return true;
		}

		public int Frame { get; private set; }

		public string SystemId => "GBALink";

		public bool DeterministicEmulation => LinkedDeterministicEmulation();

		private bool LinkedDeterministicEmulation()
		{
			for (int i = 0; i < _numCores; i++)
			{
				if (_linkedCores[i].DeterministicEmulation)
					return true;
			}
			return false;
		}

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		public void Dispose()
		{
			if (_numCores > 0)
			{
				for (int i = 0; i < _numCores; i++)
				{
					_linkedCores[i].Dispose();
					_linkedCores[i] = null;
				}

				_numCores = 0;
			}
		}
	}
}