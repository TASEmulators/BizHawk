namespace BizHawk.Client.EmuHawk
{
	public interface IControlMainform
	{
		bool WantsToControlReadOnly { get; }
		void ToggleReadOnly();

		bool WantsToCOntrolStopMovie { get; }
		void StopMovie();
	}
}
