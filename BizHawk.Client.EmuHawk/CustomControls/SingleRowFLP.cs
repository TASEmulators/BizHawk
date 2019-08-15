using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk.CustomControls
{
	/// <seealso cref="SingleColumnFLP"/>
	public sealed class SingleRowFLP : FlowLayoutPanel
	{
		public SingleRowFLP()
		{
			AutoSize = true;
			WrapContents = false;
		}
	}
}