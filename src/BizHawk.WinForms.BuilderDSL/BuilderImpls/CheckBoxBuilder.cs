using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.WinForms.BuilderDSL
{
	public sealed class CheckBoxBuilder
		: IControlBuilder<CheckBox>,
		IBuilderCanBeChecked,
		IBuilderCanBeDisabled,
		IBuilderPublishesCheckedChanged,
		IBuilderTakesAnchor,
		IBuilderTakesLabelText,
		IBuilderTakesPadding,
		IBuilderTakesPosition,
		IBuilderTakesSize
	{
		private AnchorStyles? _anchor;

		private bool _autoSize = true;

		private bool _checked;

		private bool _disabled;

		private Padding? _innerPadding;

		private string? _labelText;

		private readonly ICollection<EventHandler> _onCheckedChanged = new List<EventHandler>();

		private Padding? _outerPadding;

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

		public IFinalizedBuilder<CheckBox>? BuildOrNull()
		{
			var c = new CheckBox();

			// content
			if (_labelText != null) c.Text = _labelText;
			if (_checked) c.Checked = true;

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
			if (_innerPadding != null) c.Padding = _innerPadding.Value;
			if (_outerPadding != null) c.Margin = _outerPadding.Value;

			// appearance
			if (Context.IsRTL) c.CheckAlign = ContentAlignment.MiddleRight; // couldn't get Control.RightToLeft to work

			// behaviour
			if (_disabled) c.Enabled = false;
			foreach (var sub in _onCheckedChanged) c.CheckedChanged += sub;

			return new FinalizedBuilder<CheckBox>(c);
		}

		public void Disable() => _disabled = true;

		public void FixedSize(Size size)
		{
			_autoSize = false;
			_size = size;
		}

		public void InnerPadding(Padding padding) => _innerPadding = padding;

		public void LabelText(string labelText) => _labelText = labelText;

		public void OuterPadding(Padding padding) => _outerPadding = padding;

		public void Position(Point pos) => _pos = pos;

		public void SetAnchor(AnchorStyles anchor) => _anchor = anchor;

		public void SetInitialValue(bool initValue) => _checked = initValue;

		public void SubToCheckedChanged(EventHandler subscriber) => _onCheckedChanged.Add(subscriber);

		public void SubToCheckedChanged(Action<bool> subscriber) => this.SubToCheckedChanged((CheckBox cb) => subscriber(cb.Checked));

		public void UnsetAnchor() => _anchor = null;

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
