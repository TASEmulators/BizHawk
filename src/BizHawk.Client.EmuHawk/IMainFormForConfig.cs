using System;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public interface IMainFormForConfig : IDialogParent
	{
		/// <exception cref="InvalidOperationException">loaded emulator is not instance of <typeparamref name="T"/></exception>
		SettingsAdapter GetSettingsAdapterForLoadedCore<T>()
			where T : IEmulator;
	}
}
