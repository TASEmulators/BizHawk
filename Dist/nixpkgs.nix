{
	nixpkgs-24_05 = system: import (builtins.fetchTarball {
		url = "https://github.com/NixOS/nixpkgs/archive/24.05.tar.gz";
		sha256 = "1lr1h35prqkd1mkmzriwlpvxcb34kmhc9dnr48gkm8hh089hifmx";
	}) { inherit system; };
	nixpkgs-23_05 = system: fetchzip: import (fetchzip {
		url = "https://github.com/NixOS/nixpkgs/archive/23.05.tar.gz";
		hash = "sha512-REPJ9fRKxTefvh1d25MloT4bXJIfxI+1EvfVWq644Tzv+nuq2BmiGMiBNmBkyN9UT5fl2tdjqGliye3gZGaIGg==";
	}) { inherit system; };
	nixpkgs-22_11-with-dotnet-5 = system: fetchzip: import (fetchzip {
		url = "https://github.com/NixOS/nixpkgs/archive/a8f575995434695a10b574d35ca51b0f26ae9049.tar.gz"; # commit immediately before .NET 5 was removed
		hash = "sha512-3ysJjKK1lYV1r/zLohyuD1fiK+8TD3MMA3TrX9fb42nKqzfGGW62Aom7ltiyyxbVbBYOCXUy41Z5Y0j2VOxRKw==";
	}) { inherit system; };
}
