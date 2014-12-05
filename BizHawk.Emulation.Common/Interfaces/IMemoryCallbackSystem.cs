using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	public interface IMemoryCallbackSystem
	{
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
		/// Adds a Read callback for the given address
		/// If no address is specified the callback will be hooked to all addresses
		/// </summary>
		void AddRead(Action function, uint? addr);

		/// <summary>
		/// Adds a Write callback for the given address
		/// If no address is specified the callback will be hooked to all addresses
		/// </summary>
		void AddWrite(Action function, uint? addr);

		/// <summary>
		/// Adds an Execute callback for the given address
		/// </summary>
		void AddExecute(Action function, uint addr);

		/// <summary>
		/// Executes all Read callbacks for the given addr
		/// </summary>
		void CallRead(uint addr);

		/// <summary>
		/// Executes all Write callbacks for the given addr
		/// </summary>
		void CallWrite(uint addr);

		/// <summary>
		/// Executes all Execute callbacks for the given addr
		/// </summary>
		void CallExecute(uint addr);

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
}
