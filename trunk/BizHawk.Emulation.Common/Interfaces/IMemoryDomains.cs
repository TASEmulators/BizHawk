using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// The list of all avaialble memory domains
	/// A memory domain is a byte array that respresents a distinct part of the emulated system.
	/// All cores sould implement a CheatDomain that represents the standard cpu bus range used for cheats for that system,
	/// In order to have a cheat system available for the core
	/// All domains should implement both peek and poke.  However, if something isn't developed it should throw NotImplementedException rather than silently fail
	/// </summary>
	public interface IMemoryDomains : IEnumerable<MemoryDomain>, IEmulatorService
	{
		MemoryDomain this[string name] { get; }

		MemoryDomain MainMemory { get; set; }

		bool HasSystemBus { get; }

		MemoryDomain SystemBus { get; set; }
	}
}
