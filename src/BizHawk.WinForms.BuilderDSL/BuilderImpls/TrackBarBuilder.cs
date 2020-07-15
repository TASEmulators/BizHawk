using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.WinForms.Controls;

namespace BizHawk.WinForms.BuilderDSL
{
	public sealed class TrackBarBuilder
		: IControlBuilder<TrackBar>,
		IBuilderPublishesScroll,
		IBuilderPublishesValueChanged,
		IBuilderTakesInitialValue<int>,
		IBuilderTakesPosition,
		IBuilderTakesRange<int>,
		IBuilderTakesSize
	{
		private bool _autoSize = true;

		private int? _bigStep;

		private int? _initValue;

		private bool _isVertical;

		private int? _markEvery;

		private readonly ICollection<EventHandler> _onScroll = new List<EventHandler>();

		private readonly ICollection<EventHandler> _onValueChanged = new List<EventHandler>();

		private Point? _pos;

		private Size? _size;

		private Range<int>? _validRange;

		public ControlBuilderContext Context { get; set; }

		public void AutoSize()
		{
			_autoSize = true;
			_size = null;
		}

		public IFinalizedBuilder<TrackBar>? BuildOrNull()
		{
#if true
			var c = new TrackBar();
#else
			var c = new TransparentTrackBar();
#endif
			c.BeginInit();

			// content
			if (_validRange != null) // must be set before TrackBar.Value
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
			c.Orientation = _isVertical ? Orientation.Vertical : Orientation.Horizontal; // should be under appearance, but must be set before Size
			if (_autoSize) c.AutoSize = true;
			else if (Context.AutoSizeOnly) throw new InvalidOperationException();
			else if (_size != null) c.Size = _size.Value;

			// appearance
			if (_markEvery != null) c.TickFrequency = _markEvery.Value;

			// behaviour
			if (_bigStep != null) c.LargeChange = _bigStep.Value;
			foreach (var sub in _onScroll) c.Scroll += sub;
			foreach (var sub in _onValueChanged) c.ValueChanged += sub;

			c.EndInit();
			return new FinalizedBuilder<TrackBar>(c);
		}

		public void FixedSize(Size size)
		{
			_autoSize = false;
			_size = size;
		}

		public void OrientHorizontally() => _isVertical = false;

		public void OrientVertically() => _isVertical = true;

		public void Position(Point pos) => _pos = pos;

		public void SetBigStep(int bigStep) => _bigStep = bigStep;

		public void SetInitialValue(int initValue) => _initValue = initValue;

		public void SetTickFrequency(int markEvery) => _markEvery = markEvery;

		public void SetValidRange(Range<int> validRange) => _validRange = validRange;

		public void SubToScroll(EventHandler subscriber) => _onScroll.Add(subscriber);

		public void SubToScroll(Action<int> subscriber) => this.SubToScroll((TrackBar tb) => subscriber(tb.Value));

		public void SubToValueChanged(EventHandler subscriber) => _onValueChanged.Add(subscriber);

		public void SubToValueChanged(Action<int> subscriber) => this.SubToValueChanged((TrackBar tb) => subscriber(tb.Value));

		public void UnsetPosition() => _pos = null;

		public void UnsetSize()
		{
			_autoSize = false;
			_size = null;
		}
	}
}
