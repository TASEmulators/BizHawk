using System.Drawing;

namespace BizHawk.Client.Common
{
	public abstract class PluginLibraryBase
	{
		protected PluginLibraryBase() { }
		public abstract string Name { get; }

		protected static Color? ToColor(object o)
		{
			if (o == null)
			{
				return null;
			}

			if (o.GetType() == typeof(double))
			{
				return Color.FromArgb((int)(long)(double)o);
			}

			if (o.GetType() == typeof(string))
			{
				return Color.FromName(o.ToString());
			}

			return null;
		}
	}
}
