namespace Jellyfish.Virtu
{
	// Serves as a generalized interface to the BizHawk serializer
	public interface IComponentSerializer
	{
		bool IsReader { get; }
		void Sync(string name, ref bool val);
		void Sync(string name, ref int val);
		void Sync(string name, ref long val);
		void Sync(string name, ref ulong val);

		void Sync(string name, ref bool[] val, bool useNull);
		void Sync(string name, ref byte[] val, bool useNull);
		void Sync(string name, ref ushort[] val, bool useNull);
		void Sync(string name, ref int[] val, bool useNull);
	}
}
