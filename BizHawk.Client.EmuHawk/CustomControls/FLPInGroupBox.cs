using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk.CustomControls
{
	/// <seealso cref="TLPInGroupBox"/>
	public class FLPInGroupBox : GroupBox
	{
		public new ControlCollection Controls => InnerFLP.Controls;

		public FlowLayoutPanel InnerFLP { get; } = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			FlowDirection = FlowDirection.TopDown,
			WrapContents = false
		};

		public FLPInGroupBox() : base() => base.Controls.Add(InnerFLP);
	}
}
