using System;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public interface IMainFormForConfig : IDialogParent
	{
		/// <exception cref="InvalidOperationException">loaded emulator is not instance of <typeparamref name="T"/></exception>
		ISettingsAdapter GetSettingsAdapterForLoadedCore<T>()
			where T : IEmulator;

		ISettingsAdapter GetSettingsAdapterFor<T>()
			where T : IEmulator;
	}
}
