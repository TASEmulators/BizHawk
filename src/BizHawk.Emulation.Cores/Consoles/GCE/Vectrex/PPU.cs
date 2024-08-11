using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	public class PPU
	{
		public VectrexHawk Core { get; set; }

		public bool zero_sig, ramp_sig, blank_sig, off_screen;
		public byte vec_scale, x_vel, y_vel, bright;
		public double x_pos, y_pos;

		public int skip;
		public uint bright_int_1, bright_int_2, bright_int_3;

		public const uint br = 0xFFFFFFFF;

		// lines to draw in a frame and vairables to go to new line
		public double[] draw_lines = new double[1024 * 4 * 4];
		public uint[] line_brights = new uint[1024 * 3 * 4];
		public bool[] line_vis = new bool[1024 * 4];

		public double[] draw_lines_old_screen = new double[1024 * 4 * 4];
		public uint[] line_brights_old_screen = new uint[1024 * 3 * 4];
		public bool[] line_vis_old_screen = new bool[1024 * 4];
		public int line_pointer_old_screen;

		public int line_pointer;
		public bool blank_old, zero_old;
		public byte x_vel_old, y_vel_old;
		public uint bright_int_1_old;

		public void tick()
		{
			//Console.WriteLine(ramp_sig + " " + zero_sig + " " + blank_sig + " " + Core.cpu.TotalExecutedCycles + " " + (x_vel - 128.0) + " " + x_pos 
			//+ " " + (y_vel - 128.0) + " " + y_pos + " " + Core.t1_counter + " " + vec_scale);

			if (ramp_sig && !zero_sig)
			{
				if (skip == 0)
				{
					x_pos += (x_vel - 128.0) / 256.0 * (vec_scale + 2);
					y_pos -= (y_vel - 128.0) / 256.0 * (vec_scale + 2);
				}
				else
				{
					skip--;
				}

				off_screen = false;
				
				if (x_pos > 257) { off_screen = true; if (x_pos > (257 + 256)) { x_pos = (257 + 256); } }
				if (x_pos < 2) { off_screen = true; if (x_pos < (2 - 256)) { x_pos = (2 - 256); } }
				if (y_pos > 385) { off_screen = true; if (y_pos > (385 + 256)) { y_pos = (385 + 256); } }
				if (y_pos < 2) { off_screen = true; if (y_pos < (2 - 256)) { y_pos = (2 - 256); } }
			}
			else if (zero_sig)
			{
				x_pos = 128 + 2;
				y_pos = 192 + 2;
			}
			/*
			if (!blank_sig && !off_screen)
			{
				
				Core._vidbuffer[(int)(Math.Round(x_pos) + 260 * Math.Round(y_pos))] |= (int)(br & bright_int_1);
				
				Core._vidbuffer[(int)(Math.Round(x_pos) + 1 + 260 * Math.Round(y_pos))] |= (int)(br & bright_int_2);
				Core._vidbuffer[(int)(Math.Round(x_pos) - 1 + 260 * Math.Round(y_pos))] |= (int)(br & bright_int_2);
				Core._vidbuffer[(int)(Math.Round(x_pos) + 260 * (Math.Round(y_pos) + 1))] |= (int)(br & bright_int_2);
				Core._vidbuffer[(int)(Math.Round(x_pos) + 260 * (Math.Round(y_pos) - 1))] |= (int)(br & bright_int_2);
				
				Core._vidbuffer[(int)(Math.Round(x_pos) + 2 + 260 * Math.Round(y_pos))] |= (int)(br & bright_int_3);
				Core._vidbuffer[(int)(Math.Round(x_pos) - 2 + 260 * Math.Round(y_pos))] |= (int)(br & bright_int_3);
				Core._vidbuffer[(int)(Math.Round(x_pos) + 260 * (Math.Round(y_pos) + 2))] |= (int)(br & bright_int_3);
				Core._vidbuffer[(int)(Math.Round(x_pos) + 260 * (Math.Round(y_pos) - 2))] |= (int)(br & bright_int_3);
				Core._vidbuffer[(int)(Math.Round(x_pos) + 1 + 260 * (Math.Round(y_pos) + 1))] |= (int)(br & bright_int_3);
				Core._vidbuffer[(int)(Math.Round(x_pos) + 1 + 260 * (Math.Round(y_pos) - 1))] |= (int)(br & bright_int_3);
				Core._vidbuffer[(int)(Math.Round(x_pos) - 1 + 260 * (Math.Round(y_pos) + 1))] |= (int)(br & bright_int_3);
				Core._vidbuffer[(int)(Math.Round(x_pos) - 1 + 260 * (Math.Round(y_pos) - 1))] |= (int)(br & bright_int_3);
				
			}
			*/
		}

		public void draw_screen()
		{
			// screen is 2 times the internal size of the image 
			double start_x = 0;
			double end_x = 0;
			double start_y = 0;
			double end_y = 0;

			uint c_bright = 0;

			for (int i = 0; i < line_pointer; i++)
			{
				if (line_vis[i])
				{
					start_x = draw_lines[i * 4];
					start_y = draw_lines[i * 4 + 1];

					end_x = draw_lines[i * 4 + 2];
					end_y = draw_lines[i * 4 + 3];

					c_bright = line_brights[i * 3];

					double steps = 0;

					double max_x = Math.Abs(end_x - start_x);
					double max_y = Math.Abs(end_y - start_y);

					bool draw_this_line = true;

					if ((start_x < 2) && (end_x < 2)) { draw_this_line = false; }
					if ((start_y < 2) && (end_y < 2)) { draw_this_line = false; }
					if ((start_x > 257) && (end_x > 257)) { draw_this_line = false; }
					if ((start_y > 385) && (end_y > 385)) { draw_this_line = false; }

					if (draw_this_line)
					{
						// truncate lines to only be on screen
						if ((start_x >= 2) && (end_x < 2)) { max_x = start_x - 2; end_x = 2; }
						if ((end_x >= 2) && (start_x < 2)) { max_x = end_x - 2; start_x = 2; }
						if ((start_x <= 257) && (end_x > 257)) { max_x = 257 - start_x; end_x = 257; }
						if ((end_x <= 257) && (start_x >= 257)) { max_x = 257 - end_x; start_x = 257; }

						if ((start_y >= 2) && (end_y < 2)) { max_y = start_y - 2; end_y = 2; }
						if ((end_y >= 2) && (start_y < 2)) { max_y = end_y - 2; start_y = 2; }
						if ((start_y <= 385) && (end_y > 385)) { max_y = 385 - start_y; end_y = 257; }
						if ((end_y <= 385) && (start_y >= 385)) { max_y = 385 - end_y; start_y = 385; }

						// screen size is double internal size
							
						start_x *= 2;
						end_x *= 2;
						start_y *= 2;
						end_y *= 2;

						max_x *= 2;
						max_y *= 2;
							
						steps = Math.Max(max_x, max_y) + 1;

						double x_step = (end_x - start_x) / steps;
						double y_step = (end_y - start_y) / steps;

						for (int j = 0; j <= steps; j++)
						{
							Core._vidbuffer[(int)(Math.Round(start_x + x_step * j) + 260 * 2 * Math.Round(start_y + y_step * j))] |= (int)(br & c_bright);

							// at minimum need to make 3 pixels thick to be represetnative of a real vectrex, add more for glow
							Core._vidbuffer[(int)(Math.Round(start_x + x_step * j) + 1 + 260 * 2 * Math.Round(start_y + y_step * j))] |= (int)(br & c_bright);
							Core._vidbuffer[(int)(Math.Round(start_x + x_step * j) - 1 + 260 * 2 * Math.Round(start_y + y_step * j))] |= (int)(br & c_bright);
							Core._vidbuffer[(int)(Math.Round(start_x + x_step * j) + 260 * 2 * (Math.Round(start_y + y_step * j) + 1))] |= (int)(br & c_bright);
							Core._vidbuffer[(int)(Math.Round(start_x + x_step * j) + 260 * 2 * (Math.Round(start_y + y_step * j) - 1))] |= (int)(br & c_bright);
						}
					}
				}
			}

			// copy all the data to the old screen arrays to save it for loading states
			for (int i = 0; i < line_pointer; i++)
			{
				draw_lines_old_screen[i * 4] = draw_lines[i * 4];
				draw_lines_old_screen[i * 4 + 1] = draw_lines[i * 4 + 1];
				draw_lines_old_screen[i * 4 + 2] = draw_lines[i * 4 + 2];
				draw_lines_old_screen[i * 4 + 3] = draw_lines[i * 4 + 3];

				line_brights_old_screen[i * 3] = line_brights[i * 3];
				line_brights_old_screen[i * 3 + 1] = line_brights[i * 3 + 1];
				line_brights_old_screen[i * 3 + 2] = line_brights[i * 3 + 2];

				line_vis_old_screen[i] = line_vis[i];
			}

			line_pointer_old_screen = line_pointer;

			// reset pointer back to zero but keep current starting point
			draw_lines[0] = draw_lines[line_pointer * 4];
			draw_lines[1] = draw_lines[line_pointer * 4 + 1];

			line_vis[0] = line_vis[line_pointer];

			line_brights[0] = line_brights[line_pointer * 3];

			line_pointer = 0;
		}

		public void draw_old_screen()
		{
			// screen is 2 times the internal size of the image 
			double start_x = 0;
			double end_x = 0;
			double start_y = 0;
			double end_y = 0;

			uint c_bright = 0;

			for (int i = 0; i < line_pointer_old_screen; i++)
			{
				if (line_vis_old_screen[i])
				{
					start_x = draw_lines_old_screen[i * 4];
					start_y = draw_lines_old_screen[i * 4 + 1];

					end_x = draw_lines_old_screen[i * 4 + 2];
					end_y = draw_lines_old_screen[i * 4 + 3];

					c_bright = line_brights_old_screen[i * 3];

					double steps = 0;

					double max_x = Math.Abs(end_x - start_x);
					double max_y = Math.Abs(end_y - start_y);

					bool draw_this_line = true;

					if ((start_x < 2) && (end_x < 2)) { draw_this_line = false; }
					if ((start_y < 2) && (end_y < 2)) { draw_this_line = false; }
					if ((start_x > 257) && (end_x > 257)) { draw_this_line = false; }
					if ((start_y > 385) && (end_y > 385)) { draw_this_line = false; }

					if (draw_this_line)
					{
						// truncate lines to only be on screen
						if ((start_x >= 2) && (end_x < 2)) { max_x = start_x - 2; end_x = 2; }
						if ((end_x >= 2) && (start_x < 2)) { max_x = end_x - 2; start_x = 2; }
						if ((start_x <= 257) && (end_x > 257)) { max_x = 257 - start_x; end_x = 257; }
						if ((end_x <= 257) && (start_x >= 257)) { max_x = 257 - end_x; start_x = 257; }

						if ((start_y >= 2) && (end_y < 2)) { max_y = start_y - 2; end_y = 2; }
						if ((end_y >= 2) && (start_y < 2)) { max_y = end_y - 2; start_y = 2; }
						if ((start_y <= 385) && (end_y > 385)) { max_y = 385 - start_y; end_y = 257; }
						if ((end_y <= 385) && (start_y >= 385)) { max_y = 385 - end_y; start_y = 385; }

						// screen size is double internal size

						start_x *= 2;
						end_x *= 2;
						start_y *= 2;
						end_y *= 2;

						max_x *= 2;
						max_y *= 2;

						steps = Math.Max(max_x, max_y) + 1;

						double x_step = (end_x - start_x) / steps;
						double y_step = (end_y - start_y) / steps;

						for (int j = 0; j <= steps; j++)
						{
							Core._framebuffer[(int)(Math.Round(start_x + x_step * j) + 260 * 2 * Math.Round(start_y + y_step * j))] |= (int)(br & c_bright);

							// at minimum need to make 3 pixels thick to be represetnative of a real vectrex, add more for glow
							Core._framebuffer[(int)(Math.Round(start_x + x_step * j) + 1 + 260 * 2 * Math.Round(start_y + y_step * j))] |= (int)(br & c_bright);
							Core._framebuffer[(int)(Math.Round(start_x + x_step * j) - 1 + 260 * 2 * Math.Round(start_y + y_step * j))] |= (int)(br & c_bright);
							Core._framebuffer[(int)(Math.Round(start_x + x_step * j) + 260 * 2 * (Math.Round(start_y + y_step * j) + 1))] |= (int)(br & c_bright);
							Core._framebuffer[(int)(Math.Round(start_x + x_step * j) + 260 * 2 * (Math.Round(start_y + y_step * j) - 1))] |= (int)(br & c_bright);
						}
					}
				}
			}
		}

		public void new_draw_line()
		{
			if ((ramp_sig && !zero_sig && (x_vel != x_vel_old || y_vel != y_vel_old))
				|| blank_sig != blank_old
				|| bright_int_1 != bright_int_1_old
				|| zero_sig != zero_old)
			{
				draw_lines[line_pointer * 4 + 2] = x_pos;
				draw_lines[line_pointer * 4 + 3] = y_pos;

				line_pointer++;

				draw_lines[line_pointer * 4] = x_pos;
				draw_lines[line_pointer * 4 + 1] = y_pos;

				line_brights[line_pointer * 3] = bright_int_1;
				line_brights[line_pointer * 3 + 1] = bright_int_2;
				line_brights[line_pointer * 3 + 2] = bright_int_3;

				line_vis[line_pointer] = !blank_sig;
			}

			if (ramp_sig && !zero_sig)
			{
				x_vel_old = x_vel;
				y_vel_old = y_vel;
			}

			zero_old = zero_sig;
			blank_old = blank_sig;
			bright_int_1_old = bright_int_1;			
		}

		public void Reset()
		{
			zero_sig = true;
			blank_sig = true;
			ramp_sig = false;

			vec_scale = x_vel = y_vel = bright = 0;
			x_pos = 128 + 2;
			y_pos = 192 + 2;

			line_pointer = 0;
			blank_old = zero_old = true;
			x_vel_old = y_vel_old = 0;
			bright_int_1_old = 0;

			// initial line array values
			draw_lines[line_pointer * 5] = x_pos;
			draw_lines[line_pointer * 5 + 1] = y_pos;

			line_brights[line_pointer * 3] = 0;
			line_brights[line_pointer * 3 + 1] = 0;
			line_brights[line_pointer * 3 + 2] = 0;

			line_vis[line_pointer] = !blank_sig;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(zero_sig), ref zero_sig);
			ser.Sync(nameof(blank_sig), ref blank_sig);
			ser.Sync(nameof(ramp_sig), ref ramp_sig);
			ser.Sync(nameof(off_screen), ref off_screen);

			ser.Sync(nameof(vec_scale), ref vec_scale);
			ser.Sync(nameof(x_vel), ref x_vel);
			ser.Sync(nameof(y_vel), ref y_vel);
			ser.Sync(nameof(bright), ref bright);

			ser.Sync(nameof(x_pos), ref x_pos);
			ser.Sync(nameof(y_pos), ref y_pos);

			ser.Sync(nameof(skip), ref skip);
			ser.Sync(nameof(bright_int_1), ref bright_int_1);
			ser.Sync(nameof(bright_int_2), ref bright_int_2);
			ser.Sync(nameof(bright_int_3), ref bright_int_3);

			ser.Sync(nameof(draw_lines), ref draw_lines, false);
			ser.Sync(nameof(line_brights), ref line_brights, false);
			ser.Sync(nameof(line_vis), ref line_vis, false);
			ser.Sync(nameof(line_pointer), ref line_pointer);
			ser.Sync(nameof(blank_old), ref blank_old);
			ser.Sync(nameof(zero_old), ref zero_old);
			ser.Sync(nameof(x_vel_old), ref x_vel_old);
			ser.Sync(nameof(y_vel_old), ref y_vel_old);
			ser.Sync(nameof(bright_int_1_old), ref bright_int_1_old);

			ser.Sync(nameof(draw_lines_old_screen), ref draw_lines_old_screen, false);
			ser.Sync(nameof(line_brights_old_screen), ref line_brights_old_screen, false);
			ser.Sync(nameof(line_vis_old_screen), ref line_vis_old_screen, false);
			ser.Sync(nameof(line_pointer_old_screen), ref line_pointer_old_screen);
		}
	}
}
