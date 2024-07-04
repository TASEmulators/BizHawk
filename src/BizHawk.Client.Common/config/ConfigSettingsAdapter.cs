#nullable enable

using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public sealed class ConfigSettingsAdapter<T> : ISettingsAdapter
		where T : IEmulator
	{
		private readonly Config _config;

		private readonly Type _typeS;

		private readonly Type _typeSS;

		public bool HasSettings { get; }

		public bool HasSyncSettings { get; }

		public ConfigSettingsAdapter(Config config)
		{
			_config = config;
			var settableType = typeof(T).GetInterfaces()
				.SingleOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ISettable<,>));
			if (settableType == null)
			{
				_typeS = typeof(object);
				_typeSS = typeof(object);
			}
			else
			{
				var tt = settableType.GetGenericArguments();
				_typeS = tt[0];
				_typeSS = tt[1];
			}
			HasSettings = _typeS != typeof(object);
			HasSyncSettings = _typeSS != typeof(object);
		}

		public object GetSettings()
			=> _config.GetCoreSettings(typeof(T), _typeS)
				?? Activator.CreateInstance(_typeS);

		public object GetSyncSettings()
			=> _config.GetCoreSyncSettings(typeof(T), _typeSS)
				?? Activator.CreateInstance(_typeSS);

		public void PutCoreSettings(object s)
			=> _config.PutCoreSettings(s, typeof(T));

		public void PutCoreSyncSettings(object ss)
			=> _config.PutCoreSyncSettings(ss, typeof(T));
	}
}
