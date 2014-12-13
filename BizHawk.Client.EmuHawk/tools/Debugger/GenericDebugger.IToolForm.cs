namespace BizHawk.Client.EmuHawk
{
	public partial class GenericDebugger : IToolForm
	{
		public void UpdateValues()
		{
			RegisterPanel.UpdateValues();
		}

		public void FastUpdate()
		{
			// TODO
		}

		public void Restart()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

			// TODO
		}

		public bool AskSaveChanges()
		{
			// TODO
			return true;
		}

		public bool UpdateBefore
		{
			get { return false; }
		}
	}
}
