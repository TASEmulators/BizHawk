{ system ? builtins.currentSystem
, pkgs ? (import ../../Dist/nixpkgs.nix).nixpkgs-26_05 system
, lib ? pkgs.lib
, mkShell ? pkgs.mkShell
, dotnet-sdk_10 ? pkgs.dotnet-sdk_10
}:
mkShell {
	packages = [ dotnet-sdk_10 ];
	shellHook = ''
		if [ -z "$BIZHAWKBUILD_WRAPPER_SCRIPT" ]; then
			printf "Getting EmuHawk wrapper script via Nix (set env. var. BIZHAWKBUILD_WRAPPER_SCRIPT to override, keep in mind it must be a filepath and not a shell alias)...\n" >&2
			BIZHAWKBUILD_WRAPPER_SCRIPT="$(nix-build --pure --no-out-link ../.. -A emuhawk)/bin"
			export BIZHAWKBUILD_WRAPPER_SCRIPT="$BIZHAWKBUILD_WRAPPER_SCRIPT/$(cd "$BIZHAWKBUILD_WRAPPER_SCRIPT"; ls emuhawk-*)"
		fi
		if [ -z "$BIZHAWKBUILD_LIBRETRO_CORES" ]; then
			printf "Collecting Libretro core binaries via Nix (set env. var. BIZHAWKBUILD_LIBRETRO_CORES to override)...\n" >&2
			export BIZHAWKBUILD_LIBRETRO_CORES="$(NIXPKGS_ALLOW_UNFREE=1 nix-build --pure --no-out-link shell.nix -A libretroCores)/lib/retroarch/cores"
		fi
		printf "Starting wizard\n" >&2
		exec ./tester_wizard.app.cs update-dev
		exit 0
	'';
	passthru.libretroCores = import ../../Dist/packages-libretro.nix {
		inherit system;
		filterFunc = { drv, licenses, defaultLicenseAllowlist }:
			!(lib.elem drv.pname [ "libretro-mame" "libretro-mame2003" "libretro-mame2016" ]); # MAME doesn't build ¯\_(ツ)_/¯ https://github.com/NixOS/nixpkgs/issues/528815
	};
}
