#nullable enable

using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Used to enable recording a subset of a controller's buttons, while keeping the existing inputs for the other buttons.
	/// Also used to allow TAStudio to clear (or otherwise edit) a subset of input columns.
	/// </summary>
	internal class MultitrackAdapter : IController
	{
		/// <summary>
		/// Input states in this definition will come from <see cref="ActiveSource"/>. All others will come from <see cref="BackingSource"/>.
		/// </summary>
		public ControllerDefinition ActiveDefinition { get; set; }

		public IController ActiveSource { get; set; }

		public IController BackingSource { get; set; }

		public ControllerDefinition Definition => BackingSource.Definition;

		public MultitrackAdapter(IController active, IController backing, ControllerDefinition activeDefinition)
		{
			ActiveSource = active;
			BackingSource = backing;
			ActiveDefinition = activeDefinition;
		}

		public bool IsPressed(string button)
		{
			if (ActiveDefinition.BoolButtons.Contains(button))
			{
				return ActiveSource.IsPressed(button);
			}
			else
			{
				return BackingSource.IsPressed(button);
			}
		}
		public int AxisValue(string name)
		{
			if (ActiveDefinition.Axes.ContainsKey(name))
			{
				return ActiveSource.AxisValue(name);
			}
			else
			{
				return BackingSource.AxisValue(name);
			}
		}

		public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() => throw new NotImplementedException(); // don't use this
		public void SetHapticChannelStrength(string name, int strength) => throw new NotImplementedException(); // don't use this
	}
}
