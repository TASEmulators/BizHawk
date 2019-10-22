using System;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Client.Common;

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
			public string DirectoryName { get; }
			public string FileName { get; }
			public string ArchiveName { get; }

			public FileInformation(string directory, string file, string archive)
			{
				DirectoryName = directory;
				FileName = file;
				ArchiveName = archive;
			}
		}

		private IEnumerable<string> KnownRomExtensions =>
			RomFilterEntries.SelectMany(f => f.EffectiveFilters.Where(s => s.StartsWith("*.", StringComparison.Ordinal)).Select(s => s.Substring(1).ToUpperInvariant()));

		private readonly string[] _nonArchive = { ".ISO", ".CUE", ".CCD" };

		#region Loaders

		private void _LoadCDL(string filename, string archive = null)
		{
			if (GlobalWin.Tools.IsAvailable<CDL>())
			{
				CDL cdl = GlobalWin.Tools.Load<CDL>();
				cdl.LoadFile(filename);
			}
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

		private void ProcessFileList(IEnumerable<string> fileList, ref Dictionary<LoadOrdering, List<FileInformation>> sortedFiles, string archive = null)
		{
			foreach (string file in fileList)
			{
				var ext = Path.GetExtension(file).ToUpperInvariant() ?? "";
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
						{
							sortedFiles[LoadOrdering.MOVIEFILE].Add(fileInformation);
						}
						else if (MovieImport.IsValidMovieExtension(ext))
						{
							sortedFiles[LoadOrdering.LEGACYMOVIEFILE].Add(fileInformation);
						}
						else if (KnownRomExtensions.Contains(ext))
						{
							if (string.IsNullOrEmpty(archive) || !_nonArchive.Contains(ext))
							{
								sortedFiles[LoadOrdering.ROM].Add(fileInformation);
							}
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

							if (string.IsNullOrEmpty(archive) && archiveHandler.CheckSignature(file, out offset, out executable))
							{
								sortedFiles[LoadOrdering.ROM].Add(fileInformation);
							}
							else
							{
								// adelikat: adding this hack to restore the default behavior that unrecognized files are treated like roms
								sortedFiles[LoadOrdering.ROM].Add(fileInformation);
							}

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

			ProcessFileList(HawkFile.Util_ResolveLinks(filePaths), ref sortedFiles, null);

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
						string filename = Path.Combine(new string[] { fileInformation.DirectoryName, fileInformation.FileName });

						switch (value)
						{
							case LoadOrdering.ROM:
								_LoadRom(filename, fileInformation.ArchiveName);
								break;
							case LoadOrdering.STATE:
								_LoadState(filename, fileInformation.ArchiveName);
								break;
							case LoadOrdering.WATCH:
								_LoadWatch(filename, fileInformation.ArchiveName);
								break;
							case LoadOrdering.CDLFILE:
								_LoadCDL(filename, fileInformation.ArchiveName);
								break;
							case LoadOrdering.LUASESSION:
								_LoadLuaSession(filename, fileInformation.ArchiveName);
								break;
							case LoadOrdering.LUASCRIPT:
								_LoadLuaFile(filename, fileInformation.ArchiveName);
								break;
							case LoadOrdering.CHEAT:
								_LoadCheats(filename, fileInformation.ArchiveName);
								break;
							case LoadOrdering.MOVIEFILE:
							case LoadOrdering.LEGACYMOVIEFILE:
								// I don't really like this hack, but for now, we only want to load one movie file.
								if (sortedFiles[LoadOrdering.MOVIEFILE].Count + sortedFiles[LoadOrdering.LEGACYMOVIEFILE].Count > 1)
									break;

								if (value == LoadOrdering.MOVIEFILE)
									_LoadMovie(filename, fileInformation.ArchiveName);
								else
									_LoadLegacyMovie(filename, fileInformation.ArchiveName);
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
