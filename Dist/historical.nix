{ lib
# infrastructure
, isVersionAtLeast
, replaceDotWithUnderscore
, fetchFromGitHub
, fetchFromGitLab
, mkNugetDeps
# makedeps
, dotnet-sdk_5
, dotnet-sdk_6
, dotnet-sdk_8
}: let
	/**
	 * updating? make sure to hit the rest of this file, the hand-written deps `/Dist/deps-historical.nix`,
	 * the shell script for CI `/Dist/nix_expr_check_attrs.sh`, and the docs `/Dist/nix_expr_usage_docs.md`
	 */
	releases = [
		"2.9.1" "2.9" "2.8" "2.7" "2.6.3" "2.6.2" "2.6.1" "2.6"
		"2.5.2" "2.5.1" "2.5" "2.4.2" "2.4.1" "2.4" "2.3.3" "2.3.2"
	];
	releaseCount = lib.length releases;
	releaseFrags = builtins.map replaceDotWithUnderscore releases;
	releaseOffsetLookup = lib.listToAttrs (lib.imap0 (lib.flip lib.nameValuePair) releaseFrags);
	depsForHistoricalRelease = releaseFrag: fetchNuGet: let
		file = import ./deps-historical.nix;
		windows = builtins.map (s: file."since-${s}" or []) releaseFrags ++ builtins.map (s: file."until-${s}" or []) releaseFrags;
		extras = builtins.map (s: file."only-${s}" or []) releaseFrags;
		i = releaseOffsetLookup.${releaseFrag};
	in builtins.map fetchNuGet (lib.elemAt extras i ++ lib.concatLists (lib.sublist i releaseCount windows));
	releaseTagSourceInfos = let
		f = { hashPostPatching, rev, version, postFetch ? "", dotnet-sdk ? null }: let
			shortHash = lib.substring 0 9 rev;
			fetcherArgs = {
				inherit rev;
				repo = "BizHawk";
				hash = hashPostPatching;
				postFetch = ''
					shortHash=${shortHash}
				'' + postFetch;
			};
		in (lib.optionalAttrs (dotnet-sdk != null) { inherit dotnet-sdk; }) // {
			inherit shortHash version;
			src = fetchFromGitHub (fetcherArgs // { owner = "TASEmulators"; });
			#TODO can use `srcs` w/ `buildDotnetModule`?
			# verified hashes match for 2.7 through 2.9, but 2.9.1 rev (745efb1dd) was missing (force-push nonsense IIRC)
			src1 = fetchFromGitLab (fetcherArgs // { owner = "TASVideos"; });
			nugetDeps = let
				releaseFrag = replaceDotWithUnderscore version;
			in mkNugetDeps {
				name = "BizHawk-${releaseFrag}-deps";
				nugetDeps = { fetchNuGet }: depsForHistoricalRelease releaseFrag fetchNuGet;
			};
		};
		/**
		 * `$(...)` is literal here--these scripts invoke Git in subshells,
		 * and we need to run them during the build without `nativeBuildInputs = [ git ];`
		 */
		from-2_6_2-through-2_9-no-git-patch = ''
			sed 's/$(git rev-parse --verify HEAD)/'"$shortHash"'/' -i $out/Dist/.InvokeCLIOnMainSln.sh
		'';
		/** ditto */
		pre-2_9_1-no-git-patch = ''
			sed -e 's/$(git rev-parse --abbrev-ref HEAD)/master/' \
				-e 's/$(git log -1 --format="%h")/'"$shortHash"'/' \
				-i $out/Build/standin.sh
			sed 's/$(git rev-list HEAD --count)/'"$commitCount"'/' -i $out/Build/standin.sh
		'';
	in lib.mapAttrs (_: f) {
		info-2_9_1 = {
			version = "2.9.1";
			rev = "745efb1dd8eb82f31ba9201a79cdfc5bcaf1f5d1";
			/**
			 * `$(...)` is literal here--this script invokes Git in a subshell,
			 * and we need to run it during the build without `nativeBuildInputs = [ git ];`
			 * (though in this case the build would continue with the dummy value after printing an error)
			 */
			postFetch = ''
				sed 's/$(git rev-parse --verify HEAD || printf "0000000000000000000000000000000000000000")/'"$shortHash"'/' \
					-i $out/Dist/.InvokeCLIOnMainSln.sh
			'';
			hashPostPatching = "sha256-g6U0B+wY5uosP5WyNFKylBRX9kq+zM6H+f05egYqcAQ=";
			dotnet-sdk = dotnet-sdk_6;
		};
		info-2_9 = {
			version = "2.9";
			rev = "ac3a8c7e5f0711b51defdb3f121d1a63c44818c3";
			postFetch = ''
				commitCount=20208
			'' + pre-2_9_1-no-git-patch + from-2_6_2-through-2_9-no-git-patch;
			hashPostPatching = "sha256-gDLStqpRGxTlXip+FKWj/O7sQElBNjQK8HpjZbXsrC0=";
			dotnet-sdk = dotnet-sdk_6;
		};
		info-2_8 = {
			version = "2.8";
			rev = "e731e0f32903cd40b83ed75bba3b1e3753105ce2";
			postFetch = ''
				commitCount=19337
			'' + pre-2_9_1-no-git-patch + from-2_6_2-through-2_9-no-git-patch;
			hashPostPatching = "sha256-TfxAA8QkyImau0MxCdbTWFKneXZwpXYPIB+iN9z+Unk=";
			dotnet-sdk = dotnet-sdk_6;
		};
		info-2_7 = {
			version = "2.7";
			rev = "dbaf2595625f79093eeec37d2d4a7a9a4d37f370";
			postFetch = ''
				commitCount=19020
			'' + pre-2_9_1-no-git-patch + from-2_6_2-through-2_9-no-git-patch;
			hashPostPatching = "sha256-IX8WmNU+gEY8Vh6OAC3XogiGSmAfdAks4HPJVt4K/4w=";
			dotnet-sdk = dotnet-sdk_5;
		};
		info-2_6_3 = {
			version = "2.6.3";
			rev = "167bfeb4c0821ac066a006233149e2e3c5b0dbe0";
			postFetch = ''
				commitCount=18925
			'' + pre-2_9_1-no-git-patch + from-2_6_2-through-2_9-no-git-patch;
			hashPostPatching = "sha256-2+r35rVDNYQ1sKffjSph+bsSWRtz1v3jgDqAi5WrhKo=";
			dotnet-sdk = dotnet-sdk_5;
		};
		info-2_6_2 = {
			version = "2.6.2";
			rev = "c5e6aadb0e4cf697385d29c2a481a3ae0017145e";
			postFetch = ''
				commitCount=18704
			'' + pre-2_9_1-no-git-patch + from-2_6_2-through-2_9-no-git-patch;
			hashPostPatching = "sha256-TPf2lFI4PrswoPFQAKrr9vQPHQ9Qi5afgPSyJEErKuo=";
			dotnet-sdk = dotnet-sdk_5;
		};
		info-2_6_1 = {
			version = "2.6.1";
			rev = "bd31773d9e44e698fd5c0356a600e532b0a9251f";
			postFetch = ''
				commitCount=18467
			'' + pre-2_9_1-no-git-patch;
			hashPostPatching = "sha256-2+zIlHIENzakZGxjSGfA0owFRr5K2u5EagxTbnKeVsw=";
			dotnet-sdk = dotnet-sdk_5;
		};
		info-2_6 = {
			version = "2.6";
			rev = "7749d02382d1c9e682cbd28ff3dd3240e5b91227";
			postFetch = ''
				commitCount=18376
			'' + pre-2_9_1-no-git-patch;
			hashPostPatching = "sha256-kswmNENYxumQlJdUKRQcb5Ni5+aXUqKxEnJ8jX5OHQ0=";
			dotnet-sdk = dotnet-sdk_5;
		};
		# if you want to get these building from source, start by changing `releasesEmuHawkInstallables` in `default.nix` so the attrs are actually exposed
		info-2_5_2 = {
			version = "2.5.2";
			rev = "21a476200e76ef815d2bb66dffc167f9720a4eb9";
			hashPostPatching = "";
		};
		info-2_5_1 = {
			version = "2.5.1";
			rev = "f104807193bd5a4bffdd67d52967ab844038775e";
			hashPostPatching = "";
		};
		info-2_5 = {
			version = "2.5";
			rev = "5b93ef14dc613417eb937c1f793dd2dfb851d717";
			hashPostPatching = "";
		};
		info-2_4_2 = {
			version = "2.4.2";
			rev = "546ccda9189e346840909350eca8b1fdcadad081";
			hashPostPatching = "";
		};
		info-2_4_1 = {
			version = "2.4.1";
			rev = "60a1ddea5e4775f3be4bd417d53cacf0eba978c4";
			hashPostPatching = "";
		};
		info-2_4 = {
			version = "2.4";
			rev = "16f5da9f9c2e57ac7a8bb70b2028e3a9501127b8";
			hashPostPatching = "";
		};
		info-2_3_3 = {
			version = "2.3.3";
			rev = "c330541c35e97c66df31d1616a69927cf65bf318";
			hashPostPatching = "";
		};
		info-2_3_2 = {
			version = "2.3.2";
			rev = "92847b1d1d534108143cfbff1e266036332c0573";
			hashPostPatching = "";
		};
	};
in {
	inherit depsForHistoricalRelease releaseFrags releaseTagSourceInfos;
	/** called by `buildAssembliesFor` i.e. immediately before use of a `hawkSourceInfo` */
	populateHawkSourceInfo = hawkSourceInfo: let
		inherit (hawkSourceInfo) version;
		neededExtraManagedDepsApprox = [ "virtu" ]
			++ lib.optionals (isVersionAtLeast "2.6" version) [ "hawkQuantizer" "isoParser" ]
			++ lib.optionals (isVersionAtLeast "2.6.2" version) [ "flatBuffersGenOutput" "srcGenReflectionCache" ]
			++ lib.optionals (!isVersionAtLeast "2.9" version) [ "flatBuffersCore" "gongShell" ]
			++ lib.optionals (isVersionAtLeast "2.9" version) [ "bizhawkAnalyzer" "nlua" ]
			++ lib.optional (isVersionAtLeast "2.9.1" version) "srcGenVersionInfo"
			++ lib.optionals (!isVersionAtLeast "2.9.2" version) [ "slimDX" "systemDataSqliteDropIn" ]
			++ lib.optional (isVersionAtLeast "2.9.2" version) "srcGenSettingsUtil"
			;
	in {
		inherit neededExtraManagedDepsApprox;
		__contentAddressed = false; #TODO try w/ CA
		copyingAssetsInEmuHawkProj = isVersionAtLeast "2.6.3" version;
		dotnet-sdk = dotnet-sdk_8;
		exePathRespectsEnvVar = isVersionAtLeast "2.9.2" version;
		hasAssemblyResolveHandler = isVersionAtLeast "2.3.3" version;
		hasAssetsInOutput = !isVersionAtLeast "2.6.1" version;
		hasFFmpegPatch_e68a49aa5 = isVersionAtLeast "2.9.2" version; # with e68a49aa5, downloading *and running* FFmpeg finally works; TODO use FFmpeg from Nixpkgs since it's a stable version (4.4.1)
		hasMiscTypeCheckerPatch_6afb3be98 = isVersionAtLeast "2.6.2" version;
		neededExtraManagedDeps = neededExtraManagedDepsApprox;
		needsLibGLVND = false; # true iff not using nixGL (i.e. on NixOS) AND using the OpenGL renderer (the default option)
		needsSDL = isVersionAtLeast "2.9.2" version;
		nugetDeps = ./deps.nix;
		packageScriptNeeds7Zip = !isVersionAtLeast "2.6.3" version;
		packageScriptRemovesWinFilesFromLinux = isVersionAtLeast "2.9.2" version;
		pathConfigNeedsOrdinal = !isVersionAtLeast "2.7.1" version;
		releaseArtifactHasRogueOTKAsmConfig = version == "2.9.1";
		releaseArtifactNeedsLowercaseAsms = !isVersionAtLeast "2.3.3" version;
		releaseArtifactNeedsOTKAsmConfig = isVersionAtLeast "2.3.3" version && !isVersionAtLeast "2.5" version; # see a1b501fe5
		releaseArtifactNeedsVBDotnetReference = !isVersionAtLeast "2.5.1" version;
		testProjectNeedsCIEnvVar = !isVersionAtLeast "2.8" version; # platform-specific tests don't run "in CI" because they assume Arch filesystem conventions (on Linux)--before 908d4519c, `-p:ContinuousIntegrationBuild=true` wasn't respected but `GITLAB_CI` was
		versionProjNeedsDoubleBuild = !isVersionAtLeast "2.9.1" version;
		#TODO warn about missing/broken features when eval'ing older releases
	} // hawkSourceInfo;
	/** to be passed to `splitReleaseArtifact` */
	releaseArtifactInfos = lib.mapAttrs'
		(releaseFrag: value: {
			name = "bizhawkAssemblies-${releaseFrag}-bin";
			value = let
				hawkSourceInfo = releaseTagSourceInfos."info-${releaseFrag}" // value.hawkSourceInfo or {};
			in {
				inherit hawkSourceInfo;
				${if isVersionAtLeast "2.6" hawkSourceInfo.version
					then if isVersionAtLeast "2.6.3" hawkSourceInfo.version
						then null
						else "zippedTarball"
					else "crossPlatformArtifact"} = true;
			} // lib.optionalAttrs (!isVersionAtLeast "2.6" hawkSourceInfo.version) { stripRoot = false; } // value;
		})
		{
			"2_9_1" = {
				stripRoot = false;
				hashPrePatching = "sha256-5aKUbNStQ89hnfaxv7MTQ+1qDfy+QNMyLS5WUTdhue4=";
			};
			"2_9" = {
				hashPrePatching = "sha256-gE0iu2L2yvC6dxBcv9Facm8RKPp9dseD7fAH3fnaZsY=";
			};
			"2_8" = {
				stripRoot = false;
				hashPrePatching = "sha256-IRbhI22l30OPn8zJ4HPemjWUohUvZStlEYKnV7RArFA=";
			};
			"2_7" = {
				hashPrePatching = "sha256-rc9Tk5Pc4p6YR33cowB9W01iRl8FYgAI/V1CHc+pL5E=";
			};
			"2_6_3" = {
				stripRoot = false;
				hashPrePatching = "sha256-gCHySfNOjqazbQDqk5lKJIYmPI6onqcaVDwuY8Ud2ns=";
			};
			"2_6_2" = {
				hashPrePatching = "sha256-tlnF/ZQOkLMbiEV2BqhxzQ/KixGZ30+LgOUoHvpv13s=";
			};
			"2_6_1" = {
				hashPrePatching = "sha256-Ou4NbRo7Gh0HWviXSEHtp0PpsGDdYsN8yhR0/gQy3rY=";
			};
			"2_6" = {
				hashPrePatching = "sha256-AHP1mgedC9wUq+YAJD0gM4Lrl0H0UkrWyifEDC9KLog=";
			};
			"2_5_2" = {
				hashPrePatching = "sha256-vurAHOSWwpHZ96cLnRvb6wR+6dvTVKgoqUvlU4Qyp3g=";
			};
			"2_5_1" = {
				hashPrePatching = "sha256-tB6UXQPFHPIhV5bERkv/kAktyMn3dnlI4nzIwXO9imQ=";
			};
			"2_5" = {
				url = "https://github.com/TASEmulators/BizHawk/releases/download/2.5/BizHawk-2.5.0.zip";
				hashPrePatching = "sha256-OPuNxgHWYBCw6gkkllir0U9z+ZF3K8K2o24W00MWgLk=";
			};
			"2_4_2" = {
				hashPrePatching = "sha256-0ZunzBTO4O+B89N5PI0+AeVFBhe9shEtCANsYJBVdaY=";
			};
			"2_4_1" = {
				hashPrePatching = "sha256-uORUDsPh7ePRzB69wqKW65Cch35Fjg34Q0aa7Vcf+dA=";
			};
			"2_4" = {
				hashPrePatching = "sha256-vgocYt0Wo5LSLxHF5W1aFi2xqkklMYhZP4zHOIFbCb0=";
			};
			"2_3_3" = {
				hashPrePatching = "sha256-kLPNpnggnrc7TgA6NCC0P/tkFiUgTHVyKPdUz0UX1EE=";
			};
			"2_3_2" = {
				hashPrePatching = "sha256-x2DwrzBkPAGhlov/eU/VwXuHVP/Rv0o7pZptmsSQLJU=";
			};
			# older releases won't pass prereq checker w/o WINE libs, relevant change was https://github.com/TASEmulators/BizHawk/commit/27a4062ea22e5cb4a81628580ce47fec7a2709a5#diff-aff17d6fcf6169cad48e9c8d08145b0de360e874aa8ffee0ea66e636cccda39f
		};
}
