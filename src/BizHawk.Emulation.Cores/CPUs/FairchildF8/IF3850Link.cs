namespace BizHawk.Emulation.Cores.Components.FairchildF8
{
	// Interface that has all the methods required by the F3850 to talk to
	// the emulator core.
	// Should only be used as a generic type argument for the F3850, and
	// implementations should be structs where possible. This combination allows
	// the JITer to generate much faster code than calling a Func<> or Action<>.
	public interface IF3850Link
	{
		// Memory Access
		byte ReadMemory(ushort address);
		void WriteMemory(ushort address, byte value);

		// Hardware I/O Port Access
		byte ReadHardware(ushort address);
		void WriteHardware(ushort address, byte value);

		// This is only called when the first byte of an instruction is fetched.
		void OnExecFetch(ushort address);
	}
}
