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

		private static FileSystemWatcher directoryMonitor;
		private static List<ToolStripMenuItem> menuItems = new List<ToolStripMenuItem>();

		#endregion

		#region cTor(s)

		/// <summary>
		/// Initilization
		/// </summary>
		static ExternalToolManager()
		{
			if(!Directory.Exists(Global.Config.PathEntries["Global", "External Tools"].Path))
			{
				Directory.CreateDirectory(Global.Config.PathEntries["Global", "External Tools"].Path);
			}
			directoryMonitor = new FileSystemWatcher(Global.Config.PathEntries["Global", "External Tools"].Path, "*.dll");
			directoryMonitor.IncludeSubdirectories = false;
			directoryMonitor.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName;
			directoryMonitor.Filter = "*.dll";
			directoryMonitor.Created += new FileSystemEventHandler(DirectoryMonitor_Created);
			directoryMonitor.EnableRaisingEvents = true;

			ClientApi.RomLoaded += delegate { BuildToolStrip(); };

			BuildToolStrip();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Build the toolstrip menu
		/// </summary>
		private static void BuildToolStrip()
		{
			menuItems.Clear();
			if (Directory.Exists(directoryMonitor.Path))
			{
				DirectoryInfo dInfo = new DirectoryInfo(directoryMonitor.Path);

				foreach (FileInfo fi in dInfo.GetFiles("*.dll"))
				{
					menuItems.Add(GenerateToolTipFromFileName(fi.FullName));
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
			Type customFormType;
			Assembly externalToolFile;
			ToolStripMenuItem item = null;

			try
			{
				externalToolFile = Assembly.LoadFrom(fileName);
				object[] attributes = externalToolFile.GetCustomAttributes(typeof(BizHawkExternalToolAttribute), false);
				if (attributes != null && attributes.Count() == 1)
				{
					BizHawkExternalToolAttribute attribute = (BizHawkExternalToolAttribute)attributes[0];
					item = new ToolStripMenuItem(attribute.Name);
					item.ToolTipText = attribute.Description;
					if (attribute.IconResourceName != string.Empty)
					{
						Stream s = externalToolFile.GetManifestResourceStream(string.Format("{0}.{1}", externalToolFile.GetName().Name, attribute.IconResourceName));
						if (s != null)
						{
							item.Image = new Bitmap(s);
						}
					}

					customFormType = externalToolFile.GetTypes().FirstOrDefault<Type>(t => t != null && t.FullName == "BizHawk.Client.EmuHawk.CustomMainForm");
					if (customFormType == null)
					{
						item.ToolTipText = "Does not have a CustomMainForm";
						item.Enabled = false;
					}
					item.Tag = fileName;

					attributes = externalToolFile.GetCustomAttributes(typeof(BizHawkExternalToolUsageAttribute), false);
					if (attributes != null && attributes.Count() == 1)
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
					item = new ToolStripMenuItem(externalToolFile.GetName().Name);
					item.ToolTipText = "BizHawkExternalTool attribute hasn't been found";
					item.Enabled = false;
				}
			}
			catch (BadImageFormatException)
			{
				item = new ToolStripMenuItem(fileName);
				item.ToolTipText = "This is not an assembly";
				item.Enabled = false;
			}

#if DEBUG //I added special debug stuff to get additionnal informations. Don(t think it can be usefull for released versions
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
			menuItems.Add(GenerateToolTipFromFileName(e.FullPath));
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets a prebuild <see cref="ToolStripMenuItem"/>
		/// This list auto-updated by the <see cref="ExternalToolManager"/> itself
		/// </summary>
		public static IEnumerable<ToolStripMenuItem> ToolStripMenu
		{
			get
			{
				return menuItems;
			}
		}

		#endregion
	}
}
