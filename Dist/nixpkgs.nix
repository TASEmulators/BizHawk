{
	nixpkgs-26_05 = system: import (builtins.fetchTarball {
		url = "https://github.com/NixOS/nixpkgs/archive/8c50a710ddca43d7a530fb805ad55bde8d0141c5.tar.gz";
		sha256 = "0am8xx09fx5yf2p0wb001v0jx1g5hrfb76h4r37xph378jgk7pcr";
	}) { inherit system; };
	nixpkgs-24_05 = system: import (builtins.fetchTarball {
		url = "https://github.com/YoshiRulz/nixpkgs/archive/ba96829431f6854871a61a9fc25287604ab1e851.tar.gz"; # .NET 10 backported to 24.05, since that was the last release before ebd3b37e6, see https://github.com/NixOS/nixpkgs/pull/327651#issuecomment-3499977198
		sha256 = "0blw62acv0h0bmws378ykf12dpvp46sbsz5yavmp6k1dy8z3b836";
	}) { inherit system; };
	nixpkgs-22_11-with-dotnet-5 = system: fetchzip: import (fetchzip {
		url = "https://github.com/NixOS/nixpkgs/archive/a8f575995434695a10b574d35ca51b0f26ae9049.tar.gz"; # commit immediately before .NET 5 was removed
		hash = "sha512-3ysJjKK1lYV1r/zLohyuD1fiK+8TD3MMA3TrX9fb42nKqzfGGW62Aom7ltiyyxbVbBYOCXUy41Z5Y0j2VOxRKw==";
	}) { inherit system; };
}
