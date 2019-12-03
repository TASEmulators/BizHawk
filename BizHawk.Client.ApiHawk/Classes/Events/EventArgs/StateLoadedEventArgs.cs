using System;

namespace BizHawk.Client.ApiHawk.Classes.Events
{
	/// <summary>
	/// This class holds event data for StateLoaded event
	/// </summary>
	public sealed class StateLoadedEventArgs: EventArgs
	{
		/// <summary>
		/// Initialize a new instance of <see cref="StateLoadedEventArgs"/>
		/// </summary>
		/// <param name="stateName">User friendly name of loaded state</param>
		internal StateLoadedEventArgs(string stateName)
		{
			Name = stateName;
		}

		/// <summary>
		/// Gets user friendly name of the loaded savestate
		/// </summary>
		public string Name { get; }
	}
}
