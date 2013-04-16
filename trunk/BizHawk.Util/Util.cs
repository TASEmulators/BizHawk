using System;
using System.Windows.Forms;

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