#nullable enable

namespace BizHawk.Bizware.Input
{
	public readonly record struct KeyEvent(DistinctKey Key, bool Pressed);
}
