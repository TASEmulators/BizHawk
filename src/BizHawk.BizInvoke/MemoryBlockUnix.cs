using System;
using System.IO;

using static BizHawk.BizInvoke.POSIXLibC;

namespace BizHawk.BizInvoke
{
	public sealed class MemoryBlockUnix : MemoryBlock
	{
		/// <summary>handle returned by <see cref="memfd_create"/></summary>
		private int _fd;

		/// <inheritdoc cref="MemoryBlock(ulong,ulong)"/>
		/// <exception cref="InvalidOperationException">failed to get file descriptor (never thrown as <see cref="NotImplementedException"/> is thrown first)</exception>
		/// <exception cref="NotImplementedException">always</exception>
		public MemoryBlockUnix(ulong start, ulong size) : base(start, size)
		{
			throw new NotImplementedException($"{nameof(MemoryBlockUnix)} ctor");
			#if false
			_fd = memfd_create("MemoryBlockUnix", 0);
			if (_fd == -1) throw new InvalidOperationException($"{nameof(memfd_create)}() returned -1");
			#endif
		}

		/// <exception cref="InvalidOperationException"><see cref="MemoryBlock.Active"/> is <see langword="true"/> or failed to map memory</exception>
		public override void Activate()
		{
			if (Active) throw new InvalidOperationException("Already active");

			var ptr = mmap(Z.US(Start), Z.UU(Size), MemoryProtection.Read | MemoryProtection.Write | MemoryProtection.Execute, 16, _fd, IntPtr.Zero);
			if (ptr != Z.US(Start)) throw new InvalidOperationException($"{nameof(mmap)}() returned NULL or the wrong pointer");

			ProtectAll();
			Active = true;
		}

		/// <exception cref="InvalidOperationException"><see cref="MemoryBlock.Active"/> is <see langword="false"/> or failed to unmap memory</exception>
		public override void Deactivate()
		{
			if (!Active) throw new InvalidOperationException("Not active");

			var exitCode = munmap(Z.US(Start), Z.UU(Size));
			if (exitCode != 0) throw new InvalidOperationException($"{nameof(munmap)}() returned {exitCode}");

			Active = false;
		}

		/// <exception cref="InvalidOperationException"><see cref="MemoryBlock.Active"/> is <see langword="false"/> or failed to make memory read-only</exception>
		public override byte[] FullHash()
		{
			if (!Active) throw new InvalidOperationException("Not active");

			// temporarily switch the entire block to `R`
			var exitCode = mprotect(Z.US(Start), Z.UU(Size), MemoryProtection.Read);
			if (exitCode != 0) throw new InvalidOperationException($"{nameof(mprotect)}() returned {exitCode}!");

			var ret = WaterboxUtils.Hash(GetStream(Start, Size, false));
			ProtectAll();
			return ret;
		}

		/// <exception cref="InvalidOperationException">failed to protect memory</exception>
		public override void Protect(ulong start, ulong length, Protection prot)
		{
			if (length == 0) return;

			var pstart = GetPage(start);
			var pend = GetPage(start + length - 1);
			for (var i = pstart; i <= pend; i++) _pageData[i] = prot; // also store the value for later use
			if (!Active) return; // it's legal to call this method if we're not active; the information is just saved for the next activation

			var computedStart = WaterboxUtils.AlignDown(start);
			var protEnum = prot.ToMemoryProtection();
			var exitCode = mprotect(
				Z.US(computedStart),
				Z.UU(WaterboxUtils.AlignUp(start + length) - computedStart),
				protEnum
			);
			if (exitCode != 0) throw new InvalidOperationException($"{nameof(mprotect)}() returned {exitCode}!");
		}

		protected override void ProtectAll()
		{
			var ps = 0;
			for (var i = 0; i < _pageData.Length; i++)
			{
				if (i == _pageData.Length - 1 || _pageData[i] != _pageData[i + 1])
				{
					var protEnum = _pageData[i].ToMemoryProtection();
					var zstart = GetStartAddr(ps);
					var exitCode = mprotect(
						Z.US(zstart),
						Z.UU(GetStartAddr(i + 1) - zstart),
						protEnum
					);
					if (exitCode != 0) throw new InvalidOperationException($"{nameof(mprotect)}() returned {exitCode}!");

					ps = i + 1;
				}
			}
		}

		/// <exception cref="InvalidOperationException">snapshot already taken, <see cref="MemoryBlock.Active"/> is <see langword="false"/>, or failed to make memory read-only</exception>
		public override void SaveXorSnapshot()
		{
			if (_snapshot != null) throw new InvalidOperationException("Snapshot already taken");
			if (!Active) throw new InvalidOperationException("Not active");

			// temporarily switch the entire block to `R`: in case some areas are unreadable, we don't want that to complicate things
			var exitCode = mprotect(Z.US(Start), Z.UU(Size), MemoryProtection.Read);
			if (exitCode != 0) throw new InvalidOperationException($"{nameof(mprotect)}() returned {exitCode}!");

			_snapshot = new byte[Size];
			GetStream(Start, Size, false).CopyTo(new MemoryStream(_snapshot, true));
			XorHash = WaterboxUtils.Hash(_snapshot);
			ProtectAll();
		}

		public override void Dispose(bool disposing)
		{
			if (_fd == 0) return;

			if (Active) Deactivate();
			close(_fd);
			_fd = -1;
		}

		~MemoryBlockUnix()
		{
			Dispose(false);
		}
	}
}
