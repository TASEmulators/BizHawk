using System;

namespace BizHawk.Client.ApiHawk.Classes.Events
{
	/// <summary>
	/// This class holds event data for StateSaved event
	/// </summary>
	public sealed class StateSavedEventArgs : EventArgs
	{
		#region Fields

		string _Name;

		#endregion

		#region cTor(s)

		/// <summary>
		/// Initialize a new instance of <see cref="StateSavedEventArgs"/>
		/// </summary>
		/// <param name="stateName">User friendly name of loaded state</param>
		internal StateSavedEventArgs(string stateName)
		{
			_Name = stateName;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets user friendly name of the loaded savestate
		/// </summary>
		public string Name
		{
			get
			{
				return _Name;
			}
		}

		#endregion
	}
}
