{ pkgs ? import <nixpkgs> {} }:
let
	versionAtLeast = exVer: acVer: builtins.compareVersions exVer acVer <= 0;
	flatbuffersPatched = pkgs.flatbuffers.overrideAttrs (oldAttrs: {
		version = "22.9.24";
		src = pkgs.fetchFromGitHub {
			owner = "google";
			repo = "flatbuffers";
			rev = "76ddae006f6e5068d2f26f235dbd167bd826a698";
			sha256 = "1vycd1641id476qhmkrgdfiisxx7n2zn54p3r6nva6dm0bd58lc8";
		};
		patches = []; # single patch has since been merged upstream
		postPatch = ''
			# Fix default value of "test_data_path" to make tests work
			substituteInPlace tests/test.cpp --replace '"tests/";' '"../tests/";'
		'';
	});
	flatbuffersFinal = if versionAtLeast "22.9.24" pkgs.flatbuffers.version
		then pkgs.flatbuffers
		else assert versionAtLeast "2.0.0" pkgs.flatbuffers.version; flatbuffersPatched; # need base of >= Nixpkgs 21.11
in pkgs.mkShell {
	packages = [ flatbuffersFinal ];
}
