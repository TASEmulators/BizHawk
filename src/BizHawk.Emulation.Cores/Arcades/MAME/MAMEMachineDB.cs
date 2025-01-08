using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	public class MAMEMachineDB
	{
		/// <summary>
		/// blocks until the DB is done loading
		/// </summary>
		private static ManualResetEvent _acquire;

		private readonly HashSet<string> MachineDB = new();

		private static MAMEMachineDB Instance;

		public static void Initialize(string basePath)
		{
			if (_acquire != null) throw new InvalidOperationException("MAME Machine DB multiply initialized");
			_acquire = new(false);

			var sw = Stopwatch.StartNew();
			ThreadPool.QueueUserWorkItem(_ =>
			{
				Instance = new(basePath);
				Util.DebugWriteLine("MAME Machine DB load: " + sw.Elapsed + " sec");
				_acquire.Set();
			});
		}

		private MAMEMachineDB(string basePath)
		{
			using var mameMachineFile = new HawkFile(Path.Combine(basePath, "mame_machines.txt"));
			using var sr = new StreamReader(mameMachineFile.GetStream());
			while (true)
			{
				var line = sr.ReadLine();

				if (string.IsNullOrEmpty(line))
				{
					break;
				}

				MachineDB.Add(line);
			}
		}

		public static bool IsMAMEMachine(string path)
		{
#if BIZHAWKBUILD_GAMEDB_ALWAYS_MISS
			_ = path;
			return false;
#else
			if (_acquire == null) throw new InvalidOperationException("MAME Machine DB not initialized. It's a client responsibility because only a client knows where the database is located.");
			if (HawkFile.PathContainsPipe(path)) return false; // binded archive, can't be a mame zip (note | is not a legal filesystem char, at least on windows)
			if (Path.GetExtension(path).ToLowerInvariant() is not ".zip" and not ".7z") return false;
			_acquire.WaitOne();
			return Instance.MachineDB.Contains(Path.GetFileNameWithoutExtension(path).ToLowerInvariant());
#endif
		}
	}
}
