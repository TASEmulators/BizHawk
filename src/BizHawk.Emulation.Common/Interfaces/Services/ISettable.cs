#nullable disable

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
		where TSettings : class, new()
		where TSync : class, new()
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
		PutSettingsDirtyBits PutSettings(TSettings o);

		/// <summary>
		/// changes the movie-sync relevant settings.  THIS SHOULD NEVER BE CALLED WHILE RECORDING
		/// if it is called while recording, the core need not guarantee continued determinism
		/// </summary>
		/// <param name="o">an object of the same type as the return for GetSyncSettings</param>
		/// <returns>true if a core reboot will be required to make the changes effective</returns>
		PutSettingsDirtyBits PutSyncSettings(TSync o);
	}

	/// <summary>
	/// Place this attribute for TSettings and TSync classes which use System.ComponentModel.DefaultValue
	/// Classes with this attribute will have a BizHawk.Common.SettingsUtil.SetDefaultValues(T) function generated
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class CoreSettingsAttribute : Attribute {}

	//note: this is a bit of a frail API. If a frontend wants a new flag, cores won't know to yea or nay it
	//this could be solved by adding a KnownSettingsDirtyBits on the settings interface
	//or, in a pinch, the same thing could be done with THESE flags, so that the interface doesn't
	//change but newly-aware cores can simply manifest that they know about more bits, in the same variable they return the bits in
	[Flags]
	public enum PutSettingsDirtyBits
	{
		None = 0,
		RebootCore = 1,
		ScreenLayoutChanged = 2,
	}

	public interface ISettingsAdapter
	{
		bool HasSettings { get; }

		bool HasSyncSettings { get; }

		/// <exception cref="InvalidOperationException">does not have non-sync settings</exception>
		object GetSettings();

		/// <exception cref="InvalidOperationException">does not have sync settings</exception>
		object GetSyncSettings();

		void PutCoreSettings(object s);

		void PutCoreSyncSettings(object ss);
	}

	/// <summary>
	/// serves as a shim between strongly typed ISettable and consumers
	/// </summary>
	public sealed class SettingsAdapter : ISettingsAdapter
	{
		private readonly Action<PutSettingsDirtyBits> _handlePutCoreSettings;

		private readonly Action<PutSettingsDirtyBits> _handlePutCoreSyncSettings;

		private readonly Func<bool> _mayPutCoreSettings;

		private readonly Func<bool> _mayPutCoreSyncSettings;

		public SettingsAdapter(
			IEmulator emulator,
			Func<bool> mayPutCoreSettings,
			Action<PutSettingsDirtyBits> handlePutCoreSettings,
			Func<bool> mayPutCoreSyncSettings,
			Action<PutSettingsDirtyBits> handlePutCoreSyncSettings)
		{
			_handlePutCoreSettings = handlePutCoreSettings;
			_handlePutCoreSyncSettings = handlePutCoreSyncSettings;
			_mayPutCoreSettings = mayPutCoreSettings;
			_mayPutCoreSyncSettings = mayPutCoreSyncSettings;

			var settableType = emulator.ServiceProvider.AvailableServices
				.SingleOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ISettable<,>));
			if (settableType == null)
			{
				HasSettings = false;
				HasSyncSettings = false;
			}
			else
			{
				var tt = settableType.GetGenericArguments();
				var settingType = tt[0];
				var syncType = tt[1];
				HasSettings = settingType != typeof(object); // object is used for a placeholder where an emu doesn't have both s and ss
				HasSyncSettings = syncType != typeof(object);

				if (HasSettings)
				{
					_gets = settableType.GetMethod("GetSettings");
					_puts = settableType.GetMethod("PutSettings");
				}

				if (HasSyncSettings)
				{
					_getss = settableType.GetMethod("GetSyncSettings");
					_putss = settableType.GetMethod("PutSyncSettings");
				}

				_settable = emulator.ServiceProvider.GetService(settableType);
			}
		}

		private readonly object _settable;

		public bool HasSettings { get; }
		public bool HasSyncSettings { get; }

		private readonly object[] _tempObject = new object[1];
		private static readonly object[] Empty = Array.Empty<object>();

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

			return _gets.Invoke(_settable, Empty);
		}

		public object GetSyncSettings()
		{
			if (!HasSyncSettings)
			{
				throw new InvalidOperationException();
			}

			return _getss.Invoke(_settable, Empty);
		}

		public void PutCoreSettings(object s)
		{
			if (HasSettings && _mayPutCoreSettings()) _handlePutCoreSettings(DoPutSettings(s));
		}

		public void PutCoreSyncSettings(object ss)
		{
			if (HasSyncSettings && _mayPutCoreSyncSettings()) _handlePutCoreSyncSettings(DoPutSyncSettings(ss));
		}

		private PutSettingsDirtyBits DoPutSettings(object o)
		{
			_tempObject[0] = o;
			return (PutSettingsDirtyBits)_puts.Invoke(_settable, _tempObject);
		}

		private PutSettingsDirtyBits DoPutSyncSettings(object o)
		{
			_tempObject[0] = o;
			return (PutSettingsDirtyBits)_putss.Invoke(_settable, _tempObject);
		}
	}
}
