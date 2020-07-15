using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace BizHawk.WinForms.BuilderDSL
{
	public sealed class TabPageBuilder
		: IContainerBuilder<TabPage, Control>,
		IBuilderTakesContentText,
		IBuilderTakesPadding
	{
		private Padding? _innerPadding;

		private Padding? _outerPadding;

		private readonly ICollection<Control> _pendingChildren = new List<Control>();

		private string? _text;

		public ControlBuilderContext Context { get; set; }

		public void AddChild(IFinalizedBuilder<Control> finalized) => _pendingChildren.Add(finalized.GetControlRef());

		public IFinalizedContainer<TabPage>? BuildOrNull()
		{
			var c = new TabPage();
			c.SuspendLayout();

			// content
			if (_text != null) c.Text = _text;

			// size
			if (_innerPadding != null) c.Padding = _innerPadding.Value;
			if (_outerPadding != null) c.Margin = _outerPadding.Value;

			// appearance
			c.UseVisualStyleBackColor = true;

			c.Controls.AddRange(_pendingChildren.ToArray());
			c.ResumeLayout();
			return new FinalizedContainer<TabPage>(c, Context);
		}

		public void InnerPadding(Padding padding) => _innerPadding = padding;

		public void OuterPadding(Padding padding) => _outerPadding = padding;

		public void SetText(string text) => _text = text;

		public void UnsetInnerPadding() => _innerPadding = null;

		public void UnsetOuterPadding() => _outerPadding = null;

		public void UnsetText() => _text = null;
	}
}
