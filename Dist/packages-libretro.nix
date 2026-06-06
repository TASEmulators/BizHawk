{ system ? builtins.currentSystem
, pkgs ? (import ./nixpkgs.nix).nixpkgs-26_05 system
, lib ? pkgs.lib
, symlinkJoin ? pkgs.symlinkJoin
, libretro ? pkgs.libretro
, defaultLicenseAllowlist ? lib.attrValues {
	inherit (lib.licenses) bsd2 bsd3 gpl2Plus gpl3Plus lgpl21Plus mit mpl20 unlicense zlib; # those which can be relicensed to GPL-3.0-or-later
	inherit (lib.licenses) gpl3Only; # those which can be relicensed to GPL-3.0-only
}
, filterFunc ? { drv, licenses, defaultLicenseAllowlist }: (lib.subtractLists defaultLicenseAllowlist licenses) == []
	&& !(lib.elem drv.pname [ "libretro-mame" "libretro-mame2016" ]) # MAME doesn't build ¯\_(ツ)_/¯ https://github.com/NixOS/nixpkgs/issues/528815
}:
symlinkJoin {
	name = "bizhawk-unmanaged-deps-libretro";
	paths = lib.pipe (lib.attrValues libretro) [
		(lib.filter (drv: lib.isDerivation drv && filterFunc {
			inherit defaultLicenseAllowlist drv;
			licenses = lib.toList (drv.meta.license or lib.licenses.unfree);
		}))
		(builtins.map (drv: drv.overrideAttrs (oldAttrs: if oldAttrs ? includeRetroArch # has split derivation https://github.com/NixOS/nixpkgs/pull/527255
			then { includeRetroArch = false; }
			else { installPhase = ''
				runHook preInstall
				install -Dt "${builtins.placeholder "out"}${oldAttrs.passthru.libretroCore}" *${drv.stdenv.hostPlatform.extensions.sharedLibrary}
				runHook postInstall
			''; })))
	];
}
