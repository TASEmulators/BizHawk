using System;
using System.Reflection;

namespace BizHawk.Client.ApiHawk
{
	/// <summary>
	/// This class contains some methods that
	/// interract with BizHawk client
	/// </summary>
	public static class ClientApi
	{
		#region Fields

		private static readonly Assembly clientAssembly;
		
		public static event EventHandler RomLoaded;

		#endregion

		#region cTor(s)

		static ClientApi()
		{
			clientAssembly = Assembly.GetEntryAssembly();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Raise when a rom is successfully Loaded
		/// </summary>
		public static void OnRomLoaded()
		{
			if(RomLoaded != null)
			{
				RomLoaded(null, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Sets the extra padding added to the 'native' surface so that you can draw HUD elements in predictable placements
		/// </summary>
		/// <param name="left">Left padding</param>
		/// <param name="top">Top padding</param>
		/// <param name="right">Right padding</param>
		/// <param name="bottom">Bottom padding</param>
		public static void SetExtraPadding(int left, int top, int right, int bottom)
		{
			Type emuLuaLib = clientAssembly.GetType("BizHawk.Client.EmuHawk.EmuHawkLuaLibrary");
			MethodInfo paddingMethod = emuLuaLib.GetMethod("SetClientExtraPadding");
			paddingMethod.Invoke(paddingMethod, new object[] { left, top, right, bottom });
		}

		/// <summary>
		/// Sets the extra padding added to the 'native' surface so that you can draw HUD elements in predictable placements
		/// </summary>
		/// <param name="left">Left padding</param>
		public static void SetExtraPadding(int left)
		{
			SetExtraPadding(left, 0, 0, 0);
		}

		/// <summary>
		/// Sets the extra padding added to the 'native' surface so that you can draw HUD elements in predictable placements
		/// </summary>
		/// <param name="left">Left padding</param>
		/// <param name="top">Top padding</param>
		public static void SetExtraPadding(int left, int top)
		{
			SetExtraPadding(left, top, 0, 0);
		}

		/// <summary>
		/// Sets the extra padding added to the 'native' surface so that you can draw HUD elements in predictable placements
		/// </summary>
		/// <param name="left">Left padding</param>
		/// <param name="top">Top padding</param>
		/// <param name="right">Right padding</param>
		public static void SetExtraPadding(int left, int top, int right)
		{
			SetExtraPadding(left, top, right, 0);
		}

		#endregion
	}
}
