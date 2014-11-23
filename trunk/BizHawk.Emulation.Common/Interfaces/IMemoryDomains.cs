namespace BizHawk.Emulation.Common
{
	public interface IMemoryDomains : IEmulator, ICoreService
	{
		///The list of all avaialble memory domains
		/// A memory domain is a byte array that respresents a distinct part of the emulated system.
		/// By convention the Main Memory is the 1st domain in the list
		// All cores sould implement a System Bus domain that represents the standard cpu bus range for that system,
		/// and a Main Memory which is typically the WRAM space (for instance, on NES - 0000-07FF),
		/// Other chips, and ram spaces can be added as well.
		/// Subdomains of another domain are also welcome.
		/// The MainMemory identifier will be 0 if not set
		MemoryDomainList MemoryDomains { get; }
	}
}
