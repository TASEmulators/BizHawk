namespace BizHawk.Client.Common
{
	public class TasClipboardEntry
	{
		public TasClipboardEntry(int frame, ILogEntryController controllerState)
		{
			Frame = frame;
			ControllerState = controllerState;
		}

		public int Frame { get; }
		public ILogEntryController ControllerState { get; }
	}
}
