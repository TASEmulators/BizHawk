{ mono, fetchFromGitHub }:
mono.overrideAttrs (old: rec {
	version = "6.12.0.151";
	src = fetchFromGitHub {
		owner = "mono";
		repo = "mono";
		rev = "mono-${version}";
		hash = "sha256-rdItM+O6PLQlxPNhMVFpXxRN0XWMC/jcxEeOBNoLo8c=";
		fetchSubmodules = true;
	};
	nativeBuildInputs = old.nativeBuildInputs ++ [ mono ];
})
