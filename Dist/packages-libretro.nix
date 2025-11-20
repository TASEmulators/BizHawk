{ system ? builtins.currentSystem
, pkgs ? (import Dist/nixpkgs.nix).nixpkgs-24_05 system
, lib ? pkgs.lib
, symlinkJoin ? pkgs.symlinkJoin
, libretro ? pkgs.libretro
, defaultLicenseAllowlist ? lib.attrValues {
	inherit (lib.licenses) bsd2 bsd3 gpl2Plus gpl3Plus lgpl21Plus mit mpl20 unlicense zlib; # those which can be relicensed to GPL-3.0-or-later
	inherit (lib.licenses) gpl3Only; # those which can be relicensed to GPL-3.0-only
}
, filterFunc ? { drv, licenses, defaultLicenseAllowlist }: (lib.subtractLists defaultLicenseAllowlist licenses) == []
}:
symlinkJoin {
	name = "bizhawk-unmanaged-deps-libretro";
	paths = lib.filter (drv: lib.isDerivation drv && filterFunc {
		inherit defaultLicenseAllowlist drv;
		licenses = lib.toList (drv.meta.license or lib.licenses.unfree);
	}) (lib.attrValues libretro);
}
