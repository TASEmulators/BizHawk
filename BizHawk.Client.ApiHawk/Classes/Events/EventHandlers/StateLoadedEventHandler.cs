namespace BizHawk.Client.ApiHawk.Classes.Events
{
	/// <summary>
	/// Represent a method that will handle the event raised when a savestate is loaded
	/// </summary>
	/// <param name="sender">Object that raised the event</param>
	/// <param name="e">Event arguments</param>
	public delegate void StateLoadedEventHandler(object sender, StateLoadedEventArgs e);
}
