using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.WinForms.BuilderDSL
{
	public sealed class ListBoxBuilder
		: IControlBuilder<ListBox>,
		IBuilderTakesAnchor,
		IBuilderTakesPosition,
		IBuilderTakesSize
	{
		private AnchorStyles? _anchor;

		private bool _autoSize = true;

		private bool _formatting;

		private Point? _pos;

		private Size? _size;

		public ControlBuilderContext Context { get; set; }

		public void AddFlagToAnchor(AnchorStyles flags)
		{
			_anchor ??= AnchorStyles.None;
			_anchor |= flags;
		}

		public void AutoSize()
		{
			_autoSize = true;
			_size = null;
		}

		public IFinalizedBuilder<ListBox>? BuildOrNull()
		{
			var c = new ListBox();
			if (Context.IsRTL) c.RightToLeft = RightToLeft.Yes;
			if (_anchor != null) c.Anchor = _anchor.Value;
			if (_autoSize) c.AutoSize = true;
			else if (_size != null) c.Size = _size.Value;
			if (_formatting) c.FormattingEnabled = true;
			if (_pos != null) c.Location = _pos.Value;
			return new FinalizedBuilder<ListBox>(c);
		}

		public void DisableFormatting() => _formatting = true;

		public void EnableFormatting() => _formatting = true;

		public void FixedSize(Size size)
		{
			_autoSize = false;
			_size = size;
		}

		public void Position(Point pos) => _pos = pos;

		public void SetAnchor(AnchorStyles anchor) => _anchor = anchor;

		public void UnsetAnchor() => _anchor = null;

		public void UnsetPosition() => _pos = null;

		public void UnsetSize()
		{
			_autoSize = false;
			_size = null;
		}
	}
}
