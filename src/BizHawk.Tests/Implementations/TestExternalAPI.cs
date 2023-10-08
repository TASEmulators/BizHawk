using BizHawk.Client.Common;

namespace BizHawk.Tests.Implementations
{
	[ExternalTool("TEST")]
	public class TestExternalAPI : IExternalToolForm
	{
		public ApiContainer? _maybeAPIContainer { get; set; }
		private ApiContainer APIs
			=> _maybeAPIContainer!;

		private int frameCount = 0;

		public bool IsActive => true;

		public bool IsLoaded => true;

		public bool ContainsFocus => false;


		public bool AskSaveChanges() => true;
		public void Close() {}
		public bool Focus() => false;
		public void Restart() { }
		public void Show() { }
		public void UpdateValues(ToolFormUpdateType type)
		{
			if (type == ToolFormUpdateType.PostFrame)
			{
				frameCount++;
			}
		}
	}
}
