using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk.CustomControls
{
	/// <seealso cref="SingleColumnFLP"/>
	public class SingleRowFLP : FlowLayoutPanel
	{
		public SingleRowFLP() : base()
		{
			AutoSize = true;
			WrapContents = false;
		}
	}
}
