using System;

namespace BizHawk.Client.ApiHawk.Classes.Events
{
	/// <summary>
	/// This class holds event data for BeforeQuickSave event
	/// </summary>
	public sealed class BeforeQuickSaveEventArgs : EventArgs
	{
		internal BeforeQuickSaveEventArgs(string name)
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
}
