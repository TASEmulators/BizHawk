namespace BizHawk.Emulation.Cores.Components.M6502
{
	// Interface that has all the methods required by the MOS 6502X to talk to
	// the emulator core.
	// Should only be used as a generic type argument for the MOS 6502X, and
	// implementations should be structs where possible. This combination allows
	// the JITer to generate much faster code than calling a Func<> or Action<>.
	public interface IMOS6502XLink
	{
		byte ReadMemory(ushort address);
		byte DummyReadMemory(ushort address);
		byte PeekMemory(ushort address);
		void WriteMemory(ushort address, byte value);

		// This only calls when the first byte of an instruction is fetched.
		void OnExecFetch(ushort address);
	}
}
