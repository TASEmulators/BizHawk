using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface INamedLuaFunction
	{
		Action InputCallback { get; }

		Guid Guid { get; }

		string GuidStr { get; }

		MemoryCallbackDelegate MemCallback { get; }

		/// <summary>for <c>doom.on_prandom</c>; single param: info on what changed the RNG index</summary>
		Action<string> RandomCallback { get; }

		/// <summary>for <c>doom.on_intercept</c>; single param: blockmap block the intercept happened in</summary>
		Action<int, int, int> InterceptCallback { get; }

		/// <summary>for <c>doom.on_use and doom.on_cross</c>; two params: pointers to activated line and to mobj that triggered it</summary>
		Action<long, long> LineCallback { get; }

		string Name { get; }

		Action OnRemove { get; set; }
	}
}
