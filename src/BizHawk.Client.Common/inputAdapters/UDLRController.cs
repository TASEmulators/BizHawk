using System.Collections.Generic;

using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Filters input for things called Up and Down while considering the client's AllowUD_LR option.
	/// This is a bit gross but it is unclear how to do it more nicely
	/// </summary>
	public class UdlrControllerAdapter : IInputAdapter
	{
		private readonly HashSet<string> _unpresses = new();

		public IController Source { get; set; }

		public ControllerDefinition Definition => Source.Definition;

		public OpposingDirPolicy OpposingDirPolicy { get; set; }

		public bool IsPressed(string button)
		{
			if (OpposingDirPolicy is OpposingDirPolicy.Allow) return Source.IsPressed(button);

			// " C " is for N64 "P1 C Up" and the like, which should not be subject to mutexing
			// regarding the unpressing and UDLR logic...... don't think about it. don't question it. don't look at it.
			if (button.Contains(" C ")) return Source.IsPressed(button);

			bool HandleOpposingDir(string opposingButtonName)
			{
				if (Source.IsPressed(opposingButtonName))
				{
					if (OpposingDirPolicy is OpposingDirPolicy.Forbid || _unpresses.Contains(button)) return false;
					_unpresses.Add(opposingButtonName);
				}
				else
				{
					_unpresses.Remove(button);
				}
				return Source.IsPressed(button);
			}
			if (button.Contains("Down")) return HandleOpposingDir($"{button.SubstringBeforeOrNull("Down")}Up");
			if (button.Contains("Up")) return HandleOpposingDir($"{button.SubstringBeforeOrNull("Up")}Down");
			if (button.Contains("Right")) return HandleOpposingDir($"{button.SubstringBeforeOrNull("Right")}Left");
			if (button.Contains("Left")) return HandleOpposingDir($"{button.SubstringBeforeOrNull("Left")}Right");
			return Source.IsPressed(button);
		}

		// The float format implies no U+D and no L+R no matter what, so just passthru
		public int AxisValue(string name) => Source.AxisValue(name);

		public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() => Source.GetHapticsSnapshot();

		public void SetHapticChannelStrength(string name, int strength) => Source.SetHapticChannelStrength(name, strength);
	}
}
