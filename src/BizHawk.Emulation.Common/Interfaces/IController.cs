#nullable disable

using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	public interface IController
	{
		/// <summary>
		/// Gets a definition of the controller schema, including all currently available buttons and their types
		/// </summary>
		ControllerDefinition Definition { get; }

		/// <seealso cref="SetHapticChannelStrength"/>
		IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot();

		/// <summary>
		/// Returns the current state of a boolean control
		/// </summary>
		bool IsPressed(string button);

		/// <summary>
		/// Returns the state of an axis control
		/// </summary>
		int AxisValue(string name);

		/// <param name="name">haptic channel name e.g. "P1 Mono", "P2 Left"</param>
		/// <param name="strength">0..<see cref="int.MaxValue"/></param>
		void SetHapticChannelStrength(string name, int strength);
	}
}
