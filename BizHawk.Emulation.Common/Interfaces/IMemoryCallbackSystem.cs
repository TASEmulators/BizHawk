using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This is a property of IDebuggable, and defines the means by which a client
	/// gets and sets memory callbacks in the core.  A memory callback should fire any time memory is
	/// read/written/executed by the core, and depends on the type specified by the callback
	/// </summary>
	/// <seealso cref="IDebuggable"/> 
	public interface IMemoryCallbackSystem : IEnumerable<IMemoryCallback>
	{
		/*
		 * DANGER:
		 * Many cores will blindly call CallReads(), CallWrites(), CallExecutes() on every rwx no matter what.
		 * These functions must return very quickly if the list is empty.  Very very quickly.
		 */

		/// <summary>
		/// Specifies whether or not Execute callbacks are available for this this implementation
		/// </summary>
		bool ExecuteCallbacksAvailable { get; }

		/// <summary>
		/// Returns whether or not there are currently any read hooks
		/// </summary>
		bool HasReads { get; }

		/// <summary>
		/// Returns whether or not there are currently any write hooks
		/// </summary>
		bool HasWrites { get; }

		/// <summary>
		/// Returns whether or not there are currently any execute hooks
		/// </summary>
		bool HasExecutes { get; }

		/// <summary>
		/// Adds a callback for the given type to the given address
		/// If no address is specified the callback will be hooked to all addresses
		/// Note: an execute callback can not be added without an address, else an InvalidOperationException will occur
		/// </summary>
		void Add(IMemoryCallback callback);

		/// <summary>
		/// Executes all Read callbacks for the given addr
		/// </summary>
		void CallReads(uint addr);

		/// <summary>
		/// Executes all Write callbacks for the given addr
		/// </summary>
		void CallWrites(uint addr);

		/// <summary>
		/// Executes all Execute callbacks for the given addr
		/// </summary>
		void CallExecutes(uint addr);

		/// <summary>
		/// Removes the given callback from the list
		/// </summary>
		void Remove(Action action);

		/// <summary>
		/// Removes the given callbacks from the list
		/// </summary>
		void RemoveAll(IEnumerable<Action> actions);

		/// <summary>
		/// Removes all read,write, and execute callbacks
		/// </summary>
		void Clear();
	}

	/// <summary>
	/// This service defines a memory callback used by an IMemoryCallbackSystem implementation
	/// </summary>
	/// <seealso cref="IMemoryCallbackSystem"/> 
	public interface IMemoryCallback
	{
		MemoryCallbackType Type { get; }
		string Name { get; }
		Action Callback { get; }
		uint? Address { get; }
		uint? AddressMask { get; }
	}

	public enum MemoryCallbackType { Read, Write, Execute }
}
