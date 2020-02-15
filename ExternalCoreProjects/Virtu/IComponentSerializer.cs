namespace Jellyfish.Virtu
{
	// Serves as a generalized interface to the BizHawk serializer
	public interface IComponentSerializer
	{
		void Sync(string name, ref bool val);
		void Sync(string name, ref int val);
		void Sync(string name, ref long val);
	}
}
