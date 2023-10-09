using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public sealed class ExternalToolManager
	{
		public struct MenuItemInfo
		{
			public readonly string AsmChecksum = "";

			public readonly string EntryPointTypeName = "";

			public readonly string AsmFilename = "";

			public readonly string Text;

			public readonly Bitmap Icon = null;

			public readonly string ToolTip;

			public readonly bool Enabled = false;

			public MenuItemInfo(
				string asmChecksum,
				string asmFilename,
				string entryPointTypeName,
				string text,
				Bitmap icon,
				string toolTip,
				bool enabled)
			{
				AsmChecksum = asmChecksum;
				EntryPointTypeName = entryPointTypeName;
				AsmFilename = asmFilename;
				Text = text;
				Icon = icon;
				ToolTip = toolTip;
				Enabled = enabled;
			}

			public MenuItemInfo(string text, string toolTip)
			{
				Text = text;
				ToolTip = toolTip;
			}
		}

		private Config _config;

		private readonly Func<(string SysID, string Hash)> _getLoadedRomInfoCallback;

		private FileSystemWatcher DirectoryMonitor;

		private readonly List<MenuItemInfo> MenuItems = new List<MenuItemInfo>();

		public readonly IList<string> PossibleExtToolTypeNames = new List<string>();

		public ExternalToolManager(
			Config config,
			Func<(string SysID, string Hash)> getLoadedRomInfoCallback)
		{
			_getLoadedRomInfoCallback = getLoadedRomInfoCallback;
			Restart(config);
		}

		public void Restart(Config config)
		{
			_config = config;
			if (DirectoryMonitor != null)
			{
				DirectoryMonitor.Created -= DirectoryMonitor_Created;
				DirectoryMonitor.Dispose();
			}
			var path = _config.PathEntries[PathEntryCollection.GLOBAL, "External Tools"].Path;
			if (Directory.Exists(path))
			{
				DirectoryMonitor = new FileSystemWatcher(path, "*.dll")
				{
					IncludeSubdirectories = false,
					NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName,
					Filter = "*.dll"
				};
				DirectoryMonitor.Created += DirectoryMonitor_Created;
				DirectoryMonitor.EnableRaisingEvents = true;
			}
			BuildToolStripInfo();
		}

		public void BuildToolStripInfo()
		{
			MenuItems.Clear();
			PossibleExtToolTypeNames.Clear();
			if (DirectoryMonitor == null) return;
			DirectoryInfo di = new(DirectoryMonitor.Path);
			if (!di.Exists) return;
			foreach (var fi in di.GetFiles("*.dll")) MenuItems.Add(GenerateMenuItemTextFromFileName(fi.FullName));
		}

		private MenuItemInfo GenerateMenuItemTextFromFileName(string fileName)
		{
			if (fileName == null) throw new Exception();
			try
			{
				if (!OSTailoredCode.IsUnixHost) MotWHack.RemoveMOTW(fileName);
				var asmBytes = File.ReadAllBytes(fileName);
				var externalToolFile = Assembly.Load(asmBytes);
				var entryPoint = externalToolFile.GetTypes()
					.SingleOrDefault(t => typeof(IExternalToolForm).IsAssignableFrom(t) && t.GetCustomAttributes().OfType<ExternalToolAttribute>().Any());
				if (entryPoint == null) throw new ExternalToolAttribute.MissingException();

				var allAttrs = entryPoint.GetCustomAttributes().ToList();
				var applicabilityAttrs = allAttrs.OfType<ExternalToolApplicabilityAttributeBase>().ToList();
				if (applicabilityAttrs.Count > 1) throw new ExternalToolApplicabilityAttributeBase.DuplicateException();

				var toolAttribute = allAttrs.OfType<ExternalToolAttribute>().First();
				if (toolAttribute.LoadAssemblyFiles != null)
				{
					foreach (var depFilename in toolAttribute.LoadAssemblyFiles) Assembly.LoadFrom($"{_config.PathEntries[PathEntryCollection.GLOBAL, "External Tools"].Path}/{depFilename}");
				}

				Bitmap image = null; // no errors, remove error icon
				var embeddedIconAttr = allAttrs.OfType<ExternalToolEmbeddedIconAttribute>().FirstOrDefault();
				if (embeddedIconAttr != null)
				{
					var rawIcon = externalToolFile.GetManifestResourceStream(embeddedIconAttr.ResourcePath);
					if (rawIcon != null) image = new Bitmap(rawIcon);
				}
				PossibleExtToolTypeNames.Add(entryPoint.AssemblyQualifiedName);
				bool enabled = true;
				string toolTip = "";
				if (applicabilityAttrs.Count is 1)
				{
					var (system, loadedRomHash) = _getLoadedRomInfoCallback();
					if (applicabilityAttrs[0].NotApplicableTo(system))
					{
						toolTip = system is VSystemID.Raw.NULL
							? "This tool doesn't work when no rom is loaded"
							: "This tool doesn't work with this system";
						enabled = false;
					}
					else if (applicabilityAttrs[0].NotApplicableTo(loadedRomHash, system))
					{
						toolTip = "This tool doesn't work with this game";
						enabled = false;
					}
				}
				else if (!string.IsNullOrWhiteSpace(toolAttribute.Description))
					toolTip = toolAttribute.Description;

				return new MenuItemInfo(
					asmChecksum: SHA1Checksum.ComputePrefixedHex(asmBytes),
					asmFilename: fileName,
					entryPointTypeName: entryPoint.FullName,
					text: toolAttribute.Name,
					icon: image,
					toolTip: toolTip,
					enabled: enabled);
			}
			catch (Exception e)
			{
#if DEBUG
				if (e is ReflectionTypeLoadException rtle)
				{
					foreach (var e1 in rtle.LoaderExceptions) Console.WriteLine(e1.Message);
				}
#endif
				string text = Path.GetFileName(fileName);
				string toolTip = e switch
				{
					BadImageFormatException => "This assembly can't be loaded, probably because it's corrupt or targets an incompatible .NET runtime.",
					ExternalToolApplicabilityAttributeBase.DuplicateException => "The IExternalToolForm has conflicting applicability attributes.",
					ExternalToolAttribute.MissingException => "The assembly doesn't contain a class implementing IExternalToolForm and annotated with [ExternalTool].",
					ReflectionTypeLoadException => "Something went wrong while trying to load the assembly.",
					_ => $"An exception of type {e.GetType().FullName} was thrown while trying to load the assembly and look for an IExternalToolForm:\n{e.Message}"
				};
				return new MenuItemInfo(text, toolTip);
			}
		}

		/// <summary>
		/// This event is raised when we add a dll file into
		/// the external tools path.
		/// It will automatically load the assembly and add it into the list
		/// </summary>
		/// <param name="sender">Object that raised the event</param>
		/// <param name="e">Event arguments</param>
		private void DirectoryMonitor_Created(object sender, FileSystemEventArgs e)
		{
			MenuItems.Add(GenerateMenuItemTextFromFileName(e.FullPath));
		}

		public IReadOnlyCollection<MenuItemInfo> ToolStripItems
			=> MenuItems;
	}
}
