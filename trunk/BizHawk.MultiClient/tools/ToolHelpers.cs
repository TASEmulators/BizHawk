using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	class ToolHelpers
	{
		public static ToolStripMenuItem[] GenerateMemoryDomainMenuItems(Action<int> SetCallback, string SelectedDomain = "", int? maxSize = null)
		{
			var items = new List<ToolStripMenuItem>();

			if (Global.Emulator.MemoryDomains.Any())
			{
				int counter = 0;
				foreach (var domain in Global.Emulator.MemoryDomains)
				{
					string temp = domain.ToString();
					var item = new ToolStripMenuItem { Text = temp };

					int index = counter;
					item.Click += (o, ev) => SetCallback(index);

					if (temp == SelectedDomain)
					{
						item.Checked = true;
					}

					if (maxSize.HasValue && domain.Size > maxSize.Value)
					{
						item.Enabled = false;
					}

					items.Add(item);
					counter++;
				}
			}

			return items.ToArray();
		}

		public static void UnfreezeAll()
		{
			Global.MainForm.Cheats1.RemoveAllCheats();

			Global.MainForm.RamWatch1.UpdateValues();
			Global.MainForm.HexEditor1.UpdateValues();
			Global.MainForm.Cheats_UpdateValues();
			Global.MainForm.RamSearch1.UpdateValues();
		}

		public static void FreezeAddress(List<Watch> watches)
		{
			foreach(var watch in watches)
			{
				switch (watch.Size)
				{
					case Watch.WatchSize.Byte:
						Cheat c = new Cheat("", watch.Address.Value, (byte)watch.Value.Value,
							true, watch.Domain);
						Global.MainForm.Cheats1.AddCheat(c);
						break;
					case Watch.WatchSize.Word:
						{
							byte low = (byte)(watch.Value.Value / 256);
							byte high = (byte)(watch.Value.Value);
							int a1 = watch.Address.Value;
							int a2 = watch.Address.Value + 1;
							if (watch.BigEndian)
							{
								Cheat c1 = new Cheat("", a1, low, true, watch.Domain);
								Cheat c2 = new Cheat("", a2, high, true, watch.Domain);
								Global.MainForm.Cheats1.AddCheat(c1);
								Global.MainForm.Cheats1.AddCheat(c2);
							}
							else
							{
								Cheat c1 = new Cheat("", a1, high, true, watch.Domain);
								Cheat c2 = new Cheat("", a2, low, true, watch.Domain);
								Global.MainForm.Cheats1.AddCheat(c1);
								Global.MainForm.Cheats1.AddCheat(c2);
							}
						}
						break;
					case Watch.WatchSize.DWord:
						{
							byte HIWORDhigh = (byte)(watch.Value.Value >> 24);
							byte HIWORDlow = (byte)(watch.Value.Value >> 16);
							byte LOWORDhigh = (byte)(watch.Value.Value >> 8);
							byte LOWORDlow = (byte)(watch.Value.Value);
							int a1 = watch.Address.Value;
							int a2 = watch.Address.Value + 1;
							int a3 = watch.Address.Value + 2;
							int a4 = watch.Address.Value + 3;
							if (watch.BigEndian)
							{
								Cheat c1 = new Cheat("", a1, HIWORDhigh, true, watch.Domain);
								Cheat c2 = new Cheat("", a2, HIWORDlow, true, watch.Domain);
								Cheat c3 = new Cheat("", a3, LOWORDhigh, true, watch.Domain);
								Cheat c4 = new Cheat("", a4, LOWORDlow, true, watch.Domain);
								Global.MainForm.Cheats1.AddCheat(c1);
								Global.MainForm.Cheats1.AddCheat(c2);
								Global.MainForm.Cheats1.AddCheat(c3);
								Global.MainForm.Cheats1.AddCheat(c4);
							}
							else
							{
								Cheat c1 = new Cheat("", a1, LOWORDlow, true, watch.Domain);
								Cheat c2 = new Cheat("", a2, LOWORDhigh, true, watch.Domain);
								Cheat c3 = new Cheat("", a3, HIWORDlow, true, watch.Domain);
								Cheat c4 = new Cheat("", a4, HIWORDhigh, true, watch.Domain);
								Global.MainForm.Cheats1.AddCheat(c1);
								Global.MainForm.Cheats1.AddCheat(c2);
								Global.MainForm.Cheats1.AddCheat(c3);
								Global.MainForm.Cheats1.AddCheat(c4);
							}
						}
						break;
				}
			}

			Global.MainForm.RamWatch1.UpdateValues();
			Global.MainForm.HexEditor1.UpdateValues();
			Global.MainForm.Cheats_UpdateValues();
		}

		public static void ViewInHexEditor(MemoryDomain domain, IEnumerable<int> addresses)
		{
			Global.MainForm.LoadHexEditor();
			Global.MainForm.HexEditor1.SetDomain(domain);
			Global.MainForm.HexEditor1.SetToAddresses(addresses.ToList());
		}

		public static MemoryDomain DomainByName(string name)
		{
			//Attempts to find the memory domain by name, if it fails, it defaults to index 0
			foreach (MemoryDomain domain in Global.Emulator.MemoryDomains)
			{
				if (domain.Name == name)
				{
					return domain;
				}
			}

			return Global.Emulator.MainMemory;
		}
	}
}
