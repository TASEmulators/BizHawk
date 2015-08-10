using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Sony.PSX
{
	public class OctoshockControlUserConfig
	{
		public bool[] Multitaps = new bool[2];
		public OctoshockDll.ePeripheralType[] Devices8 = new OctoshockDll.ePeripheralType[8];

		public OctoshockControlLogicalConfig ToLogicalConfig()
		{
			var lc = new OctoshockControlLogicalConfig();
			lc.PopulateFrom(this);
			return lc;
		}
	}

	public class OctoshockControlLogicalConfig
	{
		public int[] PlayerAssignments = new int[8];
		public bool[] Multitaps;
		public OctoshockDll.ePeripheralType[] Devices8;

		internal void PopulateFrom(OctoshockControlUserConfig userConfig)
		{
			Multitaps = (bool[])userConfig.Multitaps.Clone();
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
		}
	}

}