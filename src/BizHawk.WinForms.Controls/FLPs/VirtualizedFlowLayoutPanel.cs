using System.Windows.Forms;

using BizHawk.Common.NumberExtensions;

namespace BizHawk.Client.EmuHawk;

public class VirtualizedFlowLayoutPanel : FlowLayoutPanel
{
	private VScrollBar? _boundScrollBar;
	public VScrollBar BoundScrollBar
	{
		get => _boundScrollBar!;
		set
		{
			_boundScrollBar = value;
			_boundScrollBar.SmallChange = 5;
			_boundScrollBar.LargeChange = this.Height;
		}
	}

	protected override void OnSizeChanged(EventArgs e)
	{
		if (_boundScrollBar is not null)
			_boundScrollBar.LargeChange = this.Height;
		base.OnSizeChanged(e);
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		BoundScrollBar.Value = (BoundScrollBar.Value - e.Delta).Clamp(BoundScrollBar.Minimum, BoundScrollBar.Maximum - BoundScrollBar.LargeChange + 1);
	}
}
