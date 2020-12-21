using System.ComponentModel;
using System.Windows.Forms;

namespace BizHawk.WinForms.Controls
{
	public abstract class RadioButtonExBase : RadioButton, ICheckBoxOrRadioEx, ITrackedRadioButton
	{
		/// <remarks>use to prevent recursion</remarks>
		protected bool CheckedChangedCausedByTracker { get; private set; }

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new int TabIndex => base.TabIndex;

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new bool TabStop => base.TabStop;

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new bool UseVisualStyleBackColor => base.UseVisualStyleBackColor;

		protected RadioButtonExBase() {}

		protected RadioButtonExBase(IRadioButtonReadOnlyTracker tracker)
		{
			tracker.Add(this);
			CheckedChanged += (changedSender, changedArgs) =>
			{
				if (((RadioButtonExBase) changedSender).Checked) tracker.UpdateDeselected(Name);
			};
		}

		public void UncheckFromTracker()
		{
			CheckedChangedCausedByTracker = true;
			Checked = false;
			CheckedChangedCausedByTracker = false;
		}
	}
}
