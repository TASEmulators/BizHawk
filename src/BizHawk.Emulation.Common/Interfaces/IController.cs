using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	public interface IController
	{
		/// <summary>
		/// Gets a definition of the controller schema, including all currently available buttons and their types
		/// </summary>
		ControllerDefinition Definition { get; }

		/// <summary>
		/// Returns the current state of a boolean control
		/// </summary>
		bool IsPressed(string button);

		/// <summary>
		/// Returns the state of an axis control
		/// </summary>
		int AxisValue(string name);

		/// <see cref="SetHapticChannelStrength"/>
		IReadOnlyCollection<(string name, int strength)> GetHapticsSnapshot();

		/// <param name="name">a haptic axis name e.g. "P1 Mono Haptic", "P2 Left Haptic"</param>
		/// <param name="strength">0..<see cref="int.MaxValue"/></param>
		void SetHapticChannelStrength(string name, int strength);
	}
}
