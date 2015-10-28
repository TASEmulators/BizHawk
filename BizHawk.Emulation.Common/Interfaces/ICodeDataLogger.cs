using System.IO;

namespace BizHawk.Emulation.Common
{
	public interface ICodeDataLogger : IEmulatorService
	{
		/// <summary>
		/// Sets the CodeDataLog as current (and logging) on the core
		/// </summary>
		void SetCDL(CodeDataLog cdl);

		/// <summary>
		/// Fills a new CodeDataLog with memory domain information suitable for the core
		/// </summary>
		void NewCDL(CodeDataLog cdl);

		/// <summary>
		/// Disassembles the CodeDataLog to an output Stream. Can't be done without a core because there's no code to disassemble otherwise!
		/// This could be extended later to produce richer multi-file disassembly
		/// </summary>
		void DisassembleCDL(Stream s, CodeDataLog cdl);
	}
}
