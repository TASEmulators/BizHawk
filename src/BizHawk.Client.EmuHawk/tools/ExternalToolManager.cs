using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;

using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class ExternalToolManager
	{
		private readonly Func<(CoreSystem System, string Hash)> _getLoadedRomInfoCallback;

		private FileSystemWatcher DirectoryMonitor;

		private readonly List<ToolStripMenuItem> MenuItems = new List<ToolStripMenuItem>();

		public ExternalToolManager(PathEntryCollection paths, Func<(CoreSystem System, string Hash)> getLoadedRomInfoCallback)
		{
			_getLoadedRomInfoCallback = getLoadedRomInfoCallback;
			Restart(paths);
		}

		public void Restart(PathEntryCollection paths)
		{
			if (DirectoryMonitor != null)
			{
				DirectoryMonitor.Created -= DirectoryMonitor_Created;
				DirectoryMonitor.Dispose();
			}
			var extToolsDir = paths["Global", "External Tools"].Path;
			if (!Directory.Exists(extToolsDir)) Directory.CreateDirectory(extToolsDir);
			DirectoryMonitor = new FileSystemWatcher(extToolsDir, "*.dll")
			{
				IncludeSubdirectories = false,
				NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName,
				Filter = "*.dll"
			};
			DirectoryMonitor.Created += DirectoryMonitor_Created;
			DirectoryMonitor.EnableRaisingEvents = true;
			BuildToolStrip();
		}

		internal void BuildToolStrip()
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

		/// <summary>Generates a <see cref="ToolStripMenuItem"/> from an assembly at <paramref name="fileName"/> containing an external tool.</summary>
		/// <returns>
		/// a <see cref="ToolStripMenuItem"/> with its <see cref="ToolStripItem.Tag"/> containing a <c>(string, string)</c>;
		/// the first is the assembly path (<paramref name="fileName"/>) and the second is the <see cref="Type.FullName"/> of the entry point form's type
		/// </returns>
		private ToolStripMenuItem GenerateToolTipFromFileName(string fileName)
		{
			if (fileName == null) throw new Exception();
			var item = new ToolStripMenuItem(Path.GetFileName(fileName)) { Enabled = false };
			try
			{
				if (!OSTailoredCode.IsUnixHost) MotWHack.RemoveMOTW(fileName);
				var externalToolFile = Assembly.LoadFrom(fileName);
				var entryPoint = externalToolFile.GetTypes()
					.SingleOrDefault(t => typeof(IExternalToolForm).IsAssignableFrom(t) && t.GetCustomAttributes().OfType<ExternalToolAttribute>().Any());
#pragma warning disable CS0618
				if (entryPoint == null) throw new ExternalToolAttribute.MissingException(externalToolFile.GetCustomAttributes().OfType<BizHawkExternalToolAttribute>().Any());
#pragma warning restore CS0618

				var allAttrs = entryPoint.GetCustomAttributes().ToList();
				var applicabilityAttrs = allAttrs.OfType<ExternalToolApplicabilityAttributeBase>().ToList();
				if (applicabilityAttrs.Count > 1) throw new ExternalToolApplicabilityAttributeBase.DuplicateException();

				var toolAttribute = allAttrs.OfType<ExternalToolAttribute>().First();
				if (toolAttribute.LoadAssemblyFiles != null)
				{
					foreach (var depFilename in toolAttribute.LoadAssemblyFiles) Assembly.LoadFrom($"ExternalTools/{depFilename}");
				}
				var embeddedIconAttr = allAttrs.OfType<ExternalToolEmbeddedIconAttribute>().FirstOrDefault();
				if (embeddedIconAttr != null)
				{
					var rawIcon = externalToolFile.GetManifestResourceStream(embeddedIconAttr.ResourcePath);
					if (rawIcon != null) item.Image = new Bitmap(rawIcon);
				}
				item.Text = toolAttribute.Name;
				item.Tag = (externalToolFile.Location, entryPoint.FullName); // Tag set => no errors (show custom icon even when disabled)
				if (applicabilityAttrs.Count == 1)
				{
					var (system, loadedRomHash) = _getLoadedRomInfoCallback();
					if (applicabilityAttrs[0].NotApplicableTo(system))
					{
						item.ToolTipText = system == CoreSystem.Null
							? "This tool doesn't work when no rom is loaded"
							: "This tool doesn't work with this system";
						return item;
					}
					if (applicabilityAttrs[0].NotApplicableTo(loadedRomHash, system))
					{
						item.ToolTipText = "This tool doesn't work with this game";
						return item;
					}
				}

				item.Enabled = true;
				if (!string.IsNullOrWhiteSpace(toolAttribute.Description)) item.ToolTipText = toolAttribute.Description;
				return item;
			}
			catch (Exception e)
			{
#if DEBUG
				if (e is ReflectionTypeLoadException rtle)
				{
					foreach (var e1 in rtle.LoaderExceptions) System.Diagnostics.Debug.WriteLine(e1.Message);
				}
#endif
				item.ToolTipText = e switch
				{
					BadImageFormatException _ => "This assembly can't be loaded, probably because it's corrupt or targets an incompatible .NET runtime.",
					ExternalToolApplicabilityAttributeBase.DuplicateException _ => "The IExternalToolForm has conflicting applicability attributes.",
					ExternalToolAttribute.MissingException e1 => e1.OldAttributeFound
						? "The assembly doesn't contain a class implementing IExternalToolForm and annotated with [ExternalTool].\nHowever, the assembly itself is annotated with [BizHawkExternalTool], which is now deprecated. Has the tool been updated since BizHawk 2.4?"
						: "The assembly doesn't contain a class implementing IExternalToolForm and annotated with [ExternalTool].",
					ReflectionTypeLoadException _ => "Something went wrong while trying to load the assembly.",
					_ => $"An exception of type {e.GetType().FullName} was thrown while trying to load the assembly and look for an IExternalToolForm:\n{e.Message}"
				};
			}
			return item;
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
			MenuItems.Add(GenerateToolTipFromFileName(e.FullPath));
		}

		/// <summary>
		/// Gets a prebuild <see cref="ToolStripMenuItem"/>
		/// This list auto-updated by the <see cref="ExternalToolManager"/> itself
		/// </summary>
		public IEnumerable<ToolStripMenuItem> ToolStripMenu => MenuItems;
	}
}
