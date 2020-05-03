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

		/// <summary>
		/// Called after a frame emulates
		/// </summary>
		PostFrame
	}

	public interface IToolForm
	{
		/// <summary>
		/// A new extensible update method
		/// </summary>
		void NewUpdate(ToolFormUpdateType type);

		/// <summary>
		/// Will be called by the client when performance is critical,
		/// The tool should only do the minimum to still function,
		/// Drawing should not occur if possible, during a fast update
		/// </summary>
		void FastUpdate();

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
		/// Indicates whether the tool should be updated before a frame loop or after.
		/// In general, tools that draw graphics from the core should update before the loop,
		/// Information tools such as those that display core ram values should be after.
		/// AWESOME! no separate preupdate and postupdate hooks. seriously?
		/// </summary>
		bool UpdateBefore { get; }

		// Necessary winform calls
		bool Focus();
		bool ContainsFocus { get; }
		void Show();
		void Close();
		bool IsDisposed { get; }
		bool IsHandleCreated { get; }
	}
}
