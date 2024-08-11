namespace BizHawk.Client.Common
{
	/// <summary>
	/// This class holds event data for BeforeQuickLoad event
	/// </summary>
	public sealed class BeforeQuickLoadEventArgs : EventArgs
	{
		public BeforeQuickLoadEventArgs(string name)
		{
			Name = name;
		}

		/// <summary>
		/// Gets or sets value that defined if saved has been handled or not
		/// </summary>
		public bool Handled { get; set; }


		/// <summary>
		/// Gets quicksave name
		/// </summary>
		public string Name { get; }


		/// <summary>
		/// Gets slot used for quicksave
		/// </summary>
		public int Slot => int.Parse(Name.Substring(Name.Length - 1));
	}

	/// <summary>
	/// This class holds event data for BeforeQuickSave event
	/// </summary>
	public sealed class BeforeQuickSaveEventArgs : EventArgs
	{
		public BeforeQuickSaveEventArgs(string name)
		{
			Name = name;
		}

		/// <summary>
		/// Gets or sets value that defined if saved has been handled or not
		/// </summary>
		public bool Handled { get; set; }

		/// <summary>
		/// Gets quicksave name
		/// </summary>
		public string Name { get; }


		/// <summary>
		/// Gets slot used for quicksave
		/// </summary>
		public int Slot => int.Parse(Name.Substring(Name.Length - 1));
	}

	/// <summary>
	/// This class holds event data for StateLoaded event
	/// </summary>
	public sealed class StateLoadedEventArgs: EventArgs
	{
		/// <summary>
		/// Initialize a new instance of <see cref="StateLoadedEventArgs"/>
		/// </summary>
		/// <param name="stateName">User friendly name of loaded state</param>
		public StateLoadedEventArgs(string stateName)
		{
			Name = stateName;
		}

		/// <summary>
		/// Gets user friendly name of the loaded savestate
		/// </summary>
		public string Name { get; }
	}

	/// <summary>
	/// This class holds event data for StateSaved event
	/// </summary>
	public sealed class StateSavedEventArgs : EventArgs
	{
		/// <summary>
		/// Initialize a new instance of <see cref="StateSavedEventArgs"/>
		/// </summary>
		/// <param name="stateName">User friendly name of loaded state</param>
		public StateSavedEventArgs(string stateName)
		{
			Name = stateName;
		}

		/// <summary>
		/// Gets user friendly name of the loaded savestate
		/// </summary>
		public string Name { get; }
	}

	/// <summary>
	/// Represent a method that will handle the event raised before a quickload is done
	/// </summary>
	/// <param name="sender">Object that raised the event</param>
	/// <param name="e">Event arguments</param>
	public delegate void BeforeQuickLoadEventHandler(object sender, BeforeQuickLoadEventArgs e);

	/// <summary>
	/// Represent a method that will handle the event raised before a quicksave is done
	/// </summary>
	/// <param name="sender">Object that raised the event</param>
	/// <param name="e">Event arguments</param>
	public delegate void BeforeQuickSaveEventHandler(object sender, BeforeQuickSaveEventArgs e);

	/// <summary>
	/// Represent a method that will handle the event raised when a savestate is loaded
	/// </summary>
	/// <param name="sender">Object that raised the event</param>
	/// <param name="e">Event arguments</param>
	public delegate void StateLoadedEventHandler(object sender, StateLoadedEventArgs e);

	/// <summary>
	/// Represent a method that will handle the event raised when a savestate is saved
	/// </summary>
	/// <param name="sender">Object that raised the event</param>
	/// <param name="e">Event arguments</param>
	public delegate void StateSavedEventHandler(object sender, StateSavedEventArgs e);
}
