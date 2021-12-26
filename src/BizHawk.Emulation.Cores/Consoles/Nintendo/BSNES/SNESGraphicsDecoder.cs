using System;

namespace BizHawk.Emulation.Cores.Nintendo.BSNES
{
	public sealed class SNESGraphicsDecoder : SNES.ISNESGraphicsDecoder
	{
		private readonly BsnesApi _api;

		public SNESGraphicsDecoder(BsnesApi api)
			=> _api = api;

		public void CacheTiles()
			=> throw new NotImplementedException();

		public int Colorize(int rgb555)
			=> throw new NotImplementedException();

		public unsafe void Colorize(int* buf, int offset, int numpixels)
			=> throw new NotImplementedException();

		public SNES.ISNESGraphicsDecoder.OAMInfo CreateOAMInfo(SNES.SNESGraphicsDecoder.ScreenInfo si, int num)
			=> throw new NotImplementedException();

		public unsafe void DecodeBG(
			int* screen,
			int stride,
			SNES.SNESGraphicsDecoder.TileEntry[] map,
			int tiledataBaseAddr,
			SNES.SNESGraphicsDecoder.ScreenSize size,
			int bpp,
			int tilesize,
			int paletteStart)
				=> throw new NotImplementedException();

		public unsafe void DecodeMode7BG(int* screen, int stride, bool extBg)
			=> throw new NotImplementedException();

		public unsafe void DirectColorify(int* screen, int numPixels)
			=> throw new NotImplementedException();

		public void Dispose() {}

		public void Enter()
			=> _api.Enter();

		public void Exit()
			=> _api.Exit();

		public SNES.SNESGraphicsDecoder.TileEntry[] FetchMode7Tilemap()
			=> throw new NotImplementedException();

		public SNES.SNESGraphicsDecoder.TileEntry[] FetchTilemap(int addr, SNES.SNESGraphicsDecoder.ScreenSize size)
			=> throw new NotImplementedException();

		public int[] GetPalette()
			=> throw new NotImplementedException();

		public unsafe void Paletteize(int* buf, int offset, int startcolor, int numpixels)
			=> throw new NotImplementedException();

		public unsafe void RenderMode7TilesToScreen(
			int* screen,
			int stride,
			bool ext,
			bool directColor,
			int tilesWide,
			int startTile,
			int numTiles)
				=> throw new NotImplementedException();

		public unsafe void RenderSpriteToScreen(
			int* screen,
			int stride,
			int destx,
			int desty,
			SNES.SNESGraphicsDecoder.ScreenInfo si,
			int spritenum,
			SNES.ISNESGraphicsDecoder.OAMInfo oam,
			int xlimit,
			int ylimit,
			byte[,] spriteMap)
				=> throw new NotImplementedException();

		public unsafe void RenderTilesToScreen(
			int* screen,
			int tilesWide,
			int tilesTall,
			int stride,
			int bpp,
			int startcolor,
			int startTile,
			int numTiles,
			bool descramble16)
				=> throw new NotImplementedException();

		public SNES.SNESGraphicsDecoder.ScreenInfo ScanScreenInfo()
			=> throw new NotImplementedException();

		public void SetBackColor(int snescol)
			=> throw new NotImplementedException();
	}
}
