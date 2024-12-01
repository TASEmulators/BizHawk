namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This object facilitates communications between client and core
	/// The primary use is to provide a client => core communication, such as providing client-side callbacks for a core to use
	/// Any communications that can be described as purely a Core -> Client system, should be provided as an <see cref="IEmulatorService"/> instead
	/// It is important that by design this class stay immutable
	/// </summary>
	public class CoreComm
	{
		public CoreComm(
			Action<string> showMessage,
			Action<string, int?> notifyMessage,
			ICoreFileProvider coreFileProvider,
			CorePreferencesFlags prefs,
			IOpenGLProvider oglProvider
			)
		{
			ShowMessage = showMessage;
			Notify = notifyMessage;
			CoreFileProvider = coreFileProvider;
			CorePreferences = prefs;
			OpenGLProvider = oglProvider;
		}

		public ICoreFileProvider CoreFileProvider { get; }

		/// <summary>
		/// Gets a message to show. Reasonably annoying (dialog box), shouldn't be used most of the time
		/// </summary>
		public Action<string> ShowMessage { get; }

		/// <summary>
		/// Gets a message to show for optional duration in seconds. Less annoying (OSD message). Should be used for ignorable helpful messages
		/// </summary>
		public Action<string, int?> Notify { get; }

		[Flags]
		public enum CorePreferencesFlags
		{
			None = 0,
			WaterboxCoreConsistencyCheck = 1,
			WaterboxMemoryConsistencyCheck = 2
		}

		/// <summary>
		/// Yeah, I put more stuff in corecomm. If you don't like it, change the settings/syncsettings stuff to support multiple "settings sets" to act like ini file sections kind of, so that we can hand a generic settings object to cores instead of strictly ones defined by the cores
		/// </summary>
		public CorePreferencesFlags CorePreferences { get; }

		/// <summary>
		/// Interface to provide OpenGL resources to the core
		/// </summary>
		public IOpenGLProvider OpenGLProvider { get; }
	}
}
