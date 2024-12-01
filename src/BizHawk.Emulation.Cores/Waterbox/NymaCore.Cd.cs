using System.Runtime.InteropServices;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.Emulation.Cores.Waterbox
{
	abstract partial class NymaCore : IDriveLight
	{
		// this code was mostly copied from Saturnus, which it will replace soon(R)
		private static readonly DiscSectorReaderPolicy _diskPolicy = new DiscSectorReaderPolicy
		{
			DeinterleavedSubcode = false
		};
		private LibNymaCore.CDTOCCallback _cdTocCallback;
		private LibNymaCore.CDSectorCallback _cdSectorCallback;
		private Disc[] _disks;
		private DiscSectorReader[] _diskReaders;

		private static void SetupTOC(LibNymaCore.TOC t, DiscTOC tin)
		{
			// everything that's not commented, we're sure about
			t.FirstTrack = tin.FirstRecordedTrackNumber;
			t.LastTrack = tin.LastRecordedTrackNumber;
			t.DiskType = (int)tin.SessionFormat;
			for (int i = 0; i < 101; i++)
			{
				t.Tracks[i].Adr = tin.TOCItems[i].Exists ? 1 : 0; // ????
				t.Tracks[i].Lba = tin.TOCItems[i].LBA;
				t.Tracks[i].Control = (int)tin.TOCItems[i].Control;
				t.Tracks[i].Valid = tin.TOCItems[i].Exists ? 1 : 0;
			}
		}

		private void CDTOCCallback(int disk, IntPtr dest)
		{
			var toc = new LibNymaCore.TOC { Tracks = new LibNymaCore.TOC.Track[101] };
			SetupTOC(toc, _disks[disk].TOC);
			Marshal.StructureToPtr(toc, dest, false);
		}
		private void CDSectorCallback(int disk, int lba, IntPtr dest)
		{
			var buff = new byte[2448];
			_diskReaders[disk].ReadLBA_2448(lba, buff, 0);
			Marshal.Copy(buff, 0, dest, 2448);
			DriveLightOn = true;
		}

		public bool DriveLightEnabled => _disks?.Length > 0;
		public bool DriveLightOn { get; private set; }

		public string DriveLightIconDescription => "CD Drive Activity";
	}
}
