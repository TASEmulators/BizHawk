# THIS IS A WORK IN PROGRESS!

{ pkgs ? import (fetchTarball "https://github.com/NixOS/nixpkgs/archive/2fa862644fc15ecb525eb8cd0a60276f1c340c7c.tar.gz") {}
# infrastructure
, lib ? pkgs.lib
, stdenv ? pkgs.stdenvNoCC
, buildDotnetModule ? pkgs.buildDotnetModule
, fetchFromGitHub ? pkgs.fetchFromGitHub
#, makeDesktopItem ? pkgs.makeDesktopItem
, makeWrapper ? pkgs.makeWrapper
, writeShellScriptBin ? pkgs.writeShellScriptBin
, writeText ? pkgs.writeText
# source
, useCWDAsSource ? false
, hawkVersion ? if useCWDAsSource then "2.7.1-local" else "2.7" # used to distinguish parallel installs' config and other data
, hawkSource ? if useCWDAsSource
	then builtins.path {
		path = ./.;
		name = "BizHawk-${hawkVersion}";
		filter = let
			denyList = [ ".idea" "ExternalCoreProjects" "ExternalProjects" "ExternalToolProjects" "libHawk" "libmupen64plus" "LibretroBridge" "LuaInterface" "lynx" "psx" "quicknes" "submodules" "waterbox" "wonderswan" ];
		in path: type: type == "regular" || (type == "directory" && !builtins.elem (baseNameOf path) denyList);
	}
	else fetchFromGitHub {
		owner = "TASEmulators";
		repo = "BizHawk";
		rev = "dbaf2595625f79093eeec37d2d4a7a9a4d37f370";
		hash = "sha256-2M9+p5NYCJQlItcyLxU7bY6JC5/lacM5jfZoILGkHrU=";
		leaveDotGit = true;
	}
# makedeps
, git ? pkgs.git
, p7zip ? pkgs.p7zip
# rundeps for NixOS hosts
#, gtk2-x11 ? pkgs.gtk2-x11
# rundeps for all Linux hosts
, mesa ? pkgs.mesa
, mono ? null
, openal ? pkgs.openal
, uname ? stdenv
# other parameters
, buildConfig ? "Release" # "Debug"/"Release"
, debugPInvokes ? false
, doCheck ? false # runs `Dist/BuildTest${buildConfig}.sh`
, forNixOS ? false
, initConfig ? {} # pretend this is JSON; the following env. vars will be substituted by the wrapper script (if surrounded by double-percent e.g. `%%BIZHAWK_DATA_HOME%%`): `BIZHAWK_DATA_HOME`
}:
let
	commentUnless = b: lib.optionalString (!b) "# ";
	versionAtLeast = reqVer: drv: builtins.compareVersions reqVer drv.version <= 0;
	monoFinal = if mono != null
		then mono
		else if versionAtLeast "6.12.0.151" pkgs.mono
			then pkgs.mono
			else pkgs.callPackage ./mono-6.12.0.151.nix {};
	bizhawk = buildDotnetModule rec {
		pname = "BizHawk";
		version = hawkVersion;
		src = hawkSource;
		nativeBuildInputs = [ git p7zip ];
		buildInputs = [ mesa monoFinal openal uname ];# ++ lib.optionals (forNixOS) [ gtk2-x11 ];
		projectFile = "BizHawk.sln";
		nugetDeps = ./deps.nix;
		extraDotnetBuildFlags = "-maxcpucount:$NIX_BUILD_CORES -p:BuildInParallel=true --no-restore";
		buildPhase = ''
			${commentUnless useCWDAsSource}cd src/BizHawk.Version
			${commentUnless useCWDAsSource}dotnet build ${extraDotnetBuildFlags}
			${commentUnless useCWDAsSource}cd ../..
			Dist/Build${buildConfig}.sh ${extraDotnetBuildFlags}
			printf "Nix" >output/dll/custombuild.txt
			Dist/Package.sh linux-x64
			rm packaged_output/EmuHawkMono.sh # replaced w/ below script(s)
		'';
		inherit doCheck;
		checkPhase = ''
			export GITLAB_CI=1 # pretend to be in GitLab CI -- platform-specific tests don't run in CI because they assume an Arch filesystem (on Linux hosts)
			# from 2.7.1, use standard -p:ContinuousIntegrationBuild=true instead
			Dist/BuildTest${buildConfig}.sh ${extraDotnetBuildFlags}

			# can't build w/ extra Analyzers, it fails to restore :(
#			Dist/Build${buildConfig}.sh -p:MachineRunAnalyzersDuringBuild=true ${extraDotnetBuildFlags}
		'';
		installPhase = ''
			mkdir -p $out
			cp -aTv packaged_output $out
		'';
		dontPatchELF = true;
	};
	initConfigFile = writeText "config.json" (builtins.toJSON ({
		LastWrittenFrom = if builtins.length (builtins.splitVersion hawkVersion) < 3 then "${hawkVersion}.0" else hawkVersion;
		PathEntries = {
			Paths = [
				({ "System" = "Global_NULL"; Type = "Base"; Path = "%%BIZHAWK_DATA_HOME%%"; } // lib.optionalAttrs (!versionAtLeast "2.7.1" bizhawk)  { "Ordinal" = 1; })
			];
		};
	} // initConfig));
	wrapperScript = writeShellScriptBin "emuhawk-wrapper" ''
		set -e

		if [ ! -e "$BIZHAWK_HOME/EmuHawk.exe" ]; then
			printf "no such file: %s\n" "$BIZHAWK_HOME/EmuHawk.exe"
			exit 1
		fi

		if [ "$XDG_DATA_HOME" ]; then
			BIZHAWK_DATA_HOME="$XDG_DATA_HOME"
		else
			BIZHAWK_DATA_HOME="$HOME/.local/share"
		fi
		BIZHAWK_DATA_HOME="$BIZHAWK_DATA_HOME/emuhawk-monort-${hawkVersion}"
		mkdir -p "$BIZHAWK_DATA_HOME"
		cd "$BIZHAWK_DATA_HOME"

		if [ ! -e config.json ]; then
			cat ${initConfigFile} >config.json # cp kept the perms as 444 -- don't @ me
			sed -i "s@%%BIZHAWK_DATA_HOME%%@$BIZHAWK_DATA_HOME@g" config.json
		fi

		export LD_LIBRARY_PATH=$BIZHAWK_HOME/dll:$BIZHAWK_GLHACKDIR:${lib.makeLibraryPath [ openal ]}
		${commentUnless debugPInvokes}MONO_LOG_LEVEL=debug MONO_LOG_MASK=dll
		if [ "$1" = "--mono-no-redirect" ]; then
			shift
			printf "(received --mono-no-redirect, stdout was not captured)\n" >EmuHawkMono_laststdout.txt
			exec ${monoFinal}/bin/mono $BIZHAWK_HOME/EmuHawk.exe --config=config.json "$@"
		else
			exec ${monoFinal}/bin/mono $BIZHAWK_HOME/EmuHawk.exe --config=config.json "$@" >EmuHawkMono_laststdout.txt
		fi
	'';
	wrapperScriptNonNixOS = writeShellScriptBin "emuhawk-wrapper-non-nixos" ''
		set -e

		if [ "$XDG_STATE_HOME" ]; then
			BIZHAWK_GLHACKDIR="$XDG_STATE_HOME"
		else
			BIZHAWK_GLHACKDIR="$HOME/.local/state"
		fi
		export BIZHAWK_GLHACKDIR="$BIZHAWK_GLHACKDIR/emuhawk-monort-${hawkVersion}-non-nixos"
		mkdir -p "$BIZHAWK_GLHACKDIR"
		if [ ! -e "$BIZHAWK_GLHACKDIR/libGLX_indirect.so.0" ]; then
			ln -fsvT "${lib.getOutput "drivers" mesa}/lib/libGLX_mesa.so.0" "$BIZHAWK_GLHACKDIR/libGLX_indirect.so.0"
		fi
		# collect links to certain GL libs (and their deps) from host, added to LD_LIBRARY_PATH without polluting it with all libs from host
		for l in libbsd.so.0 libdrm_amdgpu.so.1 libdrm_nouveau.so.2 libdrm_radeon.so.1 libedit.so.0 libedit.so.2 libelf.so.1 libffi.so.7 libGLdispatch.so.0 libicudata.so.69 libicuuc.so.69 libLLVM-11.so libLLVM-12.so.1 liblzma.so.5 libncursesw.so.6 libsensors.so.5 libstdc++.so.6 libtinfo.so.6 libvulkan.so.1 libxml2.so.2 libz.so.1 libzstd.so.1; do
			if [ -e "$BIZHAWK_GLHACKDIR/$l" ]; then continue; fi
			# else it's either a broken link or it doesn't exist, we use ln -f to cover both
			for d in /usr/lib /usr/lib/x86_64-linux-gnu /usr/lib64 /lib /lib64; do
				if [ -e "$d/$l" ]; then
					ln -fsvT "$d/$l" "$BIZHAWK_GLHACKDIR/$l"
					break
				fi
			done
		done

		for d in /usr/lib/dri /usr/lib/x86_64-linux-gnu/dri; do
			if [ -e "$d" ]; then
				export LIBGL_DRIVERS_PATH=$d
				break
			fi
		done

		exec ${wrapperScript}/bin/emuhawk-wrapper "$@"
	'';
in {
	bizhawkAssemblies = bizhawk;
	emuhawk = stdenv.mkDerivation rec {
		pname = "emuhawk-monort";
		version = hawkVersion;
		nativeBuildInputs = [ makeWrapper ];
		buildInputs = [ bizhawk ];
		# there must be a helper for this somewhere...
		dontUnpack = true;
		dontPatch = true;
		dontConfigure = true;
		dontBuild = true;
		installPhase = ''
			mkdir -p $out/bin
			makeWrapper ${if forNixOS then "${wrapperScript}/bin/emuhawk-wrapper" else "${wrapperScriptNonNixOS}/bin/emuhawk-wrapper-non-nixos"} $out/bin/${pname}-${version} \
				--set BIZHAWK_HOME ${bizhawk}
		'';
		dontFixup = true;
#		desktopItems = [ (makeDesktopItem rec {
#			name = "emuhawk-monort-${version}"; # actually filename
#			exec = "${pname}-monort-${version}";
#			desktopName = "EmuHawk (Mono Runtime)"; # actually Name
#		}) ];
	};
	mono = monoFinal;
}
