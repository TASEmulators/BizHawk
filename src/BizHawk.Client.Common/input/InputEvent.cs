#nullable enable

using System.Collections.Generic;
using System.Text;

namespace BizHawk.Client.Common
{
	[Flags]
	public enum ClientInputFocus
	{
		None = 0,
		Mouse = 1,
		Keyboard = 2,
		Pad = 4
	}

	public class InputEvent
	{
		public InputEventType EventType;

		public LogicalButton LogicalButton;

		public ClientInputFocus Source;

		public override string ToString() => $"{EventType}:{LogicalButton}";
	}

	public enum InputEventType
	{
		Press, Release
	}

	public struct LogicalButton
	{
		public const uint MASK_ALT = 1U << 2;

		public const uint MASK_CTRL = 1U << 1;

		public const uint MASK_SHIFT = 1U << 3;

		public const uint MASK_WIN = 1U << 0;

		public static bool operator ==(LogicalButton lhs, LogicalButton rhs)
			=> lhs.Button == rhs.Button && lhs.Modifiers == rhs.Modifiers;

		public static bool operator !=(LogicalButton lhs, LogicalButton rhs) => !(lhs == rhs);

		/// <remarks>pretty sure these are always consumed during the same iteration of the main program loop, but ¯\_(ツ)_/¯ better safe than sorry --yoshi</remarks>
		private readonly Func<IReadOnlyList<string>> _getEffectiveModListCallback;

		public readonly string Button;

		public readonly uint Modifiers;

		public LogicalButton(string button, uint modifiers, Func<IReadOnlyList<string>> getEffectiveModListCallback)
		{
			_getEffectiveModListCallback = getEffectiveModListCallback;
			Button = button;
			Modifiers = modifiers;
		}

		public override readonly bool Equals(object? obj) => obj is not null && (LogicalButton) obj == this; //TODO safe type check?

		public override readonly int GetHashCode()
			=> HashCode.Combine(Button, Modifiers);

		public override readonly string ToString()
		{
			if (Modifiers is 0U) return Button;
			var allMods = _getEffectiveModListCallback();
			StringBuilder ret = new();
			for (var i = 0; i < allMods.Count; i++)
			{
				var b = 1U << i;
				if ((Modifiers & b) is not 0U)
				{
					ret.Append(allMods[i]);
					ret.Append('+');
				}
			}
			return ret + Button;
		}
	}
}
