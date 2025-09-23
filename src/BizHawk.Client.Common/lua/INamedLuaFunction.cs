using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	[CLSCompliant(false)]
	public interface INamedLuaFunction
	{
		Action InputCallback { get; }

		Guid Guid { get; }

		string GuidStr { get; }

		MemoryCallbackDelegate MemCallback { get; }

		string Name { get; }

		Action OnRemove { get; set; }
	}
}
