#nullable enable

namespace BizHawk.Client.Common
{
	public readonly struct KeyEvent
	{
		public readonly DistinctKey Key;

		public readonly bool Pressed;

		public KeyEvent(DistinctKey key, bool pressed)
		{
			Key = key;
			Pressed = pressed;
		}
	}
}
