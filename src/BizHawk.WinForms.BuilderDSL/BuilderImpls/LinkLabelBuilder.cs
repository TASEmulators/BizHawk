using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.WinForms.BuilderDSL
{
	public sealed class LinkLabelBuilder
		: IControlBuilder<LinkLabel>,
		IBuilderTakesAnchor,
		IBuilderTakesLabelText,
		IBuilderTakesPadding,
		IBuilderTakesPosition,
		IBuilderTakesSize
	{
		private AnchorStyles _anchor;

		private bool _autoSize = true;

		private string? _hyperlink;

		private Padding? _innerPadding;

		private string? _labelText;

		private Padding? _outerPadding;

		private Point? _pos;

		private Size? _size;

		public ControlBuilderContext Context { get; set; }

		public void AddFlagToAnchor(AnchorStyles flags) => _anchor |= flags;

		public void AutoSize()
		{
			_autoSize = true;
			_size = null;
		}

		public IFinalizedBuilder<LinkLabel>? BuildOrNull()
		{
			var c = new LinkLabel();

			// content
			if (_labelText != null) c.Text = _labelText;

			// position
			if (_pos != null)
			{
				if (Context.AutoPosOnly) throw new InvalidOperationException();
				c.Location = _pos.Value;
			}
			c.Anchor = _anchor;

			// size
			if (_autoSize) c.AutoSize = true;
			else if (Context.AutoSizeOnly) throw new InvalidOperationException();
			else if (_size != null) c.Size = _size.Value;
			if (_innerPadding != null) c.Padding = _innerPadding.Value;
			if (_outerPadding != null) c.Margin = _outerPadding.Value;

			// behaviour
			c.LinkClicked += (sender, e) => Process.Start(_hyperlink);

			return new FinalizedBuilder<LinkLabel>(c);
		}

		public void FixedSize(Size size)
		{
			_autoSize = false;
			_size = size;
		}

		public void Hyperlink(string uriString) => _hyperlink = uriString;

		public void InnerPadding(Padding padding) => _innerPadding = padding;

		public void LabelText(string labelText) => _labelText = labelText;

		public void OuterPadding(Padding padding) => _outerPadding = padding;

		public void Position(Point pos) => _pos = pos;

		public void SetAnchor(AnchorStyles anchor) => _anchor = anchor;

		public void UnsetAnchor() => _anchor = AnchorStyles.None;

		public void UnsetHyperlink() => _hyperlink = null;

		public void UnsetInnerPadding() => _innerPadding = null;

		public void UnsetLabelText() => _labelText = null;

		public void UnsetOuterPadding() => _outerPadding = null;

		public void UnsetPosition() => _pos = null;

		public void UnsetSize()
		{
			_autoSize = false;
			_size = null;
		}
	}
}
