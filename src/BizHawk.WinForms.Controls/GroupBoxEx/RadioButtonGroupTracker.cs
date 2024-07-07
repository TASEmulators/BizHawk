using System.Collections.Generic;
using System.Windows.Forms;

namespace BizHawk.WinForms.Controls
{
	/// <summary>
	/// Functions as a collection of <see cref="ITrackedRadioButton">ITrackedRadioButtons</see>.<br/>
	/// Using this in our custom <see cref="GroupBox">GroupBoxes</see> circumvents the direct-child restriction on radio buttons.
	/// With that gone, we're free to use nested <see cref="FlowLayoutPanel">FLPs</see> in our layouts, the cost being the complexity of this class and its related types.
	/// </summary>
	/// <remarks>Elements should have unique <see cref="Control.Name">Names</see>; breaking this rule is UB, not checked at runtime.</remarks>
	/// <inheritdoc cref="IRadioButtonReadOnlyTracker"/>
	public sealed class RadioButtonGroupTracker : List<ITrackedRadioButton>, IRadioButtonReadOnlyTracker
	{
		/// <value>The selected radio button, or <see langword="null"/> if no button is selected or if the collection is empty.</value>
		public ITrackedRadioButton? Selection
		{
			get
			{
				if (Count == 0) return null;
				foreach (var rb in this) if (rb.Checked) return rb;
				return null;
			}
		}

		/// <returns>The <see cref="Control.Tag"/> of the selected radio button, cast to <typeparamref name="T"/><c>?</c>, or <see langword="null"/> if no button is selected or if the collection is empty.</returns>
		public T? GetSelectionTagAs<T>() where T : struct, Enum => (T?) Selection?.Tag;

		public void UpdateDeselected(string name)
		{
			foreach (var rb in this) if (rb.Name != name) rb.UncheckFromTracker();
		}
	}
}
