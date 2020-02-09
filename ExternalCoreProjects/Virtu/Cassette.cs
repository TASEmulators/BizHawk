namespace Jellyfish.Virtu
{
	internal sealed class Cassette : MachineComponent
	{
		public Cassette() { }
		public Cassette(Machine machine) : base(machine)
		{
		}

		public bool ReadInput() => false;

		public void ToggleOutput()
		{
		}
	}
}
