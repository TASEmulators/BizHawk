using System;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public interface IMainFormForConfig : IDialogParent
	{
		/// <remarks>only referenced from <see cref="GenericCoreConfig"/></remarks>
		IEmulator Emulator { get; }

		/// <exception cref="InvalidOperationException">loaded emulator is not instance of <typeparamref name="T"/></exception>
		SettingsAdapter GetSettingsAdapterForLoadedCore<T>()
			where T : IEmulator;

		void PutCoreSettings(object o, SettingsAdapter settable);

		void PutCoreSyncSettings(object o, SettingsAdapter settable);
	}
}
