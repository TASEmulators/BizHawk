using System;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This object facilitates communications between client and core
	/// The primary use is to provide a client => core communication, such as providing client-side callbacks for a core to use
	/// Any communications that can be described as purely a Core -> Client system, should be provided as an <seealso cref="IEmulatorService"/> instead
	/// </summary>
	public class CoreComm
	{
		public CoreComm(
			Action<string> showMessage,
			Action<string> notifyMessage,
			ICoreFileProvider coreFileProvider)
		{
			ShowMessage = showMessage;
			Notify = notifyMessage;
			CoreFileProvider = coreFileProvider;
		}

		public CoreComm Clone() => (CoreComm)MemberwiseClone();

		public ICoreFileProvider CoreFileProvider { get; }

		/// <summary>
		/// Gets a message to show. reasonably annoying (dialog box), shouldn't be used most of the time
		/// </summary>
		public Action<string> ShowMessage { get; }

		/// <summary>
		/// Gets a message to show. less annoying (OSD message). Should be used for ignorable helpful messages
		/// </summary>
		public Action<string> Notify { get; }
	}
}
