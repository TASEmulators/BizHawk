using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BizHawk.WinForms.BuilderDSL
{
	public sealed class TabControlBuilder
		: IContainerBuilder<TabControl, TabPage>,
		IBuilderTakesAnchor,
		IBuilderTakesPosition,
		IBuilderTakesSize
	{
		private AnchorStyles? _anchor;

		private bool _autoSize = true;

		private readonly ICollection<Control> _pendingChildren = new List<Control>();

		private Point? _pos;

		private Size? _size;

		public ControlBuilderContext Context { get; set; }

		public void AddChild(IFinalizedBuilder<TabPage> finalized) => _pendingChildren.Add(finalized.GetControlRef());

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

		public IFinalizedContainer<TabControl>? BuildOrNull()
		{
			var c = new TabControl();
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

			c.Controls.AddRange(_pendingChildren.ToArray());
			c.ResumeLayout();
			return new FinalizedContainer<TabControl>(c, Context);
		}

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
