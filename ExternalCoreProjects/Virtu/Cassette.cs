namespace Jellyfish.Virtu
{
	internal sealed class Cassette : MachineComponent
	{
		// ReSharper disable once UnusedMember.Global
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
