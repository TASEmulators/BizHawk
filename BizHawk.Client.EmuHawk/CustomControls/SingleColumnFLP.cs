using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk.CustomControls
{
	/// <seealso cref="SingleRowFLP"/>
	public class SingleColumnFLP : FlowLayoutPanel
	{
		public SingleColumnFLP() : base()
		{
			AutoSize = true;
			FlowDirection = FlowDirection.TopDown;
			WrapContents = false;
		}
	}
}
