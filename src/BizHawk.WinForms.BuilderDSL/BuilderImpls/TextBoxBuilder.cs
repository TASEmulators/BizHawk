using System;
using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.WinForms.BuilderDSL
{
	public sealed class TextBoxBuilder
		: IControlBuilder<TextBox>,
		IBuilderTakesAnchor,
		IBuilderTakesInitialValue<string>,
		IBuilderTakesPosition,
		IBuilderTakesSize
	{
		private AnchorStyles? _anchor;

		private bool _autoSize = true;

		private string? _initValue;

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

		public IFinalizedBuilder<TextBox>? BuildOrNull()
		{
			var c = new TextBox();

			// content
			if (_initValue != null) c.Text = _initValue;

			// position
			if (_pos != null)
			{
				if (Context.AutoPosOnly) throw new InvalidOperationException();
				c.Location = _pos.Value;
			}
			if (_anchor != null) c.Anchor = _anchor.Value;

			// size
			if (_autoSize) c.AutoSize = true;
			else if (Context.AutoSizeOnly) throw new InvalidOperationException();
			else if (_size != null) c.Size = _size.Value;

			return new FinalizedBuilder<TextBox>(c);
		}

		public void FixedSize(Size size)
		{
			_autoSize = false;
			_size = size;
		}

		public void Position(Point pos) => _pos = pos;

		public void SetAnchor(AnchorStyles anchor) => _anchor = anchor;

		public void SetInitialValue(string initValue) => _initValue = initValue;

		public void UnsetAnchor() => _anchor = null;

		public void UnsetPosition() => _pos = null;

		public void UnsetSize()
		{
			_autoSize = false;
			_size = null;
		}
	}
}
