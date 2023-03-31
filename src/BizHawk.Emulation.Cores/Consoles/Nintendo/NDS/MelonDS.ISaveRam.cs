using System;
using System.IO;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	public partial class NDS : ISaveRam
	{
		public new bool SaveRamModified => IsDSiWare || _core.SaveRamIsDirty();

		public new byte[] CloneSaveRam()
		{
			if (IsDSiWare)
			{
				_core.DSiWareSavsLength(DSiTitleId.Lower, out var publicSavSize, out var privateSavSize, out var bannerSavSize);
				if (publicSavSize + privateSavSize + bannerSavSize == 0) return null;
				_exe.AddTransientFile(Array.Empty<byte>(), "public.sav");
				_exe.AddTransientFile(Array.Empty<byte>(), "private.sav");
				_exe.AddTransientFile(Array.Empty<byte>(), "banner.sav");
				_core.ExportDSiWareSavs(DSiTitleId.Lower);
				var publicSav = _exe.RemoveTransientFile("public.sav");
				var privateSav = _exe.RemoveTransientFile("private.sav");
				var bannerSav = _exe.RemoveTransientFile("banner.sav");
				if (publicSav.Length != publicSavSize || privateSav.Length != privateSavSize ||
					bannerSav.Length != bannerSavSize)
				{
					throw new InvalidOperationException("Unexpected size difference in DSiWare sav files!");
				}
				var ret = new byte[publicSavSize + privateSavSize + bannerSavSize];
				publicSav.AsSpan().CopyTo(ret.AsSpan().Slice(0, publicSavSize));
				privateSav.AsSpan().CopyTo(ret.AsSpan().Slice(publicSavSize, privateSavSize));
				bannerSav.AsSpan().CopyTo(ret.AsSpan().Slice(publicSavSize + privateSavSize, bannerSavSize));
				return ret;
			}

			var length = _core.GetSaveRamLength();

			if (length > 0)
			{
				var ret = new byte[length];
				_core.GetSaveRam(ret);
				return ret;
			}

			return null;
		}

		public new void StoreSaveRam(byte[] data)
		{
			if (IsDSiWare)
			{
				_core.DSiWareSavsLength(DSiTitleId.Lower, out var publicSavSize, out var privateSavSize, out var bannerSavSize);
				if (data.Length == publicSavSize + privateSavSize + bannerSavSize)
				{
					if (publicSavSize > 0) _exe.AddReadonlyFile(data.AsSpan().Slice(0, publicSavSize).ToArray(), "public.sav");
					if (privateSavSize > 0) _exe.AddReadonlyFile(data.AsSpan().Slice(publicSavSize, privateSavSize).ToArray(), "private.sav");
					if (bannerSavSize > 0) _exe.AddReadonlyFile(data.AsSpan().Slice(publicSavSize + privateSavSize, bannerSavSize).ToArray(), "banner.sav");
					_core.ImportDSiWareSavs(DSiTitleId.Lower);
					if (publicSavSize > 0) _exe.RemoveReadonlyFile("public.sav");
					if (privateSavSize > 0) _exe.RemoveReadonlyFile("private.sav");
					if (bannerSavSize > 0) _exe.RemoveReadonlyFile("banner.sav");
				}
			}
			else if (data.Length > 0)
			{
				_core.PutSaveRam(data, (uint)data.Length);
			}
		}
	}
}
