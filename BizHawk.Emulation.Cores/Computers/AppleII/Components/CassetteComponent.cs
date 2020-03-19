using Jellyfish.Virtu;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	/// <summary>
	/// An empty implementation of ICassette, since we have not current built cassette functionality
	/// </summary>
	public class EmptyCassetteComponent : ICassette
	{
		// TODO: remove when json serialization is no longer used
		public EmptyCassetteComponent() { }

		public bool ReadInput() => false;

		public void ToggleOutput()
		{
		}
	}
}
