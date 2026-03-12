{
	nixpkgs-24_05 = system: import (builtins.fetchTarball {
		url = "https://github.com/YoshiRulz/nixpkgs/archive/a9302067440792f6137e4e96f3bfcbb1ed086eb2.tar.gz"; # .NET 10 backported to 24.05, since that was the last release before ebd3b37e6, see https://github.com/NixOS/nixpkgs/pull/327651#issuecomment-3499977198
		sha256 = "1app1qh40blfr2r1dkkpp7r22kmbq50x4zicrak8rrcgamxd0lxb";
	}) { inherit system; };
	nixpkgs-22_11-with-dotnet-5 = system: fetchzip: import (fetchzip {
		url = "https://github.com/NixOS/nixpkgs/archive/a8f575995434695a10b574d35ca51b0f26ae9049.tar.gz"; # commit immediately before .NET 5 was removed
		hash = "sha512-3ysJjKK1lYV1r/zLohyuD1fiK+8TD3MMA3TrX9fb42nKqzfGGW62Aom7ltiyyxbVbBYOCXUy41Z5Y0j2VOxRKw==";
	}) { inherit system; };
}
