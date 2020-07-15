using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.WinForms.Controls;

namespace BizHawk.WinForms.BuilderDSL
{
#if false
	public sealed class GroupBoxExBuilder
		: IContainerBuilder<GroupBoxEx, Control>,
		IBuilderCanBeDisabled,
		IBuilderTakesLabelText,
		IBuilderTakesPosition,
		IBuilderTakesRBGroupTracker,
		IBuilderTakesSize
	{
		private bool _autoSize = true;

		private bool _disabled;

		private string? _labelText;

		private readonly ICollection<Control> _pendingChildren = new List<Control>();

		private Point? _pos;

		private Size? _size;

		private RadioButtonGroupTracker? _tracker;

		public ControlBuilderContext Context { get; set; }

		public void AddChild(IFinalizedBuilder<Control> finalized) => _pendingChildren.Add(finalized.GetControlRef());

		public void AutoSize()
		{
			_autoSize = true;
			_size = null;
		}

		public IFinalizedContainer<GroupBoxEx>? BuildOrNull()
		{
			var c = _tracker == null ? new GroupBoxEx() : new GroupBoxEx(_tracker);
			c.SuspendLayout();

			// content
			if (_labelText != null) c.Text = _labelText;

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

			c.Controls.AddRange(_pendingChildren.ToArray());
			c.ResumeLayout();
			return new FinalizedContainer<GroupBoxEx>(c, Context);
		}

		public void Disable() => _disabled = true;

		public void FixedSize(Size size)
		{
			_autoSize = false;
			_size = size;
		}

		public void LabelText(string labelText) => _labelText = labelText;

		public void Position(Point pos) => _pos = pos;

		public void SetTracker(RadioButtonGroupTracker tracker)
		{
			if (_tracker != null) throw new InvalidOperationException();
			_tracker = tracker;
		}

		public void UnsetLabelText() => _labelText = null;

		public void UnsetPosition() => _pos = null;

		public void UnsetSize()
		{
			_autoSize = false;
			_size = null;
		}
	}
#endif
}
