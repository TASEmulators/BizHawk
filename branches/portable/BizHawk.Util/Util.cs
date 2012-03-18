using System;
using System.Windows.Forms;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace BizHawk
{
	public static class Extensions
	{
		//extension method to make Control.Invoke easier to use
		public static void Invoke(this Control control, Action action)
		{
			control.Invoke((Delegate)action);
		}
	}
}