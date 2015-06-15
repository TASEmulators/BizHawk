local canvas = gui.createcanvas(200, 200);
canvas.Clear(0xFF000000);
canvas.DrawRectangle(50, 50, 100, 100, 0xFFFF0000, 0xFF0000FF);
canvas.DrawText(0, 0, "Hello, world!");
canvas.Refresh();