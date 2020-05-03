namespace BizHawk.WinForms.Controls
{
	public interface ITrackedRadioButton
	{
		/// <remarks>Does not declare a setter intentionally, use <see cref="UncheckFromTracker"/>.</remarks>
		bool Checked { get; }

		string Name { get; }

		object Tag { get; }

		void UncheckFromTracker();
	}
}
