using System;

namespace BizHawk.Client.ApiHawk.Classes.Events
{
	/// <summary>
	/// This class holds event data for StateLoaded event
	/// </summary>
	public sealed class StateLoadedEventArgs: EventArgs
	{
		#region Fields

		string _Name;

		#endregion

		#region cTor(s)

		/// <summary>
		/// Initialize a new instance of <see cref="StateLoadedEventArgs"/>
		/// </summary>
		/// <param name="stateName">User friendly name of loaded state</param>
		internal StateLoadedEventArgs(string stateName)
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
