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

		public int AxisValue(string name)
		{
			int neutralValue = Source.Definition.Axes[name].Neutral;
			return SourceAnd.AxisValue(name) != neutralValue ? Source.AxisValue(name) : neutralValue;
		}

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

		// xor logic for axes: xor the logical state of axes (not neutral vs. neutral) and return the result
		public int AxisValue(string name)
		{
			int sourceAxisValue = Source.AxisValue(name);
			int sourceXorAxisValue = SourceXor.AxisValue(name);
#if DEBUG
			var neutral = Definition.Axes[name].Neutral; // throw if no such axis
#else
			var neutral = Definition.Axes.TryGetValue(name, out var axisSpec) ? axisSpec.Neutral : default;
#endif

			if (sourceAxisValue == neutral)
			{
				return sourceXorAxisValue;
			}

			return sourceXorAxisValue == neutral ? sourceAxisValue : neutral;
		}

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

		public int AxisValue(string name)
		{
			int sourceValue = Source.AxisValue(name);
#if DEBUG
			var neutralValue = Source.Definition.Axes[name].Neutral; // throw if no such axis
#else
			var neutralValue = Source.Definition.Axes.TryGetValue(name, out var axisSpec) ? axisSpec.Neutral : default;
#endif
			return sourceValue != neutralValue
				? sourceValue
				: SourceOr.AxisValue(name);
		}

		public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() => Source.GetHapticsSnapshot();

		public void SetHapticChannelStrength(string name, int strength) => Source.SetHapticChannelStrength(name, strength);

		public IController Source { get; set; }
		internal IController SourceOr { get; set; }
	}
}
