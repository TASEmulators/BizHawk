using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarioAI.Runner
{
	public class DebugController : IController
	{
		public ControllerDefinition Definition => new ControllerDefinition
		{
			Name = "Debug Controller"
		};

		public bool IsPressed(string button)
		{
			Console.WriteLine("Asking for button {0}", button);

			return false;
		}

		public int AxisValue(string name)
		{
			return 0;
		}

		public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot()
		{
			return Array.Empty<(string, int)>();
		}

		public void SetHapticChannelStrength(string name, int strength) { 

		}

		public static readonly DebugController Instance = new DebugController();
	}
}
