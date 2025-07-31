using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace BizHawk.Client.Common
{
	public interface IConfigPersist
	{
		public class Provider
		{
			private Dictionary<string, object> _data;

			public Provider(Dictionary<string, object> data)
			{
				_data = data;
			}

			public bool Get<T>(string name, ref T value)
			{
				if (_data.TryGetValue(name, out var val))
				{
					if (val is string str && typeof(T) != typeof(string))
					{
						// if a type has a TypeConverter, and that converter can convert to string,
						// that will be used in place of object markup by JSON.NET

						// but that doesn't work with $type metadata, and JSON.NET fails to fall
						// back on regular object serialization when needed.  so try to undo a TypeConverter
						// operation here
						var converter = TypeDescriptor.GetConverter(typeof(T));
						val = converter.ConvertFromString(null, CultureInfo.InvariantCulture, str);
					}
					else if (val is not bool && typeof(T).IsPrimitive)
					{
						// numeric constants are similarly hosed
						val = Convert.ChangeType(val, typeof(T), CultureInfo.InvariantCulture);
					}

					value = (T)val;
					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Load the provided configuration.
		/// </summary>
		void LoadConfig(Provider provider);

		/// <summary>
		/// Return a dictionary representing the current configuration.
		/// </summary>
		Dictionary<string, object> SaveConfig();
	}
}
