using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This service manages the ability for a client to read/write to memory regions of the core,
	/// It is a list of all available memory domains
	/// A memory domain is a byte array that represents the memory of a distinct part of the emulated system.
	/// All cores should implement a SystemBus that represents the standard CPU bus range used for cheats for that system,
	/// In order to have a cheat system available for the core
	/// All domains should implement both peek and poke.  However,
	/// if a domain does not implement poke, it should throw NotImplementedException rather than silently fail
	/// If this service is available the client will expose many RAM related tools such as the Hex Editor, RAM Search/Watch, and Cheats
	/// In addition, this is an essential service for effective LUA scripting, and many other tools
	/// </summary>
	public interface IMemoryDomains : IReadOnlyList<MemoryDomain>, IEmulatorService
	{
		MemoryDomain? this[string name] { get; }

		/// <remarks>implementation note: must also be in the collection</remarks>
		MemoryDomain MainMemory { get; }

		bool HasSystemBus { get; }

		/// <remarks>
		/// if <see cref="HasSystemBus"/> is <see langword="false"/>, the getter behaviour is undefined
		/// (it may return <see cref="MainMemory"/> or another domain)<br/>
		/// implementation note: must also be in the collection
		/// </remarks>
		MemoryDomain SystemBus { get; }

		bool Has(string name);
	}
}
