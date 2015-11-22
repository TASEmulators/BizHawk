using System;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Interface to implements in order to make a custom tool
	/// </summary>
	public interface IExternalToolForm : IToolForm
	{
		/// <summary>
		/// <see cref="FormClosedEventHandler"/>
		/// </summary>
		event FormClosedEventHandler FormClosed;
	}
}
