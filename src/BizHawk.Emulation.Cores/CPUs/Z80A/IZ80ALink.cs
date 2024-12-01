namespace BizHawk.Emulation.Cores.Components.Z80A
{
	// Interface that has all the methods required by the Z80A to talk to
	// the emulator core.
	// Should only be used as a generic type argument for the Z80A, and
	// implementations should be structs where possible. This combination allows
	// the JITer to generate much faster code than calling a Func<> or Action<>.
	public interface IZ80ALink
	{
		// Memory Access
		byte FetchMemory(ushort address);
		byte ReadMemory(ushort address);
		void WriteMemory(ushort address, byte value);

		// Hardware I/O Port Access
		byte ReadHardware(ushort address);
		void WriteHardware(ushort address, byte value);

		// Data Bus
		// Interrupting Devices are responsible for putting a value onto the data bus
		// for as long as the interrupt is valid
		byte FetchDB();

		// This is only called when the first byte of an instruction is fetched.
		void OnExecFetch(ushort address);

		void IRQCallback();
		void NMICallback();

		// This will be a few cycles off for now
		// It should suffice for now until Alyosha returns from hiatus
		void IRQACKCallback();
	}
}
