namespace BizHawk.Client.ApiHawk.Classes.Events
{
	/// <summary>
	/// Represent a method that will handle the event raised when a savestate is saved
	/// </summary>
	/// <param name="sender">Object that raised the event</param>
	/// <param name="e">Event arguments</param>
	public delegate void StateSavedEventHandler(object sender, StateSavedEventArgs e);
}
