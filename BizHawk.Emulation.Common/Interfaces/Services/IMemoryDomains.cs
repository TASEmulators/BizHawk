using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This service manages the ability for a client to read/write to memory regions of the core,
	/// It is a list of all avaialble memory domains
	/// A memory domain is a byte array that respresents the memory of a distinct part of the emulated system.
	/// All cores sould implement a SystemBus that represents the standard cpu bus range used for cheats for that system,
	/// In order to have a cheat system available for the core
	/// All domains should implement both peek and poke.  However, 
	/// if a domain does not implement poke, it should throw NotImplementedException rather than silently fail
	/// If this service is available the client will expose many RAM related tools such as the Hex Editor, RAM Search/Watch, and Cheats
	/// In addition, this is an essential service for effective lua scripting, and many other tools
	/// </summary>
	public interface IMemoryDomains : IEnumerable<MemoryDomain>, IEmulatorService
	{
		MemoryDomain this[string name] { get; }

		MemoryDomain MainMemory { get; set; }

		bool HasSystemBus { get; }

		MemoryDomain SystemBus { get; set; }

		bool Has(string name);
	}
}
