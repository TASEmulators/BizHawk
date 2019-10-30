using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk.CustomControls
{
	public class FLPInGroupBox : GroupBox
	{
		public readonly FlowLayoutPanel InnerFLP = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			FlowDirection = FlowDirection.TopDown,
			WrapContents = false
		};

		public new ControlCollection Controls => InnerFLP.Controls;

		public FLPInGroupBox() => base.Controls.Add(InnerFLP);
	}
}