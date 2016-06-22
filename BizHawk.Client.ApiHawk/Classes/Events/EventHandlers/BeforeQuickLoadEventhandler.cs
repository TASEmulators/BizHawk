namespace BizHawk.Client.ApiHawk.Classes.Events
{
	/// <summary>
	/// Represent a method that will handle the event raised before a quickload is done
	/// </summary>
	/// <param name="sender">Object that raised the event</param>
	/// <param name="e">Event arguments</param>
	public delegate void BeforeQuickLoadEventHandler(object sender, BeforeQuickLoadEventArgs e);
}
