# THIS IS A WORK IN PROGRESS!

{ pkgs ? import (fetchTarball "https://github.com/NixOS/nixpkgs/archive/2fa862644fc15ecb525eb8cd0a60276f1c340c7c.tar.gz") {}
# infrastructure
, stdenv ? pkgs.stdenvNoCC
, buildDotnetModule ? pkgs.buildDotnetModule
, fetchFromGitHub ? pkgs.fetchFromGitHub
#, makeDesktopItem ? pkgs.makeDesktopItem
, makeWrapper ? pkgs.makeWrapper
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
		hash = "sha256-AQhnBy8lSiuFqA6I7lk6M1u3osAJEoEMELGDgGC/aII="; # changes randomly? maybe when submodules are changed? --yoshi
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
, debugPInvokes ? false # forwarded to Dist/wrapper-scripts.nix
, doCheck ? false # runs `Dist/BuildTest${buildConfig}.sh`
, forNixOS ? false
, initConfig ? {} # forwarded to Dist/wrapper-scripts.nix (see docs there)
}:
let
	lib = pkgs.lib;
	commentUnless = b: lib.optionalString (!b) "# ";
	versionAtLeast = exVer: acVer: builtins.compareVersions exVer acVer <= 0;
	monoFinal = if mono != null
		then mono
		else if versionAtLeast "6.12.0.151" pkgs.mono.version
			then pkgs.mono
			else pkgs.callPackage Dist/mono-6.12.0.151.nix {}; # not actually reproducible :( https://github.com/NixOS/nixpkgs/issues/143110#issuecomment-984251253
	bizhawk = buildDotnetModule rec {
		pname = "BizHawk";
		version = hawkVersion;
		src = hawkSource;
		nativeBuildInputs = [ git p7zip ];
		buildInputs = [ mesa monoFinal openal uname ];# ++ lib.optionals (forNixOS) [ gtk2-x11 ];
		projectFile = "BizHawk.sln";
		nugetDeps = Dist/deps.nix;
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
	wrapperScripts = import Dist/wrapper-scripts.nix {
		inherit (pkgs) lib writeShellScriptBin writeText;
		inherit commentUnless versionAtLeast mesa openal debugPInvokes hawkVersion initConfig;
		mono = monoFinal;
	};
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
			makeWrapper ${if forNixOS then "${wrapperScripts.wrapperScript}/bin/emuhawk-wrapper" else "${wrapperScripts.wrapperScriptNonNixOS}/bin/emuhawk-wrapper-non-nixos"} $out/bin/${pname}-${version} \
				--set BIZHAWK_HOME ${bizhawk}
		'';
		dontFixup = true;
#		desktopItems = [ (makeDesktopItem rec {
#			name = "emuhawk-monort-${version}"; # actually filename
#			exec = "${pname}-monort-${version}";
#			desktopName = "EmuHawk (Mono Runtime)"; # actually Name
#		}) ];
	};
	emuhawkWrapperScriptNonNixOS = wrapperScripts.wrapperScriptNonNixOS;
	mono = monoFinal;
}
