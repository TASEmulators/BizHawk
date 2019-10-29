using System;
using System.Linq;
using System.Reflection;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This service provides mechanism for the client to set sync and non-sync related settings to the core
	/// Settings are settings that can change during the lifetime of the core and do not affect potential movie sync
	/// Sync Settings do not change during the lifetime of the core and affect movie sync
	/// If available, Sync settings are stored in movie files and automatically applied when the movie is loaded
	/// If this service is available the client can provide UI for the user to manage these settings
	/// </summary>
	/// <typeparam name="TSettings">The Type of the object that represent regular settings (settings that can be changed during the lifespan of a core instance</typeparam>
	/// <typeparam name="TSync">The Type of the object that represents sync settings (settings that can not change during the lifespan of the core and are required for movie sync</typeparam>
	public interface ISettable<TSettings, TSync> : IEmulatorService
	{
		// in addition to these methods, it's expected that the constructor or Load() method
		// will take a Settings and SyncSettings object to set the initial state of the core
		// (if those are null, default settings are to be used)

		/// <summary>
		/// get the current core settings, excepting movie settings.  should be a clone of the active in-core object.
		/// VERY IMPORTANT: changes to the object returned by this function SHOULD NOT have any effect on emulation
		/// (unless the object is later passed to PutSettings)
		/// </summary>
		/// <returns>a JSON serializable object</returns>
		TSettings GetSettings();

		/// <summary>
		/// get the current core settings that affect movie sync.  these go in movie 2.0 files, so don't make the JSON too extravagant, please
		/// should be a clone of the active in-core object.
		/// VERY IMPORTANT: changes to the object returned by this function MUST NOT have any effect on emulation
		/// (unless the object is later passed to PutSyncSettings)
		/// </summary>
		/// <returns>a JSON serializable object</returns>
		TSync GetSyncSettings();

		/// <summary>
		/// change the core settings, excepting movie settings
		/// </summary>
		/// <param name="o">an object of the same type as the return for GetSettings</param>
		/// <returns>true if a core reboot will be required to make the changes effective</returns>
		bool PutSettings(TSettings o);

		/// <summary>
		/// changes the movie-sync relevant settings.  THIS SHOULD NEVER BE CALLED WHILE RECORDING
		/// if it is called while recording, the core need not guarantee continued determinism
		/// </summary>
		/// <param name="o">an object of the same type as the return for GetSyncSettings</param>
		/// <returns>true if a core reboot will be required to make the changes effective</returns>
		bool PutSyncSettings(TSync o);
	}

	/// <summary>
	/// serves as a shim between strongly typed ISettable and consumers
	/// </summary>
	public class SettingsAdapter
	{
		public SettingsAdapter(IEmulator e)
		{
			_emu = e;

			Type impl = e.GetType().GetInterfaces()
				.FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ISettable<,>));
			if (impl == null)
			{
				HasSettings = false;
				HasSyncSettings = false;
			}
			else
			{
				var tt = impl.GetGenericArguments();
				var settingType = tt[0];
				var syncType = tt[1];
				HasSettings = settingType != typeof(object); // object is used for a placeholder where an emu doesn't have both s and ss
				HasSyncSettings = syncType != typeof(object);

				if (HasSettings)
				{
					_gets = impl.GetMethod("GetSettings");
					_puts = impl.GetMethod("PutSettings");
				}

				if (HasSyncSettings)
				{
					_getss = impl.GetMethod("GetSyncSettings");
					_putss = impl.GetMethod("PutSyncSettings");
				}
			}
		}

		private readonly IEmulator _emu;

		public bool HasSettings { get; }
		public bool HasSyncSettings { get; }

		private readonly object[] _tmp1 = new object[1];
		private readonly object[] _tmp0 = new object[0];

		private readonly MethodInfo _gets;
		private readonly MethodInfo _puts;
		private readonly MethodInfo _getss;
		private readonly MethodInfo _putss;

		public object GetSettings()
		{
			if (!HasSettings)
			{
				throw new InvalidOperationException();
			}

			return _gets.Invoke(_emu, _tmp0);
		}

		public object GetSyncSettings()
		{
			if (!HasSyncSettings)
			{
				throw new InvalidOperationException();
			}

			return _getss.Invoke(_emu, _tmp0);
		}

		public bool PutSettings(object o)
		{
			if (!HasSettings)
			{
				throw new InvalidOperationException();
			}

			_tmp1[0] = o;
			return (bool)_puts.Invoke(_emu, _tmp1);
		}

		public bool PutSyncSettings(object o)
		{
			if (!HasSyncSettings)
			{
				throw new InvalidOperationException();
			}

			_tmp1[0] = o;
			return (bool)_putss.Invoke(_emu, _tmp1);
		}
	}
}
