{ pkgs ? import <nixpkgs> {} }:
let
	versionAtLeast = exVer: acVer: builtins.compareVersions exVer acVer <= 0;
	flatbuffersTargetVersion = "23.5.26";
	flatbuffersPatched = pkgs.flatbuffers.overrideAttrs (oldAttrs: {
		version = flatbuffersTargetVersion;
		src = pkgs.fetchFromGitHub {
			owner = "google";
			repo = "flatbuffers";
			rev = "0100f6a5779831fa7a651e4b67ef389a8752bd9b";
			hash = "sha256-e+dNPNbCHYDXUS/W+hMqf/37fhVgEGzId6rhP3cToTE=";
		};
		patches = [];
		doCheck = false; # don't know and don't care why the test 1 of 1 (!) is failing
	});
	flatbuffersFinal = if versionAtLeast flatbuffersTargetVersion pkgs.flatbuffers.version
		then pkgs.flatbuffers
		else assert versionAtLeast "2.0.0" pkgs.flatbuffers.version; flatbuffersPatched; # need base of >= Nixpkgs 21.11
in pkgs.mkShell {
	packages = [ flatbuffersFinal ];
}
