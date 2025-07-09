namespace BizHawk.Client.Common
{
	/// <summary>
	/// When the implementing class also implements <see cref="IToolFormAutoConfig"/>, this interface's method is called when the generated <c>Restore Defaults</c> menu item is clicked.
	/// </summary>
	public interface IRestoreDefaults
	{
		void RestoreDefaults();
	}
}
