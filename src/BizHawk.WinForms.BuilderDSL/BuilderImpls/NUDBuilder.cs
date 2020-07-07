using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Common;

namespace BizHawk.WinForms.BuilderDSL
{
	public sealed class NUDBuilder
		: IControlBuilder<NumericUpDown>,
		IBuilderCanBeDisabled,
		IBuilderPublishesValueChanged,
		IBuilderTakesInitialValue<decimal>,
		IBuilderTakesPosition,
		IBuilderTakesRange<decimal>,
		IBuilderTakesSize
	{
		private bool _autoSize = true;

		private bool _disabled;

		private decimal? _initValue;

		private readonly ICollection<EventHandler> _onValueChanged = new List<EventHandler>();

		private Point? _pos;

		private Size? _size;

		private Range<decimal>? _validRange;

		public ControlBuilderContext Context { get; set; }

		public void AutoSize()
		{
			_autoSize = true;
			_size = null;
		}

		public IFinalizedBuilder<NumericUpDown>? BuildOrNull()
		{
			var c = new NumericUpDown();
			c.BeginInit();

			// content
			if (_validRange != null) // must be set before NumericUpDown.Value
			{
				c.Minimum = _validRange.Start;
				c.Maximum = _validRange.EndInclusive;
			}
			if (_initValue != null) c.Value = _validRange != null ? _initValue.Value.ConstrainWithin(_validRange) : _initValue.Value;

			// position
			if (_pos != null)
			{
				if (Context.AutoPosOnly) throw new InvalidOperationException();
				c.Location = _pos.Value;
			}

			// size
			if (_autoSize) c.AutoSize = true;
			else if (Context.AutoSizeOnly) throw new InvalidOperationException();
			else if (_size != null) c.Size = _size.Value;

			// behaviour
			if (_disabled) c.Enabled = false;
			foreach (var sub in _onValueChanged) c.ValueChanged += sub;

			c.EndInit();
			return new FinalizedBuilder<NumericUpDown>(c);
		}

		public void Disable() => _disabled = true;

		public void FixedSize(Size size)
		{
			_autoSize = false;
			_size = size;
		}

		public void Position(Point pos) => _pos = pos;

		public void SetInitialValue(decimal initValue) => _initValue = initValue;

		public void SetValidRange(Range<decimal> validRange) => _validRange = validRange;

		public void SubToValueChanged(EventHandler subscriber) => _onValueChanged.Add(subscriber);

		public void SubToValueChanged(Action<decimal> subscriber) => this.SubToValueChanged((NumericUpDown nud) => subscriber(nud.Value));

		public void UnsetPosition() => _pos = null;

		public void UnsetSize()
		{
			_autoSize = false;
			_size = null;
		}
	}
}
