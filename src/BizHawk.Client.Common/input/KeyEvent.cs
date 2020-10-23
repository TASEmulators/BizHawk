#nullable enable

using OpenTK.Input;

namespace BizHawk.Client.Common
{
	public readonly struct KeyEvent
	{
		public readonly Key Key;

		public readonly bool Pressed;

		public KeyEvent(Key key, bool pressed)
		{
			Key = key;
			Pressed = pressed;
		}
	}
}
