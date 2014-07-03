using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Common.ReflectionExtensions;

namespace BizHawk.Client.EmuHawk.ControlExtensions
{
	public static class ControlExtensions
	{
		public static void PopulateFromEnum<T>(this ComboBox box, object enumVal)
			where T : struct, IConvertible
		{
			if (!typeof(T).IsEnum)
			{
				throw new ArgumentException("T must be an enumerated type");
			}

			box.Items.Clear();
			box.Items.AddRange(
				typeof(T).GetEnumDescriptions()
				.ToArray());
			box.SelectedItem = enumVal.GetDescription();
		}
	}
}
