namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio : IControlMainform
	{
		public void ToggleReadOnly()
		{
			GlobalWin.OSD.AddMessage("TAStudio does not allow manual readonly toggle");
		}
	}
}
