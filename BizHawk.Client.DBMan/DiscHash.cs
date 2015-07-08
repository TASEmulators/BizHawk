using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.Client.DBMan
{
	class DiscHash
	{
		Job job;
		public void Run(string[] args)
		{
			using (job = new Job())
			{
				MyRun(args);
			}
		}

		static List<string> FindExtensionsRecurse(string dir, string extUppercaseWithDot)
		{
			List<string> ret = new List<string>();
			Queue<string> dpTodo = new Queue<string>();
			dpTodo.Enqueue(dir);
			for (; ; )
			{
				string dpCurr;
				if (dpTodo.Count == 0)
					break;
				dpCurr = dpTodo.Dequeue();
				Parallel.ForEach(new DirectoryInfo(dpCurr).GetFiles(), (fi) =>
				{
					if (fi.Extension.ToUpperInvariant() == extUppercaseWithDot)
						lock (ret)
							ret.Add(fi.FullName);
				});
				Parallel.ForEach(new DirectoryInfo(dpCurr).GetDirectories(), (di) =>
				{
					lock (dpTodo)
						dpTodo.Enqueue(di.FullName);
				});
			}

			return ret;
		}

		void MyRun(string[] args)
		{
			string indir = null;
			string dpTemp = null;
			string fpOutfile = null;

			for(int i=0;;)
			{
				if (i == args.Length) break;
				var arg = args[i++];
				if (arg == "--indir")
					indir = args[i++];
				if (arg == "--tempdir")
					dpTemp = args[i++];
				if (arg == "--outfile")
					fpOutfile = args[i++];
			}

			using(var outf = new StreamWriter(fpOutfile))
			{

				Dictionary<uint, string> FoundHashes = new Dictionary<uint, string>();
				object olock = new object();

				var todo = FindExtensionsRecurse(indir, ".CUE");

				int progress = 0;

				//loop over games (parallel doesnt work well when reading tons of data over the network, as we are here to do the complete redump hash)
				var po = new ParallelOptions();
				//po.MaxDegreeOfParallelism = Environment.ProcessorCount - 1;
				po.MaxDegreeOfParallelism = 1;
				Parallel.ForEach(todo, po, (fiCue) =>
				{
					string name = Path.GetFileNameWithoutExtension(fiCue);

					//now look for the cue file
					using (var disc = Disc.LoadAutomagic(fiCue))
					{
						var hasher = new DiscHasher(disc);

						uint bizHashId = hasher.Calculate_PSX_BizIDHash();
						uint redumpHash = hasher.Calculate_PSX_RedumpHash();

						lock (olock)
						{
							progress++;
							Console.WriteLine("{0}/{1} [{2:X8}] {3}", progress, todo.Count, bizHashId, Path.GetFileNameWithoutExtension(fiCue));
							outf.WriteLine("bizhash:{0:X8} datahash:{1:X8} //{2}", bizHashId, redumpHash, name);
							if (FoundHashes.ContainsKey(bizHashId))
							{
								Console.WriteLine("--> COLLISION WITH: {0}", FoundHashes[bizHashId]);
								outf.WriteLine("--> COLLISION WITH: {0}", FoundHashes[bizHashId]);
							}
							else
								FoundHashes[bizHashId] = name;

							Console.Out.Flush();
							outf.Flush();
						}
					}


				}); //major loop

			} //using(outfile)

		} //MyRun()
	} //class PsxRedump

	public enum JobObjectInfoType
	{
		AssociateCompletionPortInformation = 7,
		BasicLimitInformation = 2,
		BasicUIRestrictions = 4,
		EndOfJobTimeInformation = 6,
		ExtendedLimitInformation = 9,
		SecurityLimitInformation = 5,
		GroupInformation = 11
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SECURITY_ATTRIBUTES
	{
		public int nLength;
		public IntPtr lpSecurityDescriptor;
		public int bInheritHandle;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct JOBOBJECT_BASIC_LIMIT_INFORMATION
	{
		public Int64 PerProcessUserTimeLimit;
		public Int64 PerJobUserTimeLimit;
		public Int16 LimitFlags;
		public UInt32 MinimumWorkingSetSize;
		public UInt32 MaximumWorkingSetSize;
		public Int16 ActiveProcessLimit;
		public Int64 Affinity;
		public Int16 PriorityClass;
		public Int16 SchedulingClass;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct IO_COUNTERS
	{
		public UInt64 ReadOperationCount;
		public UInt64 WriteOperationCount;
		public UInt64 OtherOperationCount;
		public UInt64 ReadTransferCount;
		public UInt64 WriteTransferCount;
		public UInt64 OtherTransferCount;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
	{
		public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
		public IO_COUNTERS IoInfo;
		public UInt32 ProcessMemoryLimit;
		public UInt32 JobMemoryLimit;
		public UInt32 PeakProcessMemoryUsed;
		public UInt32 PeakJobMemoryUsed;
	}

	public class Job : IDisposable
	{
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		static extern IntPtr CreateJobObject(object a, string lpName);

		[DllImport("kernel32.dll")]
		static extern bool SetInformationJobObject(IntPtr hJob, JobObjectInfoType infoType, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

		private IntPtr m_handle;
		private bool m_disposed = false;

		public Job()
		{
			m_handle = CreateJobObject(null, null);

			JOBOBJECT_BASIC_LIMIT_INFORMATION info = new JOBOBJECT_BASIC_LIMIT_INFORMATION();
			info.LimitFlags = 0x2000;

			JOBOBJECT_EXTENDED_LIMIT_INFORMATION extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
			extendedInfo.BasicLimitInformation = info;

			int length = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
			IntPtr extendedInfoPtr = Marshal.AllocHGlobal(length);
			Marshal.StructureToPtr(extendedInfo, extendedInfoPtr, false);

			if (!SetInformationJobObject(m_handle, JobObjectInfoType.ExtendedLimitInformation, extendedInfoPtr, (uint)length))
				throw new Exception(string.Format("Unable to set information.  Error: {0}", Marshal.GetLastWin32Error()));
		}

		#region IDisposable Members

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		private void Dispose(bool disposing)
		{
			if (m_disposed)
				return;

			if (disposing) { }

			Close();
			m_disposed = true;
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CloseHandle(IntPtr hObject);
		public void Close()
		{
			CloseHandle(m_handle);
			m_handle = IntPtr.Zero;
		}

		public bool AddProcess(IntPtr handle)
		{
			return AssignProcessToJobObject(m_handle, handle);
		}

	}
}