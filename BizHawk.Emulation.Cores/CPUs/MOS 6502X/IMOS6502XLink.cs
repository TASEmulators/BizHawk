namespace BizHawk.Emulation.Cores.Components.M6502
{
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
