using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	/// <summary>for querying host input</summary>
	/// <seealso cref="IJoypadApi"/>
	public interface IInputApi : IExternalApi
	{
		/// <returns>
		/// Map of key/button names (of host) to their pressed state.<br/>
		/// Only pressed buttons will appear (with a value of <see langword="true"/>), unpressed buttons are omitted.
		/// </returns>
		/// <remarks>
		/// Includes gamepad axes (<c>!axis.isNeutral</c>, with sticks as 4 "buttons" suffixed <c>"Up"</c>/<c>"Down"</c>/<c>"Left"</c>/<c>"Right"</c>).<br/>
		/// Includes mouse buttons, but not axes (cursor position and wheel rotation).
		/// Unlike <see cref="GetMouse"/>, these have the names <c>"WMouse L"</c>, <c>"WMouse R"</c>, <c>"WMouse M"</c>, <c>"WMouse 1"</c>, and <c>"WMouse 2"</c> for LMB, RMB, MMB, Mouse4, and Mouse5, respectively.<br/>
		/// See <see cref="DistinctKey"/> for keyboard key names, though some are overridden by <see cref="DistinctKeyNameOverrides"/> (check the source).
		/// </remarks>
		/// <seealso cref="GetPressedButtons"/>
		[Obsolete($"consider using {nameof(GetPressedButtons)}/{nameof(GetPressedAxes)}/{nameof(GetMouse)}")]
		Dictionary<string, bool> Get();

		/// <returns>
		/// Map of (host) mouse button/axis names to their state:
		/// either <see cref="bool"/> for a button (<c>button.isPressed</c>), or <see cref="int"/> for an axis.
		/// </returns>
		/// <remarks>
		/// Buttons are <c>"Left"</c>, <c>"Right"</c>, <c>"Middle"</c>, <c>"XButton1"</c>, and <c>"XButton2"</c> for LMB, RMB, MMB, Mouse4, and Mouse5, respectively.
		/// Mouse position is axes <c>"X"</c> and <c>"Y"</c>. Mouse wheel rotation is the <c>"Wheel"</c> axis.
		/// </remarks>
		IReadOnlyDictionary<string, object> GetMouse();

		/// <returns>
		/// Map of (host) axis names to their state.<br/>
		/// Axes may not appear if they have never been seen with a value other than <c>0</c>
		/// (for example, if the gamepad has been set down on a table since launch, or if it was recently reconnected).
		/// </returns>
		/// <remarks>
		/// Includes mouse cursor position axes, but not mouse wheel rotation.
		/// Unlike <see cref="GetMouse"/>, these have the names <c>"WMouse X"</c> and <c>"WMouse Y"</c>.
		/// </remarks>
		IReadOnlyDictionary<string, int> GetPressedAxes();

		/// <returns>
		/// List of (host) key/button names which are pressed
		/// (i.e. were pressed when EmuHawk last polled; this is distinct from virtual gamepad polling/latching).
		/// Unpressed buttons are omitted.
		/// </returns>
		/// <remarks>
		/// Includes gamepad axes (<c>!axis.isNeutral</c>, with sticks as 4 "buttons" suffixed <c>"Up"</c>/<c>"Down"</c>/<c>"Left"</c>/<c>"Right"</c>).<br/>
		/// Includes mouse buttons, but not axes (cursor position and wheel rotation).
		/// Unlike <see cref="GetMouse"/>, these have the names <c>"WMouse L"</c>, <c>"WMouse R"</c>, <c>"WMouse M"</c>, <c>"WMouse 1"</c>, and <c>"WMouse 2"</c> for LMB, RMB, MMB, Mouse4, and Mouse5, respectively.<br/>
		/// See <see cref="DistinctKey"/> for keyboard key names, though some are overridden by <see cref="DistinctKeyNameOverrides"/> (check the source).
		/// </remarks>
		IReadOnlyList<string> GetPressedButtons();
	}
}
