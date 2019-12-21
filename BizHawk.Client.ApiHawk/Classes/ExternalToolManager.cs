using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;

using System.Windows.Forms;
using BizHawk.Client.Common;

namespace BizHawk.Client.ApiHawk
{
	/// <summary>
	/// This static class handle all ExternalTools
	/// </summary>
	public static class ExternalToolManager
	{
		#region Fields

		private static readonly FileSystemWatcher DirectoryMonitor;
		private static readonly List<ToolStripMenuItem> MenuItems = new List<ToolStripMenuItem>();

		#endregion

		#region cTor(s)

		/// <summary>
		/// Initialization
		/// </summary>
		static ExternalToolManager()
		{
			if(!Directory.Exists(Global.Config.PathEntries["Global", "External Tools"].Path))
			{
				Directory.CreateDirectory(Global.Config.PathEntries["Global", "External Tools"].Path);
			}

			DirectoryMonitor = new FileSystemWatcher(Global.Config.PathEntries["Global", "External Tools"].Path, "*.dll")
			{
				IncludeSubdirectories = false
				, NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName
				, Filter = "*.dll"
			};
			DirectoryMonitor.Created += DirectoryMonitor_Created;
			DirectoryMonitor.EnableRaisingEvents = true;

			ClientApi.RomLoaded += delegate { BuildToolStrip(); };

			BuildToolStrip();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Build the ToolStrip menu
		/// </summary>
		private static void BuildToolStrip()
		{
			MenuItems.Clear();
			if (Directory.Exists(DirectoryMonitor.Path))
			{
				DirectoryInfo dInfo = new DirectoryInfo(DirectoryMonitor.Path);

				foreach (FileInfo fi in dInfo.GetFiles("*.dll"))
				{
					MenuItems.Add(GenerateToolTipFromFileName(fi.FullName));
				}
			}
		}

		/// <summary>
		/// Generate a <see cref="ToolStripMenuItem"/> from an
		/// external tool dll.
		/// The assembly must have <see cref="BizHawkExternalToolAttribute"/> in its
		/// assembly attributes
		/// </summary>
		/// <param name="fileName">File that will be reflected</param>
		/// <returns>A new <see cref="ToolStripMenuItem"/>; assembly path can be found in the Tag property</returns>
		/// <remarks>For the moment, you could only load a dll that have a form (which implements <see cref="BizHawk.Client.EmuHawk.IExternalToolForm"/>)</remarks>
		private static ToolStripMenuItem GenerateToolTipFromFileName(string fileName)
		{
			ToolStripMenuItem item = null;

			try
			{
				BizHawk.Common.MotWHack.RemoveMOTW(fileName);
				var externalToolFile = Assembly.LoadFrom(fileName);
				object[] attributes = externalToolFile.GetCustomAttributes(typeof(BizHawkExternalToolAttribute), false);
				if (attributes != null && attributes.Count() == 1)
				{
					BizHawkExternalToolAttribute attribute = (BizHawkExternalToolAttribute)attributes[0];
					item = new ToolStripMenuItem(attribute.Name) { ToolTipText = attribute.Description };
					if (attribute.IconResourceName != "")
					{
						Stream s = externalToolFile.GetManifestResourceStream($"{externalToolFile.GetName().Name}.{attribute.IconResourceName}");
						if (s != null)
						{
							item.Image = new Bitmap(s);
						}
					}

					var customFormType = externalToolFile.GetTypes().FirstOrDefault(t => t != null && t.FullName == "BizHawk.Client.EmuHawk.CustomMainForm");
					if (customFormType == null)
					{
						item.ToolTipText = "Does not have a CustomMainForm";
						item.Enabled = false;
					}
					item.Tag = fileName;

					attributes = externalToolFile.GetCustomAttributes(typeof(BizHawkExternalToolUsageAttribute), false);
					if (attributes != null && attributes.Length == 1)
					{
						BizHawkExternalToolUsageAttribute attribute2 = (BizHawkExternalToolUsageAttribute)attributes[0];
						if(Global.Emulator.SystemId == "NULL" && attribute2.ToolUsage != BizHawkExternalToolUsage.Global)
						{
							item.ToolTipText = "This tool doesn't work if nothing is loaded";
							item.Enabled = false;
						}
						else if(attribute2.ToolUsage == BizHawkExternalToolUsage.EmulatorSpecific && Global.Emulator.SystemId != ClientApi.SystemIdConverter.ConvertBack(attribute2.System))
						{
							item.ToolTipText = "This tool doesn't work for current system";
							item.Enabled = false;
						}
						else if (attribute2.ToolUsage == BizHawkExternalToolUsage.GameSpecific && Global.Game.Hash != attribute2.GameHash)
						{
							item.ToolTipText = "This tool doesn't work for current game";
							item.Enabled = false;
						}
					}
				}
				else
				{
					item = new ToolStripMenuItem(externalToolFile.GetName().Name)
					{
						ToolTipText = "BizHawkExternalTool attribute hasn't been found", Enabled = false
					};
				}
			}
			catch (BadImageFormatException)
			{
				item = new ToolStripMenuItem(fileName);
				item.ToolTipText = "This is not an assembly";
				item.Enabled = false;
			}

#if DEBUG //I added special debug stuff to get additional information. Don't think it can be useful for released versions
			catch (ReflectionTypeLoadException ex)
			{
				foreach (Exception e in ex.LoaderExceptions)
				{
					Debug.WriteLine(e.Message);
				}
				item.ToolTipText = "Something goes wrong while trying to load";
				item.Enabled = false;
			}
#else
			catch (ReflectionTypeLoadException)
			{
				item.ToolTipText = "Something goes wrong while trying to load";
				item.Enabled = false;
			}
#endif

			return item;
		}

		/// <summary>
		/// This event is raised when we add a dll file into
		/// the external tools path.
		/// It will automatically load the assembly and add it into the list
		/// </summary>
		/// <param name="sender">Object that raised the event</param>
		/// <param name="e">Event arguments</param>
		private static void DirectoryMonitor_Created(object sender, FileSystemEventArgs e)
		{
			MenuItems.Add(GenerateToolTipFromFileName(e.FullPath));
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets a prebuild <see cref="ToolStripMenuItem"/>
		/// This list auto-updated by the <see cref="ExternalToolManager"/> itself
		/// </summary>
		public static IEnumerable<ToolStripMenuItem> ToolStripMenu => MenuItems;

		#endregion
	}
}
