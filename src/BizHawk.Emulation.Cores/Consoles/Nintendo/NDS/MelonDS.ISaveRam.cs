using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	public partial class NDS : ISaveRam
	{
		private readonly int PublicSavSize, PrivateSavSize, BannerSavSize;
		private readonly int DSiWareSaveLength;

		public override bool SaveRamModified => IsDSiWare ? DSiWareSaveLength != 0 : _core.SaveRamIsDirty(_console);

		public override byte[] CloneSaveRam(bool clearDirty)
		{
			if (IsDSiWare)
			{
				if (DSiWareSaveLength == 0)
				{
					throw new InvalidOperationException("Core currently has no SRAM and should not be providing ISaveRam service.");
				}

				_exe.AddTransientFile([ ], "public.sav");
				_exe.AddTransientFile([ ], "private.sav");
				_exe.AddTransientFile([ ], "banner.sav");
				_core.ExportDSiWareSavs(_console, DSiTitleId.Full);

				var publicSav = _exe.RemoveTransientFile("public.sav");
				var privateSav = _exe.RemoveTransientFile("private.sav");
				var bannerSav = _exe.RemoveTransientFile("banner.sav");
				if (publicSav.Length != PublicSavSize || privateSav.Length != PrivateSavSize || bannerSav.Length != BannerSavSize)
				{
					throw new InvalidOperationException("Unexpected size difference in DSiWare sav files!");
				}

				var ret = new byte[DSiWareSaveLength];
				publicSav.CopyTo(ret.AsSpan());
				privateSav.CopyTo(ret.AsSpan(start: PublicSavSize));
				bannerSav.CopyTo(ret.AsSpan(start: PublicSavSize + PrivateSavSize));
				return ret;
			}

			var length = _core.GetSaveRamLength(_console);

			if (length > 0)
			{
				var ret = new byte[length];
				_core.GetSaveRam(_console, ret, clearDirty);
				return ret;
			}

			throw new InvalidOperationException("Core currently has no SRAM and should not be providing ISaveRam service.");
		}

		public override void StoreSaveRam(byte[] data)
		{
			if (IsDSiWare)
			{
				if (DSiWareSaveLength == 0) throw new InvalidOperationException("Core currently has no SRAM and should not be providing ISaveRam service.");
				if (data.Length == DSiWareSaveLength)
				{
					if (PublicSavSize > 0) _exe.AddReadonlyFile(data.AsSpan(0, PublicSavSize).ToArray(), "public.sav");
					if (PrivateSavSize > 0) _exe.AddReadonlyFile(data.AsSpan(PublicSavSize, PrivateSavSize).ToArray(), "private.sav");
					if (BannerSavSize > 0) _exe.AddReadonlyFile(data.AsSpan(PublicSavSize + PrivateSavSize, BannerSavSize).ToArray(), "banner.sav");

					_core.ImportDSiWareSavs(_console, DSiTitleId.Full);

					if (PublicSavSize > 0) _exe.RemoveReadonlyFile("public.sav");
					if (PrivateSavSize > 0) _exe.RemoveReadonlyFile("private.sav");
					if (BannerSavSize > 0) _exe.RemoveReadonlyFile("banner.sav");
				}
				else
				{
					throw new InvalidOperationException("Incorrect sram size.");
				}
			}
			else if (data.Length > 0)
			{
				_core.PutSaveRam(_console, data, (uint)data.Length);
			}
		}
	}
}
