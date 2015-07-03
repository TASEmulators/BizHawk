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
		public class CRC32
		{
			// Lookup table for speed.
			private static readonly uint[] CRC32Table;

			static CRC32()
			{
				CRC32Table = new uint[256];
				for (uint i = 0; i < 256; ++i)
				{
					uint crc = i;
					for (int j = 8; j > 0; --j)
					{
						if ((crc & 1) == 1)
							crc = ((crc >> 1) ^ 0xEDB88320);
						else
							crc >>= 1;
					}
					CRC32Table[i] = crc;
				}
			}

			uint current = 0xFFFFFFFF;
			public void Add(byte[] data, int offset, int size)
			{
				for (int i = 0; i < size; i++)
				{
					byte b = data[offset + i];
					current = (((Result) >> 8) ^ CRC32Table[b ^ ((Result) & 0xFF)]);
				}
			}

			byte[] smallbuf = new byte[8];
			public void Add(int data)
			{
				smallbuf[0] = (byte)((data) & 0xFF);
				smallbuf[1] = (byte)((data >> 8) & 0xFF);
				smallbuf[2] = (byte)((data >> 16) & 0xFF);
				smallbuf[3] = (byte)((data >> 24) & 0xFF);
				Add(smallbuf, 0, 4);
			}

			public uint Result { get { return current ^ 0xFFFFFFFF; } }
		}

		Job job;
		public void Run(string[] args)
		{
			using (job = new Job())
			{
				MyRun(args);
			}
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

			//prepare input
			var diInput = new DirectoryInfo(indir);

			//prepare output
			if(dpTemp == null) dpTemp = Path.GetTempFileName() + ".dir";
			//delete existing files in output
			foreach (var fi in new DirectoryInfo(dpTemp).GetFiles())
				fi.Delete();
			foreach (var di in new DirectoryInfo(dpTemp).GetDirectories())
				di.Delete(true);
			
			using(var outf = new StreamWriter(fpOutfile))
			{

				Dictionary<uint, string> FoundHashes = new Dictionary<uint, string>();
				object olock = new object();
				int ctr = 0;


				//loop over games
				var po = new ParallelOptions();
				po.MaxDegreeOfParallelism = Environment.ProcessorCount - 1;
				Parallel.ForEach(diInput.GetFiles("*.7z"), po, (fi) =>
				{
					CRC32 crc = new CRC32();
					byte[] discbuf = new byte[2352*28];

					int myctr;
					lock (olock)
						myctr = ctr++;

					string mydpTemp = Path.Combine(dpTemp,myctr.ToString());

					var mydiTemp = new DirectoryInfo(mydpTemp);
					mydiTemp.Create();

					var process = new System.Diagnostics.Process();
					process.StartInfo.UseShellExecute = false;
					process.StartInfo.RedirectStandardOutput = true;
					process.StartInfo.FileName = "7z.exe";
					process.StartInfo.Arguments = string.Format("x -o\"{1}\" \"{0}\"", fi.FullName, mydiTemp.FullName);
					process.Start();
					job.AddProcess(process.Handle);

					//if we need it
					//for (; ; )
					//{
					//  int c = process.StandardOutput.Read();
					//  if (c == -1)
					//    break;
					//  Console.Write((char)c);
					//}
					process.WaitForExit(); //just in case

					//now look for the cue file
					var fiCue = mydiTemp.GetFiles("*.cue").FirstOrDefault();
					if (fiCue == null)
					{
						Console.WriteLine("MISSING CUE FOR: " + fi.Name);
						outf.WriteLine("MISSING CUE FOR: " + fi.Name);
						Console.Out.Flush();
					}
					else
					{
						using (var disc = Disc.LoadAutomagic(fiCue.FullName))
						{
							//generate a hash with our own custom approach
							//basically, the "TOC" and a few early sectors completely
							crc.Add(disc.LBACount);
							crc.Add(disc.Structure.Sessions[0].Tracks.Count);
							foreach (var track in disc.Structure.Sessions[0].Tracks)
							{
								crc.Add(track.Start_ABA);
								crc.Add(track.Length);
							}
							//ZAMMO: change to disc sector reader, maybe a new class to read multiple
							disc.ReadLBA_2352_Flat(0, discbuf, 0, discbuf.Length);
							crc.Add(discbuf, 0, discbuf.Length);

							lock (olock)
							{
								Console.WriteLine("[{0:X8}] {1}", crc.Result, fi.Name);
								outf.WriteLine("[{0:X8}] {1}", crc.Result, fi.Name);
								if (FoundHashes.ContainsKey(crc.Result))
								{
									Console.WriteLine("--> COLLISION WITH: ", FoundHashes[crc.Result]);
									outf.WriteLine("--> COLLISION WITH: ", FoundHashes[crc.Result]);
								}
								Console.Out.Flush();
							}
						}
					}

					foreach (var fsi in mydiTemp.GetFileSystemInfos())
						fsi.Delete();
					mydiTemp.Delete();

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