using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	public partial class NDS : ISaveRam
	{
		private readonly int PublicSavSize, PrivateSavSize, BannerSavSize;
		private readonly int DSiWareSaveLength;

		public new bool SaveRamModified => IsDSiWare ? DSiWareSaveLength != 0 : _core.SaveRamIsDirty();

		public new byte[] CloneSaveRam()
		{
			if (IsDSiWare)
			{
				if (DSiWareSaveLength == 0)
				{
					return null;
				}

				_exe.AddTransientFile([ ], "public.sav");
				_exe.AddTransientFile([ ], "private.sav");
				_exe.AddTransientFile([ ], "banner.sav");
				_core.ExportDSiWareSavs(_console, DSiTitleId.Lower);

				var publicSav = _exe.RemoveTransientFile("public.sav");
				var privateSav = _exe.RemoveTransientFile("private.sav");
				var bannerSav = _exe.RemoveTransientFile("banner.sav");
				if (publicSav.Length != PublicSavSize || privateSav.Length != PrivateSavSize || bannerSav.Length != BannerSavSize)
				{
					throw new InvalidOperationException("Unexpected size difference in DSiWare sav files!");
				}

				var ret = new byte[DSiWareSaveLength];
				publicSav.AsSpan().CopyTo(ret.AsSpan().Slice(0, PublicSavSize));
				privateSav.AsSpan().CopyTo(ret.AsSpan().Slice(PublicSavSize, PrivateSavSize));
				bannerSav.AsSpan().CopyTo(ret.AsSpan().Slice(PublicSavSize + PrivateSavSize, BannerSavSize));
				return ret;
			}

			var length = _core.GetSaveRamLength(_console);

			if (length > 0)
			{
				var ret = new byte[length];
				_core.GetSaveRam(_console, ret);
				return ret;
			}

			return null;
		}

		public new void StoreSaveRam(byte[] data)
		{
			if (IsDSiWare)
			{
				if (data.Length == DSiWareSaveLength)
				{
					if (PublicSavSize > 0) _exe.AddReadonlyFile(data.AsSpan().Slice(0, PublicSavSize).ToArray(), "public.sav");
					if (PrivateSavSize > 0) _exe.AddReadonlyFile(data.AsSpan().Slice(PublicSavSize, PrivateSavSize).ToArray(), "private.sav");
					if (BannerSavSize > 0) _exe.AddReadonlyFile(data.AsSpan().Slice(PublicSavSize + PrivateSavSize, BannerSavSize).ToArray(), "banner.sav");

					_core.ImportDSiWareSavs(_console, DSiTitleId.Lower);

					if (PublicSavSize > 0) _exe.RemoveReadonlyFile("public.sav");
					if (PrivateSavSize > 0) _exe.RemoveReadonlyFile("private.sav");
					if (BannerSavSize > 0) _exe.RemoveReadonlyFile("banner.sav");
				}
			}
			else if (data.Length > 0)
			{
				_core.PutSaveRam(_console, data, (uint)data.Length);
			}
		}
	}
}