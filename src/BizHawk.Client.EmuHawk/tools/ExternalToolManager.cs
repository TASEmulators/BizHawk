using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;

using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class ExternalToolManager
	{
		public struct MenuItemInfo
		{
			private readonly string _asmChecksum;

			private readonly string _entryPointTypeName;

			private readonly ExternalToolManager _extToolMan;

			private bool _skipExtToolWarning;

			public readonly string AsmFilename;

			public MenuItemInfo(
				ExternalToolManager extToolMan,
				string asmChecksum,
				string asmFilename,
				string entryPointTypeName)
			{
				_asmChecksum = asmChecksum;
				_entryPointTypeName = entryPointTypeName;
				_extToolMan = extToolMan;
				_skipExtToolWarning = _extToolMan._config.TrustedExtTools.TryGetValue(asmFilename, out var s) && s == _asmChecksum;
				AsmFilename = asmFilename;
			}

			public void TryLoad()
			{
				var success = _extToolMan._loadCallback(
					/*toolPath:*/ AsmFilename,
					/*customFormTypeName:*/ _entryPointTypeName,
					/*skipExtToolWarning:*/ _skipExtToolWarning);
				if (!success || _skipExtToolWarning) return;
				_skipExtToolWarning = true;
				_extToolMan._config.TrustedExtTools[AsmFilename] = _asmChecksum;
			}
		}

		private Config _config;

		private readonly Func<(string SysID, string Hash)> _getLoadedRomInfoCallback;

		private readonly Func<string, string, bool, bool> _loadCallback;

		private FileSystemWatcher DirectoryMonitor;

		private readonly List<ToolStripMenuItem> MenuItems = new List<ToolStripMenuItem>();

		internal readonly IList<string> PossibleExtToolTypeNames = new List<string>();

		public ExternalToolManager(
			Config config,
			Func<(string SysID, string Hash)> getLoadedRomInfoCallback,
			Func<string, string, bool, bool> loadCallback)
		{
			_getLoadedRomInfoCallback = getLoadedRomInfoCallback;
			_loadCallback = loadCallback;
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
			BuildToolStrip();
		}

		internal void BuildToolStrip()
		{
			MenuItems.Clear();
			PossibleExtToolTypeNames.Clear();
			if (DirectoryMonitor == null) return;
			DirectoryInfo di = new(DirectoryMonitor.Path);
			if (!di.Exists) return;
			foreach (var fi in di.GetFiles("*.dll")) MenuItems.Add(GenerateToolTipFromFileName(fi.FullName));
		}

		/// <summary>Generates a <see cref="ToolStripMenuItem"/> from an assembly at <paramref name="fileName"/> containing an external tool.</summary>
		/// <returns>a <see cref="ToolStripMenuItem"/> with its <see cref="ToolStripItem.Tag"/> containing a <see cref="MenuItemInfo"/></returns>
		private ToolStripMenuItem GenerateToolTipFromFileName(string fileName)
		{
			if (fileName == null) throw new Exception();
			var item = new ToolStripMenuItem(Path.GetFileName(fileName))
			{
				Enabled = false,
				Image = Properties.Resources.ExclamationRed,
			};
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

				item.Image = null; // no errors, remove error icon
				var embeddedIconAttr = allAttrs.OfType<ExternalToolEmbeddedIconAttribute>().FirstOrDefault();
				if (embeddedIconAttr != null)
				{
					var rawIcon = externalToolFile.GetManifestResourceStream(embeddedIconAttr.ResourcePath);
					if (rawIcon != null) item.Image = new Bitmap(rawIcon);
				}
				item.Text = toolAttribute.Name;
				MenuItemInfo menuItemInfo = new(
					this,
					asmChecksum: SHA1Checksum.ComputePrefixedHex(asmBytes),
					asmFilename: fileName,
					entryPointTypeName: entryPoint.FullName);
				item.Tag = menuItemInfo;
				item.Click += (_, _) => menuItemInfo.TryLoad();
				PossibleExtToolTypeNames.Add(entryPoint.AssemblyQualifiedName);
				if (applicabilityAttrs.Count is 1)
				{
					var (system, loadedRomHash) = _getLoadedRomInfoCallback();
					if (applicabilityAttrs[0].NotApplicableTo(system))
					{
						item.ToolTipText = system is VSystemID.Raw.NULL
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
					foreach (var e1 in rtle.LoaderExceptions) Console.WriteLine(e1.Message);
				}
#endif
				item.ToolTipText = e switch
				{
					BadImageFormatException => "This assembly can't be loaded, probably because it's corrupt or targets an incompatible .NET runtime.",
					ExternalToolApplicabilityAttributeBase.DuplicateException => "The IExternalToolForm has conflicting applicability attributes.",
					ExternalToolAttribute.MissingException => "The assembly doesn't contain a class implementing IExternalToolForm and annotated with [ExternalTool].",
					ReflectionTypeLoadException => "Something went wrong while trying to load the assembly.",
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

		public IReadOnlyCollection<ToolStripItem> ToolStripItems
			=> MenuItems;
	}
}
