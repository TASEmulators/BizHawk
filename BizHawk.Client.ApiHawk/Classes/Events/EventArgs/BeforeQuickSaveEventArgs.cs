using System;

namespace BizHawk.Client.ApiHawk.Classes.Events
{
	/// <summary>
	/// This class holds event data for BeforeQuickSave event
	/// </summary>
	public sealed class BeforeQuickSaveEventArgs : EventArgs
	{
		#region Fields

		private bool _Handled = false;
		private string _QuickSaveSlotName;

		#endregion

		#region cTor(s)

		internal BeforeQuickSaveEventArgs(string name)
		{
			_QuickSaveSlotName = name;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets value that defined if saved has been handled or not
		/// </summary>
		public bool Handled
		{
			get
			{
				return _Handled;
			}
			set
			{
				_Handled = value;
			}
		}


		/// <summary>
		/// Gets quicksave name
		/// </summary>
		public string Name
		{
			get
			{
				return _QuickSaveSlotName;
			}
		}


		/// <summary>
		/// Gets slot used for quicksave
		/// </summary>
		public int Slot
		{
			get
			{
				return int.Parse(_QuickSaveSlotName.Substring(_QuickSaveSlotName.Length - 1));
			}
		}

		#endregion
	}
}
