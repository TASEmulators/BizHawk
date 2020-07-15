using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BizHawk.WinForms.BuilderDSL
{
	/// <remarks>defaults: &lt;as Winforms> + auto-size + `Margin = <see cref="Padding.Empty"/>` + flow LTR-in-LTR (WinForms default is invariant LTR)</remarks>
	public sealed class FLPBuilder
		: IContainerBuilder<FlowLayoutPanel, Control>,
		IBuilderCanBeDisabled,
		IBuilderTakesAnchor,
		IBuilderTakesPadding,
		IBuilderTakesPosition,
		IBuilderTakesSize
	{
		private AnchorStyles? _anchor;

		private bool _autoSize = true;

		private FlowDirection _direction;

		private bool _disabled;

		private Padding? _innerPadding;

		private Padding? _outerPadding = Padding.Empty;

		private readonly ICollection<Control> _pendingChildren = new List<Control>();

		private Point? _pos;

		private Size? _size;

		private bool _wrapOnOverflow = true;

		public ControlBuilderContext Context { get; set; }

		public void AddChild(IFinalizedBuilder<Control> finalized) => _pendingChildren.Add(finalized.GetControlRef());

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

		public IFinalizedContainer<FlowLayoutPanel>? BuildOrNull()
		{
			var c = new FlowLayoutPanel();
			c.SuspendLayout();

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

			// behaviour
			c.FlowDirection = _direction;
			if (!_wrapOnOverflow) c.WrapContents = false;
			if (_disabled) c.Enabled = false;

			c.Controls.AddRange(_pendingChildren.ToArray());
			c.ResumeLayout();
			return new FinalizedContainer<FlowLayoutPanel>(c, Context);
		}

		public void Disable() => _disabled = true;

		public void FixedSize(Size size)
		{
			_autoSize = false;
			_size = size;
		}

		public void FlowDown() => SetFlowDirection(FlowDirection.TopDown);

		public void FlowLTR() => SetFlowDirection(FlowDirection.LeftToRight);

		public void FlowLTRInLTR()
		{
			if (Context.IsLTR) FlowLTR();
			else FlowRTL();
		}

		public void FlowRTL() => SetFlowDirection(FlowDirection.RightToLeft);

		public void FlowRTLInLTR()
		{
			if (Context.IsLTR) FlowRTL();
			else FlowLTR();
		}

		public void FlowUp() => SetFlowDirection(FlowDirection.BottomUp);

		public void InnerPadding(Padding padding) => _innerPadding = padding;

		public void OuterPadding(Padding padding) => _outerPadding = padding;

		public void Position(Point pos) => _pos = pos;

		public void SetAnchor(AnchorStyles anchor) => _anchor = anchor;

		public void SetFlowDirection(FlowDirection direction) => _direction = direction;

		public void SingleColumn() => _wrapOnOverflow = false;

		public void SingleRow() => SingleColumn();

		public void UnsetAnchor() => _anchor = null;

		public void UnsetInnerPadding() => _innerPadding = null;

		public void UnsetOuterPadding() => _outerPadding = null;

		public void UnsetPosition() => _pos = null;

		public void UnsetSize()
		{
			_autoSize = false;
			_size = null;
		}

		public void WrapToNewColumns() => _wrapOnOverflow = true;

		public void WrapToNewRows() => WrapToNewColumns();
	}
}
