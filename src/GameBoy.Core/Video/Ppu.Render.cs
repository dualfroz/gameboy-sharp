namespace GameBoy.Video
{
    public sealed partial class Ppu
    {
        private readonly byte[] lineBgColor = new byte[ScreenWidth];

        private void RenderScanline()
        {
            RenderBackground();
            RenderWindow();
            RenderSprites();
        }

        private void RenderBackground()
        {
            int rowBase = ly * ScreenWidth;
            bool bgEnabled = (lcdc & 0x01) != 0;

            if (!bgEnabled)
            {
                for (int x = 0; x < ScreenWidth; x++)
                {
                    lineBgColor[x] = 0;
                    Framebuffer[rowBase + x] = 0;
                }
                return;
            }

            int tileMapBase = (lcdc & 0x08) != 0 ? 0x1C00 : 0x1800;
            bool signed = (lcdc & 0x10) == 0;
            int y = (scy + ly) & 0xFF;
            int tileRow = y >> 3;
            int rowInTile = y & 7;

            for (int x = 0; x < ScreenWidth; x++)
            {
                int mapX = (scx + x) & 0xFF;
                int tileCol = mapX >> 3;
                int bit = 7 - (mapX & 7);

                int tileIndex = vram[tileMapBase + tileRow * 32 + tileCol];
                int tileAddr = signed
                    ? 0x1000 + ((sbyte)tileIndex) * 16 + rowInTile * 2
                    : tileIndex * 16 + rowInTile * 2;

                int low = vram[tileAddr];
                int high = vram[tileAddr + 1];
                int color = (((high >> bit) & 1) << 1) | ((low >> bit) & 1);

                lineBgColor[x] = (byte)color;
                Framebuffer[rowBase + x] = Shade(bgp, color);
            }
        }

        private void RenderWindow()
        {
            if ((lcdc & 0x20) == 0)
                return;
            if (wy > ly)
                return;
            if (wx > 166)
                return;

            int rowBase = ly * ScreenWidth;
            int tileMapBase = (lcdc & 0x40) != 0 ? 0x1C00 : 0x1800;
            bool signed = (lcdc & 0x10) == 0;
            int winY = windowLine;
            int tileRow = winY >> 3;
            int rowInTile = winY & 7;
            int start = wx - 7;
            bool drawn = false;

            for (int x = 0; x < ScreenWidth; x++)
            {
                if (x < start)
                    continue;
                drawn = true;

                int winX = x - start;
                int tileCol = winX >> 3;
                int bit = 7 - (winX & 7);

                int tileIndex = vram[tileMapBase + tileRow * 32 + tileCol];
                int tileAddr = signed
                    ? 0x1000 + ((sbyte)tileIndex) * 16 + rowInTile * 2
                    : tileIndex * 16 + rowInTile * 2;

                int low = vram[tileAddr];
                int high = vram[tileAddr + 1];
                int color = (((high >> bit) & 1) << 1) | ((low >> bit) & 1);

                lineBgColor[x] = (byte)color;
                Framebuffer[rowBase + x] = Shade(bgp, color);
            }

            if (drawn)
                windowLine++;
        }

        private readonly int[] spriteCandidates = new int[10];

        private void RenderSprites()
        {
            if ((lcdc & 0x02) == 0)
                return;

            int height = (lcdc & 0x04) != 0 ? 16 : 8;
            int count = 0;

            for (int i = 0; i < 40 && count < 10; i++)
            {
                int spriteY = oam[i * 4] - 16;
                if (ly >= spriteY && ly < spriteY + height)
                    spriteCandidates[count++] = i;
            }

            SortByPriority(count);

            int rowBase = ly * ScreenWidth;
            for (int x = 0; x < ScreenWidth; x++)
            {
                for (int c = 0; c < count; c++)
                {
                    int i = spriteCandidates[c];
                    int spriteX = oam[i * 4 + 1] - 8;
                    if (x < spriteX || x >= spriteX + 8)
                        continue;

                    int attr = oam[i * 4 + 3];
                    int spriteY = oam[i * 4] - 16;
                    int row = ly - spriteY;
                    if ((attr & 0x40) != 0)
                        row = height - 1 - row;

                    int tile = oam[i * 4 + 2];
                    if (height == 16)
                    {
                        tile &= 0xFE;
                        if (row >= 8)
                        {
                            tile += 1;
                            row -= 8;
                        }
                    }

                    int pixelCol = x - spriteX;
                    int col = (attr & 0x20) != 0 ? 7 - pixelCol : pixelCol;
                    int bit = 7 - col;

                    int tileAddr = tile * 16 + row * 2;
                    int low = vram[tileAddr];
                    int high = vram[tileAddr + 1];
                    int color = (((high >> bit) & 1) << 1) | ((low >> bit) & 1);

                    if (color == 0)
                        continue;

                    bool behindBg = (attr & 0x80) != 0 && lineBgColor[x] != 0;
                    if (!behindBg)
                    {
                        byte palette = (attr & 0x10) != 0 ? obp1 : obp0;
                        Framebuffer[rowBase + x] = Shade(palette, color);
                    }
                    break;
                }
            }
        }

        private void SortByPriority(int count)
        {
            for (int a = 1; a < count; a++)
            {
                int key = spriteCandidates[a];
                int keyX = oam[key * 4 + 1];
                int b = a - 1;
                while (b >= 0 && ShouldSwap(spriteCandidates[b], keyX, key))
                {
                    spriteCandidates[b + 1] = spriteCandidates[b];
                    b--;
                }
                spriteCandidates[b + 1] = key;
            }
        }

        private bool ShouldSwap(int current, int keyX, int key)
        {
            int currentX = oam[current * 4 + 1];
            if (currentX != keyX)
                return currentX > keyX;
            return current > key;
        }

        private static byte Shade(byte palette, int color)
        {
            return (byte)((palette >> (color * 2)) & 0x03);
        }
    }
}
