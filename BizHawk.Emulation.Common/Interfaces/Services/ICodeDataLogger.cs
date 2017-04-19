using System;
using System.IO;
using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This service manages the communication from the core to the Code/Data logging tool,
	/// If available then the Code/Data logging tool will be exposed in the client
	/// </summary>
	public interface ICodeDataLogger : IEmulatorService
	{
		/// <summary>
		/// Sets the CodeDataLog as current (and logging) on the core
		/// </summary>
		void SetCDL(ICodeDataLog cdl);

		/// <summary>
		/// Fills a new CodeDataLog with memory domain information suitable for the core
		/// </summary>
		void NewCDL(ICodeDataLog cdl);

		/// <summary>
		/// Disassembles the CodeDataLog to an output Stream. Can't be done without a core because there's no code to disassemble otherwise!
		/// This could be extended later to produce richer multi-file disassembly
		/// </summary>
		void DisassembleCDL(Stream s, ICodeDataLog cdl);
	}

	/// <summary>
	/// Defines a code/data log to be used with the code/data logger
	/// </summary>
	/// <seealso cref="ICodeDataLogger" /> 
	public interface ICodeDataLog : IDictionary<string, byte[]>
	{
		/// <summary>
		/// Pins the managed arrays. Not that we expect them to be allocated, but in case we do, seeing this here will remind us to check for the pin condition and abort
		/// </summary>
		void Pin();

		/// <summary>
		/// Unpins the managed arrays, to be paired with calls to Pin()
		/// </summary>
		void Unpin();

		/// <summary>
		/// Retrieves the pointer to a managed array
		/// </summary>
		IntPtr GetPin(string key);

		/// <summary>
		/// Whether the CDL is tracking a block with the given name
		/// </summary>
		bool Has(string blockname);

		/// <summary>
		/// Gets or sets a value indicating whether the status is active.
		/// This is just a hook, if needed, to readily suspend logging, without having to rewire the core
		/// </summary>
		bool Active { get; set; }

		string SubType { get; set; }

		int SubVer { get; set; }

		/// <summary>
		/// Tests whether the other CodeDataLog is structurally identical
		/// </summary>
		bool Check(ICodeDataLog other);

		void LogicalOrFrom(ICodeDataLog other);

		void ClearData();

		void Save(Stream s);
	}
}
