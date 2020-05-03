using System.Windows.Forms;

namespace BizHawk.WinForms.Controls
{
	/// <summary>Functions as a write-only collection of <see cref="ITrackedRadioButton">ITrackedRadioButtons</see>.</summary>
	/// <remarks>Elements should have unique <see cref="Control.Name">Names</see>; breaking this rule is UB, not checked at runtime.</remarks>
	/// <seealso cref="RadioButtonGroupTracker"/>
	public interface IRadioButtonReadOnlyTracker
	{
		void Add(ITrackedRadioButton rb);

		void UpdateDeselected(string name);
	}
}
