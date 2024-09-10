using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class AndAdapter : IInputAdapter
	{
		public ControllerDefinition Definition => Source.Definition;

		public bool IsPressed(string button)
		{
			if (Source != null && SourceAnd != null)
			{
				return Source.IsPressed(button) & SourceAnd.IsPressed(button);
			}

			return false;
		}

		// pass axes solely from the original source
		// this works in the code because SourceOr is the autofire controller
		public int AxisValue(string name) => Source.AxisValue(name);

		public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() => Source.GetHapticsSnapshot();

		public void SetHapticChannelStrength(string name, int strength) => Source.SetHapticChannelStrength(name, strength);

		public IController Source { get; set; }
		internal IController SourceAnd { get; set; }
	}

	public class XorAdapter : IInputAdapter
	{
		public ControllerDefinition Definition => Source.Definition;

		public bool IsPressed(string button)
		{
			if (Source != null && SourceXor != null)
			{
				return Source.IsPressed(button) ^ SourceXor.IsPressed(button);
			}

			return false;
		}

		// pass axes solely from the original source
		// this works in the code because SourceOr is the autofire controller
		public int AxisValue(string name) => Source.AxisValue(name);

		public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() => Source.GetHapticsSnapshot();

		public void SetHapticChannelStrength(string name, int strength) => Source.SetHapticChannelStrength(name, strength);

		public IController Source { get; set; }
		internal IController SourceXor { get; set; }
	}

	public class ORAdapter : IInputAdapter
	{
		public ControllerDefinition Definition => Source.Definition;

		public bool IsPressed(string button)
		{
			return (Source?.IsPressed(button) ?? false)
					| (SourceOr?.IsPressed(button) ?? false);
		}

		// pass axes solely from the original source
		// this works in the code because SourceOr is the autofire controller
		public int AxisValue(string name) => Source.AxisValue(name);

		public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() => Source.GetHapticsSnapshot();

		public void SetHapticChannelStrength(string name, int strength) => Source.SetHapticChannelStrength(name, strength);

		public IController Source { get; set; }
		internal IController SourceOr { get; set; }
	}
}
