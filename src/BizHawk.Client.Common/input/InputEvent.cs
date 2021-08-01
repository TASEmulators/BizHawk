#nullable enable

using System;

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

	public readonly struct LogicalButton
	{
		public static bool operator ==(LogicalButton lhs, LogicalButton rhs)
			=> lhs.Button == rhs.Button && lhs.Modifiers == rhs.Modifiers;

		public static bool operator !=(LogicalButton lhs, LogicalButton rhs) => !(lhs == rhs);

		public bool Alt => (Modifiers & ModifierKey.Alt) != 0;

		public readonly string Button;

		public bool Control => (Modifiers & ModifierKey.Control) != 0;

		public readonly ModifierKey Modifiers;

		public bool Shift => (Modifiers & ModifierKey.Shift) != 0;

		public LogicalButton(string button, ModifierKey modifiers)
		{
			Button = button;
			Modifiers = modifiers;
		}

		public override readonly bool Equals(object? obj) => obj is not null && (LogicalButton) obj == this; //TODO safe type check?

		public override readonly int GetHashCode() => Button.GetHashCode() ^ Modifiers.GetHashCode();

		public override readonly string ToString()
		{
			var ret = "";
			if (Control) ret += "Ctrl+";
			if (Alt) ret += "Alt+";
			if (Shift) ret += "Shift+";
			ret += Button;
			return ret;
		}
	}
}
