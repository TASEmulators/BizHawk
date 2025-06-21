{ system ? builtins.currentSystem
, pkgs ? import (builtins.fetchTarball {
	url = "https://github.com/NixOS/nixpkgs/archive/24.05.tar.gz";
	sha256 = "1lr1h35prqkd1mkmzriwlpvxcb34kmhc9dnr48gkm8hh089hifmx";
}) { inherit system; }
, lib ? pkgs.lib
, stdenv ? pkgs.stdenvNoCC
# infrastructure
, buildDotnetModule ? pkgs.buildDotnetModule
, dpkg ? pkgs.dpkg
, fetchFromGitHub ? pkgs.fetchFromGitHub
, fetchFromGitLab ? pkgs.fetchFromGitLab
, fetchpatch ? pkgs.fetchpatch
, fetchzip ? pkgs.fetchzip
, makeDesktopItem ? pkgs.makeDesktopItem
, mkNugetDeps ? pkgs.mkNugetDeps
, runCommand ? pkgs.runCommand
, symlinkJoin ? pkgs.symlinkJoin
, writeShellScript ? pkgs.writeShellScript
, writeShellScriptBin ? pkgs.writeShellScriptBin
, writeText ? pkgs.writeText
# source
, hawkSourceInfoDevBuild ? let # called "dev build", but you could pass whatever branch and commit you want here
	version = "2.10.1-local"; # used in default value of `BIZHAWK_DATA_HOME`, which distinguishes parallel installs' config and other data
in {
	inherit version;
	src = builtins.path { path = ./.; name = "BizHawk-${version}"; }; # source derivation; did have filter here for speed, but it wasn't faster and it wasn't correct and it couldn't be made correct and I'm mad
}
# makedeps
, dotnet-sdk_8 ? pkgs.dotnet-sdk_8
, dotnet-sdk_6 ? pkgs.dotnet-sdk_6
, dotnet-sdk_5 ? let result = builtins.tryEval pkgs.dotnet-sdk_5; in if result.success
	then result.value
	else (import (fetchzip {
		url = "https://github.com/NixOS/nixpkgs/archive/a8f575995434695a10b574d35ca51b0f26ae9049.tar.gz"; # commit immediately before .NET 5 was removed
		hash = "sha512-3ysJjKK1lYV1r/zLohyuD1fiK+8TD3MMA3TrX9fb42nKqzfGGW62Aom7ltiyyxbVbBYOCXUy41Z5Y0j2VOxRKw==";
	}) { inherit system; }).dotnet-sdk_5
, git ? pkgs.gitMinimal # only when building from-CWD (`-local`)
# rundeps
, coreutils ? pkgs.coreutils
, gnome-themes-extra ? pkgs.gnome3.gnome-themes-extra
, gtk2-x11 ? pkgs.gtk2-x11
, kate ? pkgs.kate.overrideAttrs (oldAttrs: {
	patches = (oldAttrs.patches or []) ++ [ (fetchpatch {
		url = "https://invent.kde.org/utilities/kate/-/commit/9ddf4f0c9eb3c26a0ab33c862d2b161bcbdc6a6e.patch"; # Fix name of OmniSharp LSP binary
		hash = "sha256-a2KqoxuuVhfAQUJA3/yEQb1QCoa1JCvLz7BZZnSLnzI=";
	}) ];
})
, libgdiplus ? pkgs.libgdiplus
, libGL ? pkgs.libGL
, lua ? pkgs.lua54Packages.lua
, mono ? null
, nixGLChannel ? (pkgs.nixgl or import (fetchzip {
	url = "https://github.com/guibou/nixGL/archive/489d6b095ab9d289fe11af0219a9ff00fe87c7c5.tar.gz";
	hash = "sha512-GvV707ftLvE0MCTfMJb/M86S2Nxf3vai+HPwq0QvJylmMBwliqYx/nW8X2ja2ruOHzaw3MXXmAxjnv5MMUn07w==";
}) { inherit system; })
, nixGL ? nixGLChannel.auto.nixGLDefault
, omnisharp-roslyn ? pkgs.omnisharp-roslyn
#, nixVulkan ? nixGLChannel.auto.nixVulkanNvidia
, openal ? pkgs.openal
, SDL2 ? pkgs.SDL2
, udev ? pkgs.udev
, zstd ? pkgs.zstd
# other parameters
, buildConfig ? "Release" # "Debug"/"Release"
, debugPInvokes ? false # forwarded to Dist/launch-scripts.nix
, debugDotnetHostCrashes ? false # forwarded to Dist/launch-scripts.nix
, doCheck ? true # runs `Dist/BuildTest${buildConfig}.sh`
, extraDefines ? "" # added to `<DefineConstants/>`, so ';'-separated
, extraDotnetBuildFlags ? "" # currently passed to EVERY `dotnet build` and `dotnet test` invocation (and does not replace the flags for parallel compilation added by default)
, forNixOS ? true
, initConfig ? {} # forwarded to Dist/launch-scripts.nix (see docs there)
, profileManagedCalls ? false # forwarded to Dist/launch-scripts.nix
}: let
	isVersionAtLeast = lib.flip lib.versionAtLeast; # I stand by this being the more useful param order w.r.t. currying
	replaceDotWithUnderscore = s: lib.replaceStrings [ "." ] [ "_" ] s;
	/** you can't actually make hard links in the sandbox, so this just copies, and we'll rely on Nix' automatic deduping */
	hardLinkJoin =
		{ name
		, paths
		, preferLocalBuild ? true
		, allowSubstitutes ? false
		, __contentAddressed ? false
		}: runCommand name {
			inherit __contentAddressed allowSubstitutes paths preferLocalBuild;
			passAsFile = [ "paths" ];
		} ''
			mkdir -p $out
			for d in $(cat $pathsPath); do
				cd "$d"
				find . -type d -exec mkdir -p "$out/{}" \;
				for f in $(find . -type f); do cp -T "$(realpath "$f")" "$out/$f"; done
			done
		'';
	inherit (import Dist/historical.nix {
		inherit lib
			isVersionAtLeast replaceDotWithUnderscore
			fetchFromGitHub fetchFromGitLab mkNugetDeps
			dotnet-sdk_5 dotnet-sdk_6 dotnet-sdk_8;
	}) depsForHistoricalRelease populateHawkSourceInfo releaseArtifactInfos releaseFrags releaseTagSourceInfos;
	launchScriptsFor = bizhawkAssemblies: isManualLocalBuild: import Dist/launch-scripts.nix {
		inherit lib
			writeShellScript writeText
			bizhawkAssemblies nixGL
			debugPInvokes debugDotnetHostCrashes initConfig isManualLocalBuild profileManagedCalls;
		mkfifo = coreutils;
		mktemp = coreutils;
	};
	pp = import Dist/packages.nix {
		inherit lib stdenv
			populateHawkSourceInfo replaceDotWithUnderscore
			buildDotnetModule fetchpatch fetchzip hardLinkJoin launchScriptsFor makeDesktopItem
				releaseTagSourceInfos runCommand symlinkJoin writeShellScriptBin
			git
			gnome-themes-extra gtk2-x11 libgdiplus libGL lua openal SDL2 udev zstd
			buildConfig doCheck extraDefines extraDotnetBuildFlags;
		mono = lib.recursiveUpdate { meta.mainProgram = "mono"; } (if mono != null
			then mono # allow older Mono if set explicitly
			else if isVersionAtLeast "6.12.0.151" pkgs.mono.version
				then pkgs.mono
				else lib.trace "provided Mono too old, using Mono from Nixpkgs 23.05"
					(import (fetchzip {
						url = "https://github.com/NixOS/nixpkgs/archive/23.05.tar.gz";
						hash = "sha512-REPJ9fRKxTefvh1d25MloT4bXJIfxI+1EvfVWq644Tzv+nuq2BmiGMiBNmBkyN9UT5fl2tdjqGliye3gZGaIGg==";
					}) { inherit system; }).mono);
		monoBasic = fetchzip {
			url = "https://download.mono-project.com/repo/debian/pool/main/m/mono-basic/libmono-microsoft-visualbasic10.0-cil_4.7-0xamarin3+debian9b1_all.deb";
			nativeBuildInputs = [ dpkg ];
			hash = "sha512-bPXbsVrViHAJz6PWuryo9HA6Nlv0bNqgc72pNKM/MUQM7JTUcfM0VDUzkz8vzXSqp/nt2LlAOIqIsS5D5iBIvQ==";
			# tried and failed building from source, following https://aur.archlinux.org/cgit/aur.git/tree/PKGBUILD?h=mono-basic
		};
	};
	emuhawk-local = pp.buildEmuHawkInstallableFor {
		inherit forNixOS;
		hawkSourceInfo = hawkSourceInfoDevBuild;
	};
	fillTargetOSDifferences = hawkSourceInfo: lib.optionalAttrs forNixOS { needsLibGLVND = true; } // hawkSourceInfo; # don't like this, but the alternative is including `forNixOS` in `hawkSourceInfo` directly
	asmsFromReleaseArtifacts = lib.mapAttrs
		(_: a: pp.splitReleaseArtifact (a // { hawkSourceInfo = fillTargetOSDifferences a.hawkSourceInfo; }))
		releaseArtifactInfos;
	# the asms for from-CWD and latest release from-source are exposed below as `bizhawkAssemblies` and `bizhawkAssemblies-latest`, respectively
	# apart from that, no `asmsFromSource`, since if you're only after the assets you might as well use the release artifact
	releasesEmuHawkInstallables = lib.pipe releaseFrags [
		(builtins.map (versionFrag: [
			({
				name = "emuhawk-${versionFrag}";
				value = pp.buildEmuHawkInstallableFor {
					inherit forNixOS;
					hawkSourceInfo = releaseTagSourceInfos."info-${versionFrag}";
				};
			})
			({
				name = "emuhawk-${versionFrag}-bin";
				value = pp.buildEmuHawkInstallableFor {
					inherit forNixOS;
					bizhawkAssemblies = asmsFromReleaseArtifacts."bizhawkAssemblies-${versionFrag}-bin";
				};
			})
		]))
		lib.concatLists
		lib.listToAttrs
		(lib.filterAttrs (name: value: lib.hasSuffix "-bin" name
			|| isVersionAtLeast "2.6" value.hawkSourceInfo.version))
	];
	latestVersionFrag = lib.head releaseFrags;
	combined = let
		launchScriptsForLocalBuild = launchScriptsFor emuhawk-local.assemblies true;
	in (pp // asmsFromReleaseArtifacts // releasesEmuHawkInstallables // {
		inherit depsForHistoricalRelease populateHawkSourceInfo releaseTagSourceInfos launchScriptsForLocalBuild;
		bizhawkAssemblies = pp.buildAssembliesFor (fillTargetOSDifferences hawkSourceInfoDevBuild);
		"bizhawkAssemblies-${latestVersionFrag}" = pp.buildAssembliesFor
			(fillTargetOSDifferences releaseTagSourceInfos."info-${latestVersionFrag}");
		discohawk = pp.buildDiscoHawkInstallableFor {
			inherit forNixOS;
			hawkSourceInfo = hawkSourceInfoDevBuild;
		};
		"discohawk-${latestVersionFrag}" = pp.buildDiscoHawkInstallableFor {
			inherit forNixOS;
			hawkSourceInfo = releaseTagSourceInfos."info-${latestVersionFrag}";
		};
		"discohawk-${latestVersionFrag}-bin" = pp.buildDiscoHawkInstallableFor {
			inherit forNixOS;
			bizhawkAssemblies = asmsFromReleaseArtifacts."bizhawkAssemblies-${latestVersionFrag}-bin";
		};
		emuhawk = emuhawk-local;
		IDEs = {
			kate = [ kate omnisharp-roslyn ];
		};
		shellHook = drv: ''
			export BIZHAWKBUILD_HOME='${builtins.toString ./.}'
			export BIZHAWK_HOME="$BIZHAWKBUILD_HOME/output/"
			ldLibPath='${lib.makeLibraryPath drv.buildInputs}' # for running tests
			if [ -z "$LD_LIBRARY_PATH" ]; then
				export LD_LIBRARY_PATH="$ldLibPath"
			else
				export LD_LIBRARY_PATH="$LD_LIBRARY_PATH:$ldLibPath"
			fi
			alias discohawk-monort-local='${launchScriptsForLocalBuild.discohawk}'
			alias emuhawk-monort-local='${launchScriptsForLocalBuild.emuhawk}'
			case "$-" in *i*)
				pfx="$(realpath --relative-to="$PWD" "$BIZHAWKBUILD_HOME")/"
				if [ "$pfx" = "./" ]; then pfx=""; fi
				printf "%s\n%s\n" \
					"Run ''${pfx}Dist/Build{Debug,Release}.sh to build the solution. You may need to clean up with ''${pfx}Dist/CleanupBuildOutputDirs.sh." \
					"Once built, running {discohawk,emuhawk}-monort-local will pull from ''${pfx}output/* and use Mono from Nixpkgs."
				;;
			esac
		'';
	});
in combined // lib.listToAttrs (lib.concatLists (builtins.map
	(f: [
		{ name = f "latest-bin"; value = combined.${f "${latestVersionFrag}-bin"}; }
		{ name = f "latest"; value = combined.${f latestVersionFrag}; }
	])
	[ (s: "bizhawkAssemblies-${s}") (s: "emuhawk-${s}") (s: "discohawk-${s}") ]))
