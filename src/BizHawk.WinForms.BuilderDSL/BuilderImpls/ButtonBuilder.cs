using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.WinForms.BuilderDSL
{
	public sealed class ButtonBuilder
		: IControlBuilder<Button>,
		IBuilderPublishesClick,
		IBuilderTakesAnchor,
		IBuilderTakesContentText,
		IBuilderTakesPosition,
		IBuilderTakesSize
	{
		private AnchorStyles? _anchor;

		private bool _autoSize = true;

		private DialogResult? _dialogResult;

		private readonly ICollection<EventHandler> _onClick = new List<EventHandler>();

		private Point? _pos;

		private Size? _size;

		private string? _text;

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

		public IFinalizedBuilder<Button>? BuildOrNull()
		{
			var c = new Button();

			// content
			if (_text != null) c.Text = _text;

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

			// behaviour
			if (_dialogResult != null) c.DialogResult = _dialogResult.Value;
			foreach (var sub in _onClick) c.Click += sub;

			return new FinalizedBuilder<Button>(c);
		}

		public void FixedSize(Size size)
		{
			_autoSize = false;
			_size = size;
		}

		public void Position(Point pos) => _pos = pos;

		public void SetAnchor(AnchorStyles anchor) => _anchor = anchor;

		public void SetDialogResult(DialogResult dialogResult) => _dialogResult = dialogResult;

		public void SetText(string text) => _text = text;

		public void SubToClick(EventHandler subscriber) => _onClick.Add(subscriber);

		public void UnsetAnchor() => _anchor = null;

		public void UnsetPosition() => _pos = null;

		public void UnsetSize()
		{
			_autoSize = false;
			_size = null;
		}

		public void UnsetText() => _text = null;
	}
}
