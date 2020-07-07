using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.WinForms.Controls;

namespace BizHawk.WinForms.BuilderDSL
{
	public sealed class RadioButtonExBuilder
		: IControlBuilder<RadioButtonEx>,
		IBuilderCanBeChecked,
		IBuilderCanBeDisabled,
		IBuilderPublishesCheckedChanged,
		IBuilderTakesAnchor,
		IBuilderTakesDataTag,
		IBuilderTakesLabelText,
		IBuilderTakesPadding,
		IBuilderTakesPosition,
		IBuilderTakesRBGroupTracker,
		IBuilderTakesSize
	{
		private AnchorStyles? _anchor;

		private bool _autoSize = true;

		private bool _checked;

		private object? _dataRef;

		private bool _disabled;

		private string? _labelText;

		private Padding? _innerPadding;

		private readonly ICollection<EventHandler> _onCheckedChanged = new List<EventHandler>();

		private Padding? _outerPadding;

		private Point? _pos;

		private Size? _size;

		private RadioButtonGroupTracker? _tracker;

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

		public IFinalizedBuilder<RadioButtonEx>? BuildOrNull()
		{
			var c = _tracker == null ? new RadioButtonEx() : new RadioButtonEx(_tracker);

			// content
			if (_labelText != null) c.Text = _labelText;
			if (_checked) c.Checked = true;

			// position
#if false
			if (_pos != null)
			{
				if (Context.AutoPosOnly) throw new InvalidOperationException();
				c.Location = _pos.Value;
			}
#endif
			if (_anchor != null) c.Anchor = _anchor.Value;

			// size
#if false
			if (_autoSize) c.AutoSize = true;
			else if (Context.AutoSizeOnly) throw new InvalidOperationException();
			else if (_size != null) c.Size = _size.Value;
#endif
			if (_innerPadding != null) c.Padding = _innerPadding.Value;
			if (_outerPadding != null) c.Margin = _outerPadding.Value;

			// appearance
			if (Context.IsRTL) c.CheckAlign = ContentAlignment.MiddleRight; // couldn't get Control.RightToLeft to work

			// behaviour
			if (_dataRef != null) c.Tag = _dataRef;
			if (_disabled) c.Enabled = false;
			foreach (var sub in _onCheckedChanged) c.CheckedChanged += sub;

			return new FinalizedBuilder<RadioButtonEx>(c);
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

		public void SetDataTag(object dataRef) => _dataRef = dataRef;

		public void SetInitialValue(bool initValue) => _checked = initValue;

		public void SetTracker(RadioButtonGroupTracker tracker)
		{
			if (_tracker != null) throw new InvalidOperationException();
			_tracker = tracker;
		}

		public void SubToCheckedChanged(EventHandler subscriber) => _onCheckedChanged.Add(subscriber);

		public void SubToCheckedChanged(Action<bool> subscriber) => this.SubToCheckedChanged((RadioButtonEx cb) => subscriber(cb.Checked));

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
