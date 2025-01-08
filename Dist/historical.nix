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
			hashPostPatching = "sha512-7/uvlkR+OxwxErrp0BK+B7ZURp58p8B581lv5iAZgO3Lr6e92kTptypLt7pmqZdZrrsSmuCphNuRfHu0BZeg0A==";
			dotnet-sdk = dotnet-sdk_6;
		};
		info-2_9 = {
			version = "2.9";
			rev = "ac3a8c7e5f0711b51defdb3f121d1a63c44818c3";
			postFetch = ''
				commitCount=20208
			'' + pre-2_9_1-no-git-patch + from-2_6_2-through-2_9-no-git-patch;
			hashPostPatching = "sha512-KJvzpLyzoqLbQsGvWeC2gTWg7871jUToAVMLkI1zm2Ju5+5K5sqhrzwjK/Oxwg8HMCegqIvM9qeM2TdYYuwc6g==";
			dotnet-sdk = dotnet-sdk_6;
		};
		info-2_8 = {
			version = "2.8";
			rev = "e731e0f32903cd40b83ed75bba3b1e3753105ce2";
			postFetch = ''
				commitCount=19337
			'' + pre-2_9_1-no-git-patch + from-2_6_2-through-2_9-no-git-patch;
			hashPostPatching = "sha512-kumq3rM86vDT/4W9GioOJ6j2SAEAnyNB7eH2TcPoQGSyxkCmngIyG/yRRY4zYzRLukgEcqM/PiTzBuZxJTNKcQ==";
			dotnet-sdk = dotnet-sdk_6;
		};
		info-2_7 = {
			version = "2.7";
			rev = "dbaf2595625f79093eeec37d2d4a7a9a4d37f370";
			postFetch = ''
				commitCount=19020
			'' + pre-2_9_1-no-git-patch + from-2_6_2-through-2_9-no-git-patch;
			hashPostPatching = "sha512-24/2zMd+Y02jO5XqQLqIZG5pAqO1TC+1ZMuhsO6xYQRTcDFHu2Rq6k0fiCiaoMpac1ymwOS/5bY/x/2e8Eqc/A==";
			dotnet-sdk = dotnet-sdk_5;
		};
		info-2_6_3 = {
			version = "2.6.3";
			rev = "167bfeb4c0821ac066a006233149e2e3c5b0dbe0";
			postFetch = ''
				commitCount=18925
			'' + pre-2_9_1-no-git-patch + from-2_6_2-through-2_9-no-git-patch;
			hashPostPatching = "sha512-oSxazA8cjN6CkVahye3SExEiV+yvkhskDNJA+++fTQcRWaiPlbBb1LtLjH+YuyCyLqgJ25Wi38VgyPwhO4QKOA==";
			dotnet-sdk = dotnet-sdk_5;
		};
		info-2_6_2 = {
			version = "2.6.2";
			rev = "c5e6aadb0e4cf697385d29c2a481a3ae0017145e";
			postFetch = ''
				commitCount=18704
			'' + pre-2_9_1-no-git-patch + from-2_6_2-through-2_9-no-git-patch;
			hashPostPatching = "sha512-W79FBXhboUPwUreeIiTJ38grdcNuphzMFDKvV5StPj8kwwf/RoSw7kyK+/hXN5wFcobd48TnbRFVm36Upfuvmg==";
			dotnet-sdk = dotnet-sdk_5;
		};
		info-2_6_1 = {
			version = "2.6.1";
			rev = "bd31773d9e44e698fd5c0356a600e532b0a9251f";
			postFetch = ''
				commitCount=18467
			'' + pre-2_9_1-no-git-patch;
			hashPostPatching = "sha512-qZjdISAxXmOSUQ3h7NVOvA2N7ezC7io8NGUWPNpoUVM+YzkHa/aBdxN5VniWid2N7m/nmfeQA69nmJi7HtkY+w==";
			dotnet-sdk = dotnet-sdk_5;
		};
		info-2_6 = {
			version = "2.6";
			rev = "7749d02382d1c9e682cbd28ff3dd3240e5b91227";
			postFetch = ''
				commitCount=18376
			'' + pre-2_9_1-no-git-patch;
			hashPostPatching = "sha512-K1XafghDr64MW0fnw5uujsHLrJhxNMcj4Ptin4HT4+cEHg6WN59oNcI5Hsml6xn5u6cRBqHmcugIfYXEK1/g5g==";
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
		mainAppFilename = "EmuHawk.exe"; # for emuhawk-mono-wrapper launch script
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
	} // hawkSourceInfo // {
		frontendPackageFlavour = if (hawkSourceInfo.frontendPackageFlavour or null) == null
			then "NixHawk"
			else hawkSourceInfo.frontendPackageFlavour;
	};
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
				hashPrePatching = "sha512-VX+TlQoCWCNj76HpCXmDsS7SQly+/QeYCnu170+8kF6Y6cZkjMmILQxaC9XfTchVb/zFGMr/8gfgcZToOFZUeQ=="; # NAR checksum; gzip archive is SHA512:BEC7E558963416B3749EF558BC682D1A874A43DF8FE3083224EC7C4C0326CDFEA662EF5C604EEA7C1743D2EEB13656CCF749F0CDB3A413F4985C4B305A95E742
			};
			"2_9" = {
				hashPrePatching = "sha512-hywOvKqygQM4CTER4DFftXuND1eNsux46KuG/QWcEEwzJZ50MSOFHDj59YR+MalWAZ6CvtK4YG5JVENlpXWEdA=="; # NAR checksum; gzip archive is SHA512:A501F7A7DF2B75B00AD242D2DD8246B3B6CC165A2F903A7F1345DD900B30F3C6E0E9DA09DF2565402E8B1F34F3DBC432D3B4569282394C72C46AA348D0D439C2
			};
			"2_8" = {
				stripRoot = false;
				hashPrePatching = "sha512-T1snCNq0Zfp2yKjoIH1nQJWxA1Ve5AWYWl7izPJNG3BesxIvrTsPMwqtCFdqmss9jjE3aKnPVdjceu1xXDNUYw=="; # NAR checksum; gzip archive is SHA512:A9FFBCD914CA1843372F50DE7C022107E65B8DED3BC61F04D584A7C5342AD83321A22B9F066A8872431181BCC8DE605827911B0C67511C5A2D4E75910C85D1A6
			};
			"2_7" = {
				hashPrePatching = "sha512-tvldUoaZNV2LaudrGmSLy6VJEuGWLJjRjEYNTuYz8AgLY6006YmaRAjXRIceC04kkPblRGbJ7o4NqAcFdoJ5aQ=="; # NAR checksum; gzip archive is SHA512:9436264AA9CF44AE580A14570EE5B7C39A252A76363118633942D410EE2E5B47F81E1BF5F16030651F9EF63EE503F58AB8F434997F9A43669DFE30D0BD215F47
			};
			"2_6_3" = {
				stripRoot = false;
				hashPrePatching = "sha512-X4fyit9A9r172lyBtFGjfR2gFQIKU1oRhyupSKuGQnUujk9hYAhUfL6uuQIRu+08oJ7/pQGwlKojMBbC3QaEMg=="; # NAR checksum; gzip archive is SHA512:E16289170DC6013456EB8BCA82B6B911A92732F8DD06B096023B90B4902AA0B99606DD7F652164D5746914414D4C7A9CCE54C2888226BF77CC9887A39BA0D08D
			};
			"2_6_2" = {
				hashPrePatching = "sha512-nfNCoAa1buug4pvltayzMEC8btjtx80sZZ1275L4Bqy3N9r1ZEGnx6p1M/UZPbpNtUd0jdU0ZawlAdTnxaMvVg=="; # NAR checksum; zip archive is SHA512:8751A2229D9E500A3F3A283CE24AE62F8F911507F5FDBE25AA41F31AC5026CF89433C89DC766864439A17531543829E4B43BC4D3B311F5A80B4DF0605BBD9627
			};
			"2_6_1" = {
				hashPrePatching = "sha512-8VDjpfBHPIz18T+YymrqQkQUw25mvyUdUSQAwlsLS4ZDkg3F3qQPvSzTIavakv31gJeIweZN8Pcnw0cAkWFytw=="; # NAR checksum; zip archive is SHA512:84F0D14F8BA3AEFFE1E8A8F34BA3955629C12C6B39D34E939B14A1A30362B1BE8C7C3A29E3C75072A2D1661926607C058364ECC4E7C8F2D25EDB110182BA901C
			};
			"2_6" = {
				hashPrePatching = "sha512-DsDkIOhsYz/XHIMCol0cLg8aPDLxo5twBfEFhkpsDVSY2dhqp+VZ5wExkz7tbypaqO52KePQlaVtGmE7mPkI9g=="; # NAR checksum; zip archive is SHA512:6F3760E71103681A0F05845D0ADADD7B55C10A57D0AC44606A5B2B88E0435DAE70129F3FFCEF9AC3794FB471196ADE78CCEE7E304DF6447EFC0D271D51ED10AB
			};
			"2_5_2" = {
				hashPrePatching = "sha512-YTGC1xK7fYhyIcoF/4wqlossfRmNgQyGlXxqzn67zpTiqiidQcW5N+XXB6maZ7Yv2TOArZJmBlRpQNIPxAildg=="; # NAR checksum; zip archive is SHA512:9E4CDF5E2E311CA5AD5750B91EA5163D8E727813F9A88200D80013657C45B33B0A51112FD5816DEBE264B99B1244B8555C0F8B9ECCFC6F87C45E67BBD1C28B06
			};
			"2_5_1" = {
				hashPrePatching = "sha512-DStLgwHDaHpe3ddKSHChU6i70gfZiVaAYm+aRrov7BvLndGamYGD2AM77sV9uSgQ0DJlfYTH2TTrp2uhD7uPkg=="; # NAR checksum; zip archive is SHA512:8DDE88D72704027AD810E1B41AB6950AA5BF4A2FA93AAB7F06CDFF878CC65EE7673E1691AFEB8385F37D2035DFCBC686674EB7AF4AC279F934C46131DA56F721
			};
			"2_5" = {
				url = "https://github.com/TASEmulators/BizHawk/releases/download/2.5/BizHawk-2.5.0.zip";
				hashPrePatching = "sha512-+lMzqhCZHk3SqLeXGXvbuDURrn5nwnw9p79kqH2ywxb4t6KVJPcSvoPi90ByeUgxmoog1H2pWAkufJPN5c6kQw=="; # NAR checksum; zip archive is SHA512:CB35924402932F13F3ED7159BD5991C58790287C856DB6958F41B631A36C345C5DDEA5249D75560AE608C2900C939579FB395989939F99BF11B4B3B9E2E671AE
			};
			"2_4_2" = {
				hashPrePatching = "sha512-waqbTSdE4sjDtZc0h61CgzlJ9AUj5RxvkpPnhgJm8V/RYuWkaE6Kvg0i7ul1q46sFTn3nI/+cfWt9Af0s44EzA=="; # NAR checksum; zip archive is SHA512:EA4B8451E461B11BFC1962333ABE7FBB38F3D10CBE0BBF30AB250798FC281CC2C43807AEFFD352AB61DCF889B761AC8394BD095F37F5F22506B634FA529E4DE0
			};
			"2_4_1" = {
				hashPrePatching = "sha512-q8Ot2oTSU3adweTBBDZrWEre5FFEKMdWRRG39MbicJFN5zR1gpg3duRgE+AW36PUL8KXtNDeLypDnHQjxWmkLA=="; # NAR checksum; zip archive is SHA512:B0DEB73C155D60487F6E77F29B247B15F441E01782820DD42D22916CCF24F73B5AA95AF6A1BFED9B982984CDD06E83AEC2A21D61584EDADF4E8C8B763CD8BD27
			};
			"2_4" = {
				hashPrePatching = "sha512-Uq+0zzMT8WbgZeriMOfQriWX5JYw2u7yycze/h6mE3Ofc31+vbhrIBXZrkK/ZDXRl5bGEpiBTdItswFSez6IGQ=="; # NAR checksum; zip archive is SHA512:34CB1E7FB300391341BA9C0BDEB87FCE93800A5B90EC971850F88B72C408E0C97AC62BD12DC1BAFBF7A5A15566BEA6726445C167832987477654BB535B69F408
			};
			"2_3_3" = {
				hashPrePatching = "sha512-V2yYsjGsUkCdLTJ6qdsmCbe1MmgkjdA3yIzb9JE1Cahn9A32Kek/t5cqDwmSA82chpffsBXk3qMLfUfsRTaLgg=="; # NAR checksum; zip archive is SHA512:CF83E1357EEFB8BDF1542850D66D8007D620E4050B5715DC83F4A921D36CE9CE47D0D13C5D85F2B0FF8318D2877EEC2F63B931BD47417A81A538327AF927DA3E
			};
			"2_3_2" = {
				hashPrePatching = "sha512-WQpoK0M+ew44JE688w7O8gUfYdzTisJhy2dn7o31mNLA6+CmPnFY5h91fpipB50ZOLzfJLSSEH5v1lbujK3nZA=="; # NAR checksum; zip archive is SHA512:67E8C7E57E735E7319647A35FB09C504240D7BC885EB8D753554FC3E4A2A61AF6A91C07DCF8BF45006F5044DF6E98A8073BA61F14B4C7BCB1B25D5A9B630D817
			};
			# older releases won't pass prereq checker w/o WINE libs, relevant change was https://github.com/TASEmulators/BizHawk/commit/27a4062ea22e5cb4a81628580ce47fec7a2709a5#diff-aff17d6fcf6169cad48e9c8d08145b0de360e874aa8ffee0ea66e636cccda39f
		};
}
