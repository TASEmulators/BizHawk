using System;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// this will be used to track classes that implement boards
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class NesBoardImplAttribute : Attribute
	{
	}

	// this tracks derived boards that shouldn't be used by the implementation scanner
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class NesBoardImplCancelAttribute : Attribute
	{
	}

	// flags it as being priority, i.e. in the top of the list
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class NesBoardImplPriorityAttribute : Attribute
	{
	}
}
