namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio : IControlMainform
	{
		public bool WantsToControlReadOnly { get { return true; } }
		public void ToggleReadOnly()
		{
			GlobalWin.OSD.AddMessage("TAStudio does not allow manual readonly toggle");
		}

		public bool WantsToControlStopMovie { get; private set; }

		public void StopMovie()
		{
			this.Focus();
			NewTasMenuItem_Click(null, null);
		}
	}
}
