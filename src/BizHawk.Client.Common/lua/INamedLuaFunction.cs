using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface INamedLuaFunction
	{
		Action InputCallback { get; }

		Guid Guid { get; }

		string GuidStr { get; }

		MemoryCallbackDelegate MemCallback { get; }

		/// <summary>for <c>doom.on_prandom</c>; single param: caller of RNG, per categories <see href="https://github.com/TASEmulators/dsda-doom/blob/7f03360ce0e9000c394fb99869d78adf4603ade5/prboom2/src/m_random.h#L63-L133">in source</see></summary>
		Action<int> RandomCallback { get; }

		/// <summary>for <c>doom.on_use and doom.on_cross</c>; two params: pointers to activated line and to mobj that triggered it</summary>
		Action<long, long> LineCallback { get; }

		string Name { get; }

		Action OnRemove { get; set; }
	}
}
