using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public interface IMainFormForConfig : IDialogController, IDialogParent
	{
		/// <remarks>only referenced from <see cref="GenericCoreConfig"/></remarks>
		IEmulator Emulator { get; }

		IMovieSession MovieSession { get; }

		void AddOnScreenMessage(string message);

		void PutCoreSettings(object o);

		void PutCoreSyncSettings(object o);
	}
}
