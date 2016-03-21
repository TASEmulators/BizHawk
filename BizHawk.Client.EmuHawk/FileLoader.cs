using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Emulation.Cores.Calculators;
using BizHawk.Emulation.Cores.ColecoVision;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.N64;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Sega.MasterSystem;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;

using BizHawk.Client.Common;

using BizHawk.Client.EmuHawk.CustomControls;
using BizHawk.Client.EmuHawk.WinFormExtensions;
using BizHawk.Client.EmuHawk.ToolExtensions;
using BizHawk.Emulation.Cores.Computers.AppleII;
using BizHawk.Client.ApiHawk;

namespace BizHawk.Client.EmuHawk
{
	partial class MainForm
	{
		private enum LoadOrdering
		{
			ROM,
			STATE,
			WATCH,
			CDLFILE,
			LUASESSION,
			LUASCRIPT,
			CHEAT,
			MOVIEFILE,
			LEGACYMOVIEFILE
		}

		public struct FileInformation
		{
			public string directoryName;
			public string fileName;
			public string archiveName;

			public FileInformation(string directory, string file, string archive)
			{
				directoryName = directory;
				fileName = file;
				archiveName = archive;
			}
		}

		// This is the list from MainForm->RomFilter()'s non-developer build.  It needs to be kept up-to-date when new cores are added.
		readonly string[] knownROMExtensions = { ".NES", ".FDS", ".UNF", ".SMS", ".GG", ".SG", ".GB", ".GBC", ".GBA", ".PCE", ".SGX", ".BIN", ".SMD", ".GEN", ".MD", ".SMC", ".SFC", ".A26", ".A78", ".LNX", ".COL", ".ROM", ".M3U", ".CUE", ".CCD", ".SGB", ".Z64", ".V64", ".N64", ".WS", ".WSC", ".XML", ".DSK", ".DO", ".PO", ".PSF", ".MINIPSF", ".NSF" };
		readonly string[] nonArchive = { ".ISO", ".CUE", ".CCD" };

		#region Loaders

		// According to the documentation (http://tasvideos.org/Bizhawk/CodeDataLogger.html),
		// Currently supported for: PCE, GB/GBC, SMS/GG, Genesis, SNES
		// Perhaps the 'is PCEngine' requirement needs to be expanded.
		private void _LoadCDL(string filename, string archive = null)
		{
			if (!(Global.Emulator is PCEngine))
				return;

			GlobalWin.Tools.Load<CDL>();
			(GlobalWin.Tools.Get<CDL>() as CDL).LoadFile(filename);
		}

		private void _LoadCheats(string filename, string archive = null)
		{
			Global.CheatList.Load(filename, false);
			GlobalWin.Tools.Load<Cheats>();
		}

		private void _LoadLegacyMovie(string filename, string archive = null)
		{
			if (Global.Emulator.IsNull())
			{
				OpenRom();
			}

			if (Global.Emulator.IsNull())
			{
				return;
			}

			// tries to open a legacy movie format by importing it
			string errorMsg;
			string warningMsg;
			var movie = MovieImport.ImportFile(filename, out errorMsg, out warningMsg);
			if (!string.IsNullOrEmpty(errorMsg))
			{
				MessageBox.Show(errorMsg, "Conversion error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
			{
				// fix movie extension to something palatable for these purposes. 
				// for instance, something which doesnt clobber movies you already may have had.
				// i'm evenly torn between this, and a file in %TEMP%, but since we dont really have a way to clean up this tempfile, i choose this:
				StartNewMovie(movie, false);
			}

			GlobalWin.OSD.AddMessage(warningMsg);
		}

		private void _LoadLuaFile(string filename, string archive = null)
		{
			OpenLuaConsole();
			if (GlobalWin.Tools.Has<LuaConsole>())
			{
				GlobalWin.Tools.LuaConsole.LoadLuaFile(filename);
			}
		}

		private void _LoadLuaSession(string filename, string archive = null)
		{
			OpenLuaConsole();
			if (GlobalWin.Tools.Has<LuaConsole>())
			{
				GlobalWin.Tools.LuaConsole.LoadLuaSession(filename);
			}
		}

		private void _LoadMovie(string filename, string archive = null)
		{
			if (Global.Emulator.IsNull())
			{
				OpenRom();
			}

			if (Global.Emulator.IsNull())
			{
				return;
			}

			StartNewMovie(MovieService.Get(filename), false);
		}

		private void _LoadRom(string filename, string archive = null)
		{
			var args = new LoadRomArgs();
			args.OpenAdvanced = new OpenAdvanced_OpenRom { Path = filename };
			LoadRom(filename, args);
		}

		private void _LoadState(string filename, string archive = null)
		{
			LoadState(filename, Path.GetFileName(filename));
		}

		private void _LoadWatch(string filename, string archive = null)
		{
			GlobalWin.Tools.LoadRamWatch(true);
			(GlobalWin.Tools.Get<RamWatch>() as RamWatch).LoadWatchFile(new FileInfo(filename), false);
		}

		#endregion

		private void ProcessFileList(string[] fileList, ref Dictionary<LoadOrdering, List<FileInformation>> sortedFiles, string archive = null)
		{
			foreach (string file in fileList)
			{
				var ext = Path.GetExtension(file).ToUpper() ?? String.Empty;
				FileInformation fileInformation = new FileInformation(Path.GetDirectoryName(file), Path.GetFileName(file), archive);

				switch (ext)
				{
					case ".LUA":
						sortedFiles[LoadOrdering.LUASCRIPT].Add(fileInformation);
						break;
					case ".LUASES":
						sortedFiles[LoadOrdering.LUASESSION].Add(fileInformation);
						break;
					case ".STATE":
						sortedFiles[LoadOrdering.STATE].Add(fileInformation);
						break;
					case ".CHT":
						sortedFiles[LoadOrdering.CHEAT].Add(fileInformation);
						break;
					case ".WCH":
						sortedFiles[LoadOrdering.WATCH].Add(fileInformation);
						break;
					case ".CDL":
						sortedFiles[LoadOrdering.CDLFILE].Add(fileInformation);
						break;
					default:
						if (MovieService.IsValidMovieExtension(ext))
							sortedFiles[LoadOrdering.MOVIEFILE].Add(fileInformation);
						else if (MovieImport.IsValidMovieExtension(ext))
							sortedFiles[LoadOrdering.LEGACYMOVIEFILE].Add(fileInformation);
						else if (knownROMExtensions.Contains(ext))
						{
							if (String.IsNullOrEmpty(archive) || !nonArchive.Contains(ext))
								sortedFiles[LoadOrdering.ROM].Add(fileInformation);
						}
						else
						{
							/* Because the existing behaviour for archives is to try loading
							 * ROMs out of them, that is exactly what we are going to continue
							 * to do at present.  Ideally, the archive should be scanned and
							 * relevant files should be extracted, but see the note below for
							 * further details.
							 */
							int offset = 0;
							bool executable = false;
							var archiveHandler = new SevenZipSharpArchiveHandler();

							if (String.IsNullOrEmpty(archive) && archiveHandler.CheckSignature(file, out offset, out executable))
								sortedFiles[LoadOrdering.ROM].Add(fileInformation);

							/*
							 * This is where handling archives would go.
							 * Right now, that's going to be a HUGE hassle, because of the problem with
							 * saving things into the archive (no) and with everything requiring filenames
							 * and not streams (also no), so for the purposes of making drag/drop more robust,
							 * I am not building this out just yet.
							 * -- Adam Michaud (Invariel)
							
							int offset = 0;
							bool executable = false;
							var archiveHandler = new SevenZipSharpArchiveHandler();

							// Not going to process nested archives at the moment.
							if (String.IsNullOrEmpty (archive) && archiveHandler.CheckSignature(file, out offset, out executable))
							{
								List<string> fileNames = new List<string>();
								var openedArchive = archiveHandler.Construct (file);

								foreach (BizHawk.Common.HawkFileArchiveItem item in openedArchive.Scan ())
									fileNames.Add(item.Name);

								ProcessFileList(fileNames.ToArray(), ref sortedFiles, file);

								openedArchive.Dispose();
							}
							archiveHandler.Dispose();
							 */
						}
						break;
				}
			}
		}

		private void _FormDragDrop_internal(object sender, DragEventArgs e)
		{
			/*
			 *  Refactor, moving the loading of particular files into separate functions that can
			 *  then be used by this code, and loading individual files through the file dialogue.
			 *  
			 *  Step 1:
			 *	  Build a dictionary of relevant files from everything that was dragged and dropped.
			 *	  This includes peeking into all relevant archives and using their files.
			 *	  
			 *  Step 2:
			 *	  Perhaps ask the user which of a particular file type they want to use.
			 *		  Example:  rom1.nes, rom2.smc, rom3.cue are drag-dropped, ask the user which they want to use.
			 *		  
			 *  Step 3:
			 *	  Load all of the relevant files, in priority order:
			 *	  1) The ROM
			 *	  2) State
			 *	  3) Watch files
			 *	  4) Code Data Logger (CDL)
			 *	  5) LUA sessions
			 *	  6) LUA scripts
			 *	  7) Cheat files
			 *	  8) Movie Playback Files
			 *	  
			 *  Bonus:
			 *	  Make that order easy to change in the code, heavily suggesting ROM and playback as first and last respectively.
			 */

			var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			Dictionary<LoadOrdering, List<FileInformation>> sortedFiles = new Dictionary<LoadOrdering, List<FileInformation>>();

			// Initialize the dictionary's lists.
			foreach (LoadOrdering value in Enum.GetValues(typeof(LoadOrdering)))
			{
				sortedFiles.Add(value, new List<FileInformation>());
			}

			ProcessFileList(filePaths, ref sortedFiles, null);

			// For each of the different types of item, if there are no items of that type, skip them.
			// If there is exactly one of that type of item, load it.
			// If there is more than one, ask.

			foreach (LoadOrdering value in Enum.GetValues(typeof(LoadOrdering)))
			{
				switch (sortedFiles[value].Count)
				{
					case 0:
						break;
					case 1:
						FileInformation fileInformation = sortedFiles[value].First<FileInformation>();
						string filename = Path.Combine(new string[] { fileInformation.directoryName, fileInformation.fileName });

						switch (value)
						{
							case LoadOrdering.ROM:
								_LoadRom(filename, fileInformation.archiveName);
								break;
							case LoadOrdering.STATE:
								_LoadState(filename, fileInformation.archiveName);
								break;
							case LoadOrdering.WATCH:
								_LoadWatch(filename, fileInformation.archiveName);
								break;
							case LoadOrdering.CDLFILE:
								_LoadCDL(filename, fileInformation.archiveName);
								break;
							case LoadOrdering.LUASESSION:
								_LoadLuaSession(filename, fileInformation.archiveName);
								break;
							case LoadOrdering.LUASCRIPT:
								_LoadLuaFile(filename, fileInformation.archiveName);
								break;
							case LoadOrdering.CHEAT:
								_LoadCheats(filename, fileInformation.archiveName);
								break;
							case LoadOrdering.MOVIEFILE:
							case LoadOrdering.LEGACYMOVIEFILE:
								// I don't really like this hack, but for now, we only want to load one movie file.
								if (sortedFiles[LoadOrdering.MOVIEFILE].Count + sortedFiles[LoadOrdering.LEGACYMOVIEFILE].Count > 1)
									break;

								if (value == LoadOrdering.MOVIEFILE)
									_LoadMovie(filename, fileInformation.archiveName);
								else
									_LoadLegacyMovie(filename, fileInformation.archiveName);
								break;
						}
						break;
					default:
						break;
				}
			}
		}
	}
}
