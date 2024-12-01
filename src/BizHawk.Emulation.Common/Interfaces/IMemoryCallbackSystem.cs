#nullable disable

using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	/// <param name="value">For reads/execs, the value read/executed; for writes, the value to be written. Cores may pass the default <c>0</c> if write/exec is partially implemented.</param>
	public delegate void MemoryCallbackDelegate(uint address, uint value, uint flags);

	/// <summary>
	/// This is a property of <see cref="IDebuggable"/>, and defines the means by which a client
	/// gets and sets memory callbacks in the core.  A memory callback should fire any time memory is
	/// read/written/executed by the core, and depends on the type specified by the callback
	/// </summary>
	public interface IMemoryCallbackSystem : IEnumerable<IMemoryCallback>
	{
		/*
		 * DANGER:
		 * Many cores will blindly call CallReads(), CallWrites(), CallExecutes() on every rwx no matter what.
		 * These functions must return very quickly if the list is empty.  Very very quickly.
		 */

		/// <summary>
		/// Gets a value indicating whether or not Execute callbacks are available for this this implementation
		/// </summary>
		bool ExecuteCallbacksAvailable { get; }

		/// <summary>
		/// Gets a value indicating whether or not there are currently any read hooks
		/// </summary>
		bool HasReads { get; }

		/// <summary>
		/// Gets a value indicating whether or not there are currently any write hooks
		/// </summary>
		bool HasWrites { get; }

		/// <summary>
		/// Gets a value indicating whether or not there are currently any execute hooks
		/// </summary>
		bool HasExecutes { get; }

		/// <summary>
		/// Gets a value indicating whether or not there are currently any read hooks
		/// </summary>
		bool HasReadsForScope(string scope);

		/// <summary>
		/// Gets a value indicating whether or not there are currently any write hooks
		/// </summary>
		bool HasWritesForScope(string scope);

		/// <summary>
		/// Gets a value indicating whether or not there are currently any execute hooks
		/// </summary>
		bool HasExecutesForScope(string scope);

		/// <summary>
		/// Adds a callback for the given type to the given address
		/// If no address is specified the callback will be hooked to all addresses
		/// Note: an execute callback can not be added without an address, else an InvalidOperationException will occur
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown when the <see cref="IMemoryCallback.Scope"/> property of the <see cref="IMemoryCallback"/> is not in the <see cref="AvailableScopes"/></exception>
		void Add(IMemoryCallback callback);

		/// <summary>
		/// Executes all matching callbacks for the given address and domain
		/// </summary>
		/// <param name="addr">The address to check for callbacks</param>
		/// <param name="value">For reads/execs, the value read/executed; for writes, the value to be written. Cores may pass the default <c>0</c> if write/exec is partially implemented.</param>
		/// <param name="flags">The callback flags relevant to this access</param>
		/// <param name="scope">The scope that the address pertains to. Must be a value in <see cref="AvailableScopes"/></param>
		void CallMemoryCallbacks(uint addr, uint value, uint flags, string scope);

		/// <summary>
		/// Removes the given callback from the list
		/// </summary>
		void Remove(MemoryCallbackDelegate action);

		/// <summary>
		/// Removes the given callbacks from the list
		/// </summary>
		void RemoveAll(IEnumerable<MemoryCallbackDelegate> actions);

		/// <summary>
		/// Removes all read,write, and execute callbacks
		/// </summary>
		void Clear();

		/// <summary>
		/// A list of available "scopes" (memory domains, cpus, etc) that a the <see cref="IMemoryCallback.Scope"/> property of the <see cref="IMemoryCallback"/> can have
		/// Passing a <see cref="IMemoryCallback"/> into the <see cref="Add(IMemoryCallback)"/> method that is not in this list will result in an <see cref="InvalidOperationException"/>
		/// </summary>
		string[] AvailableScopes { get; }
	}

	/// <summary>
	/// This service defines a memory callback used by an IMemoryCallbackSystem implementation
	/// </summary>
	/// <seealso cref="IMemoryCallbackSystem"/>
	/// <seealso cref="MemoryCallbackDelegate"/>
	public interface IMemoryCallback
	{
		MemoryCallbackType Type { get; }
		string Name { get; }

		/// <seealso cref="MemoryCallbackDelegate"/>
		MemoryCallbackDelegate Callback { get; }

		uint? Address { get; }
		uint? AddressMask { get; }
		string Scope { get; }
	}

	public enum MemoryCallbackType
	{
		Read, Write, Execute
	}

	[Flags]
	public enum MemoryCallbackFlags : uint
	{
		SizeUnknown = 0x00 << 16,
		SizeByte = 0x01 << 16,
		SizeWord = 0x02 << 16,
		SizeLong = 0x03 << 16,
		AccessUnknown = 0x00 << 12,
		AccessRead = 0x01 << 12,
		AccessWrite = 0x02 << 12,
		AccessExecute = 0x04 << 12,
		CPUUnknown = 0x00 << 8,
		CPUZero = 0x01 << 8,
		DomainUnknown = 0x00
	}
}
