namespace BizHawk.Client.Common
{
	public enum ToolFormUpdateType
	{
		/// <summary>
		/// Called by other tools and other events outside of a frame loop
		/// </summary>
		General,

		/// <summary>
		/// Called before a frame emulates
		/// </summary>
		PreFrame,
		FastPreFrame,

		/// <summary>
		/// Called after a frame emulates
		/// </summary>
		PostFrame,
		FastPostFrame
	}

	public interface IToolForm
	{
		/// <summary>
		/// Directs the tool to update, with an indicator of the type of update
		/// </summary>
		void UpdateValues(ToolFormUpdateType type);

		/// <summary>
		/// Will be called anytime the dialog needs to be restarted, such as when a new ROM is loaded
		/// The tool implementing this needs to account for a Game and Core change
		/// </summary>
		void Restart();

		/// <summary>
		/// This gives the opportunity for the tool dialog to ask the user to save changes (such is necessary when 
		/// This tool dialog edits a file.  Returning false will tell the client the user wants to cancel the given action,
		/// Return false to tell the client to back out of an action (such as closing the emulator)
		/// </summary>
		bool AskSaveChanges();

		/// <summary>
		/// Returns a value indicating whether or not the current tool is active and running
		/// </summary>
		bool IsActive { get; }

		/// <summary>
		/// Gets a value indicating whether a tool is actually open.
		/// This value should be the same as <see cref="IsActive"/>
		/// except for tools that can be closed/hidden,
		/// where the tool can be active but not loaded
		/// </summary>
		bool IsLoaded { get; }

		// Necessary winform calls
		bool Focus();
		bool ContainsFocus { get; }
		void Show();
		void Close();
	}
}
