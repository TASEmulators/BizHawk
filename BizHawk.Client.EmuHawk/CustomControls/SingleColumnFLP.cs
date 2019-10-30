using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk.CustomControls
{
	/// <seealso cref="SingleRowFLP"/>
	public sealed class SingleColumnFLP : FlowLayoutPanel
	{
		public SingleColumnFLP()
		{
			AutoSize = true;
			FlowDirection = FlowDirection.TopDown;
			WrapContents = false;
		}
	}
}