using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Sony.PSX
{
	/// <summary>
	/// Represents a user's view of what equipment is plugged into the PSX FIO
	/// </summary>
	public class OctoshockFIOConfigUser
	{
		public bool[] Multitaps = new bool[2];
		public bool[] Memcards = new bool[2];
		public OctoshockDll.ePeripheralType[] Devices8 = new OctoshockDll.ePeripheralType[8];

		public OctoshockFIOConfigLogical ToLogical()
		{
			var lc = new OctoshockFIOConfigLogical();
			lc.PopulateFrom(this);
			return lc;
		}
	}

	/// <summary>
	/// Represents a baked-down view of what's plugged into the PSX FIO.
	/// But really, users are interested in it too (its what produces the player number assignments)
	/// </summary>
	public class OctoshockFIOConfigLogical
	{
		public bool[] Multitaps;
		public bool[] Memcards;
		public OctoshockDll.ePeripheralType[] Devices8;

		/// <summary>
		/// Total number of players defined
		/// </summary>
		public int NumPlayers;

		/// <summary>
		/// The player number on each of the input slots
		/// </summary>
		public int[] PlayerAssignments = new int[8];

		/// <summary>
		/// The device type associated with each player
		/// </summary>
		public OctoshockDll.ePeripheralType[] DevicesPlayer = new OctoshockDll.ePeripheralType[8];

		/// <summary>
		/// Total number of connected memcards
		/// </summary>
		public int NumMemcards { get { return (Memcards[0] ? 1 : 0) + (Memcards[1] ? 1 : 0); } }

		internal void PopulateFrom(OctoshockFIOConfigUser userConfig)
		{
			Multitaps = (bool[])userConfig.Multitaps.Clone();
			Memcards = (bool[])userConfig.Memcards.Clone();
			Devices8 = (OctoshockDll.ePeripheralType[])userConfig.Devices8.Clone();

			int id = 1;

			if (userConfig.Devices8[0] == OctoshockDll.ePeripheralType.None) PlayerAssignments[0] = -1; else PlayerAssignments[0] = id++;
			if (userConfig.Devices8[1] == OctoshockDll.ePeripheralType.None || !userConfig.Multitaps[0]) PlayerAssignments[1] = -1; else PlayerAssignments[1] = id++;
			if (userConfig.Devices8[2] == OctoshockDll.ePeripheralType.None || !userConfig.Multitaps[0]) PlayerAssignments[2] = -1; else PlayerAssignments[2] = id++;
			if (userConfig.Devices8[3] == OctoshockDll.ePeripheralType.None || !userConfig.Multitaps[0]) PlayerAssignments[3] = -1; else PlayerAssignments[3] = id++;

			if (userConfig.Devices8[4] == OctoshockDll.ePeripheralType.None) PlayerAssignments[4] = -1; else PlayerAssignments[4] = id++;
			if (userConfig.Devices8[5] == OctoshockDll.ePeripheralType.None || !userConfig.Multitaps[1]) PlayerAssignments[5] = -1; else PlayerAssignments[5] = id++;
			if (userConfig.Devices8[6] == OctoshockDll.ePeripheralType.None || !userConfig.Multitaps[1]) PlayerAssignments[6] = -1; else PlayerAssignments[6] = id++;
			if (userConfig.Devices8[7] == OctoshockDll.ePeripheralType.None || !userConfig.Multitaps[1]) PlayerAssignments[7] = -1; else PlayerAssignments[7] = id++;

			NumPlayers = id - 1;

			for (int i = 0; i < 8; i++)
			{
				int pnum = i+1;
				for (int j = 0; j < 8; j++)
				{
					if(PlayerAssignments[j] == pnum)
						DevicesPlayer[i] = userConfig.Devices8[j];
				}
			}
		}
	}

}