using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink4x
{
	public partial class GBHawkLink4x : IEmulator, IVideoProvider, ISoundProvider
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _controllerDeck.Definition;

		public bool FrameAdvance(IController controller, bool render, bool rendersound)
		{
			//Console.WriteLine("-----------------------FRAME-----------------------");
			//Update the color palette if a setting changed
			if (Link4xSettings.Palette_A == GBHawk.GBHawk.GBSettings.PaletteType.BW)
			{
				A.ppu.color_palette[0] = color_palette_BW[0];
				A.ppu.color_palette[1] = color_palette_BW[1];
				A.ppu.color_palette[2] = color_palette_BW[2];
				A.ppu.color_palette[3] = color_palette_BW[3];
			}
			else
			{
				A.ppu.color_palette[0] = color_palette_Gr[0];
				A.ppu.color_palette[1] = color_palette_Gr[1];
				A.ppu.color_palette[2] = color_palette_Gr[2];
				A.ppu.color_palette[3] = color_palette_Gr[3];
			}

			if (Link4xSettings.Palette_B == GBHawk.GBHawk.GBSettings.PaletteType.BW)
			{
				B.ppu.color_palette[0] = color_palette_BW[0];
				B.ppu.color_palette[1] = color_palette_BW[1];
				B.ppu.color_palette[2] = color_palette_BW[2];
				B.ppu.color_palette[3] = color_palette_BW[3];
			}
			else
			{
				B.ppu.color_palette[0] = color_palette_Gr[0];
				B.ppu.color_palette[1] = color_palette_Gr[1];
				B.ppu.color_palette[2] = color_palette_Gr[2];
				B.ppu.color_palette[3] = color_palette_Gr[3];
			}

			if (Link4xSettings.Palette_C == GBHawk.GBHawk.GBSettings.PaletteType.BW)
			{
				C.ppu.color_palette[0] = color_palette_BW[0];
				C.ppu.color_palette[1] = color_palette_BW[1];
				C.ppu.color_palette[2] = color_palette_BW[2];
				C.ppu.color_palette[3] = color_palette_BW[3];
			}
			else
			{
				C.ppu.color_palette[0] = color_palette_Gr[0];
				C.ppu.color_palette[1] = color_palette_Gr[1];
				C.ppu.color_palette[2] = color_palette_Gr[2];
				C.ppu.color_palette[3] = color_palette_Gr[3];
			}

			if (Link4xSettings.Palette_D == GBHawk.GBHawk.GBSettings.PaletteType.BW)
			{
				D.ppu.color_palette[0] = color_palette_BW[0];
				D.ppu.color_palette[1] = color_palette_BW[1];
				D.ppu.color_palette[2] = color_palette_BW[2];
				D.ppu.color_palette[3] = color_palette_BW[3];
			}
			else
			{
				D.ppu.color_palette[0] = color_palette_Gr[0];
				D.ppu.color_palette[1] = color_palette_Gr[1];
				D.ppu.color_palette[2] = color_palette_Gr[2];
				D.ppu.color_palette[3] = color_palette_Gr[3];
			}

			if (_tracer.IsEnabled())
			{
				A.cpu.TraceCallback = s => _tracer.Put(s);
			}
			else
			{
				A.cpu.TraceCallback = null;
			}

			if (controller.IsPressed("P1 Power"))
			{
				A.HardReset();
			}
			if (controller.IsPressed("P2 Power"))
			{
				B.HardReset();
			}
			if (controller.IsPressed("P3 Power"))
			{
				C.HardReset();
			}
			if (controller.IsPressed("P4 Power"))
			{
				D.HardReset();
			}

			if (controller.IsPressed("Toggle Cable UD") | controller.IsPressed("Toggle Cable LR") | controller.IsPressed("Toggle Cable X") | controller.IsPressed("Toggle Cable 4x"))
			{
				// if any connection exists, disconnect it
				// otherwise connect in order of precedence
				// only one event can happen per frame, either a connection or disconnection
				if (_cableconnected_UD | _cableconnected_LR | _cableconnected_X | _cableconnected_4x)
				{
					_cableconnected_UD = _cableconnected_LR = _cableconnected_X = _cableconnected_4x = false;
					do_2_next_1 = false;
					do_2_next_2 = false;
				}
				else if (controller.IsPressed("Toggle Cable UD"))
				{
					_cableconnected_UD = true;
				}
				else if (controller.IsPressed("Toggle Cable LR"))
				{
					_cableconnected_LR = true;
				}
				else if (controller.IsPressed("Toggle Cable X"))
				{
					_cableconnected_X = true;
				}
				else if (controller.IsPressed("Toggle Cable 4x"))
				{
					_cableconnected_4x = true;
					is_pinging = false;
					is_transmitting = false;
				}

				Console.WriteLine("Cable connect status:");
				Console.WriteLine("UD: " + _cableconnected_UD);
				Console.WriteLine("LR: " + _cableconnected_LR);
				Console.WriteLine("X: " + _cableconnected_X);
				Console.WriteLine("4x: " + _cableconnected_4x);
			}

			_islag = true;

			GetControllerState(controller);

			do_frame_fill = false;

			if (_cableconnected_4x)
			{
				do_frame_4x();
			}
			else
			{
				do_frame_2x2();
			}
			
			if (do_frame_fill)
			{
				FillVideoBuffer();
			}

			_islag = A._islag & B._islag & C._islag & D._islag;

			if (_islag)
			{
				_lagcount++;
			}

			_frame++;

			return true;
		}

		// NOTE: this is HLE only and should be replaced eventually. It relies on information that a physical adapter does not have to sync transfers.
		// I think the adapter can see the high bit of each tranfer byte on its port, so is probably using that for syncing.
		// But, the details aren't known.
		public void do_frame_4x()
		{
			// advance one full frame
			for (int i = 0; i < 70224; i++)
			{
				A.do_single_step();
				B.do_single_step();
				C.do_single_step();
				D.do_single_step();

				x4_clock--;

				if (x4_clock == 0)
				{
					if (is_transmitting)
					{ 
						if (ready_to_transmit)
						{
							// fill the buffer on the second pass
							A.serialport.serial_clock = 1;
							A.serialport.going_out = (byte)(A.serialport.serial_data >> 7);
							A.serialport.coming_in = (byte)((x4_buffer[transmit_byte + (buffer_parity ? 0x400 : 0)] >> bit_count) & 1);
							temp1_rec = (byte)((temp1_rec << 1) | A.serialport.going_out);

							if ((status_byte & 0x20) == 0x20)
							{
								B.serialport.serial_clock = 1;
								B.serialport.going_out = (byte)(B.serialport.serial_data >> 7);
								B.serialport.coming_in = (byte)((x4_buffer[transmit_byte + (buffer_parity ? 0x400 : 0)] >> bit_count) & 1);

								temp2_rec = (byte)((temp2_rec << 1) | B.serialport.going_out);
							}
							else
							{
								temp2_rec = (byte)((temp2_rec << 1) | 0);
							}

							if ((status_byte & 0x40) == 0x40)
							{
								C.serialport.serial_clock = 1;
								C.serialport.going_out = (byte)(C.serialport.serial_data >> 7);
								C.serialport.coming_in = (byte)((x4_buffer[transmit_byte + (buffer_parity ? 0x400 : 0)] >> bit_count) & 1);

								temp3_rec = (byte)((temp3_rec << 1) | C.serialport.going_out);
							}
							else
							{
								temp3_rec = (byte)((temp3_rec << 1) | 0);
							}

							if ((status_byte & 0x80) == 0x80)
							{
								D.serialport.serial_clock = 1;
								D.serialport.going_out = (byte)(D.serialport.serial_data >> 7);
								D.serialport.coming_in = (byte)((x4_buffer[transmit_byte + (buffer_parity ? 0x400 : 0)] >> bit_count) & 1);

								temp4_rec = (byte)((temp4_rec << 1) | D.serialport.going_out);
							}
							else
							{
								temp4_rec = (byte)((temp4_rec << 1) | 0);
							}

							bit_count--;
							x4_clock = 512 + transmit_speed * 8;

							if (bit_count == -1)
							{
								bit_count = 7;
								x4_clock = 64;
								ready_to_transmit = false;

								if ((transmit_byte >= 1) && (transmit_byte < (num_bytes_transmit + 1)))
								{
									x4_buffer[(buffer_parity ? 0 : 0x400) + (transmit_byte - 1)] = temp1_rec;
									x4_buffer[(buffer_parity ? 0 : 0x400) + num_bytes_transmit + (transmit_byte - 1)] = temp2_rec;
									x4_buffer[(buffer_parity ? 0 : 0x400) + num_bytes_transmit * 2 + (transmit_byte - 1)] = temp3_rec;
									x4_buffer[(buffer_parity ? 0 : 0x400) + num_bytes_transmit * 3 + (transmit_byte - 1)] = temp4_rec;
								}

								//Console.WriteLine(temp1_rec + " " + temp2_rec + " " + temp3_rec + " " + temp4_rec + " " + transmit_byte);

								transmit_byte++;

								if (transmit_byte == num_bytes_transmit * 4)
								{
									transmit_byte = 0;
									buffer_parity = !buffer_parity;
								}
							}
						}
						else
						{
							if ((A.serialport.clk_rate == -1) && A.serialport.serial_control.Bit(7))
							{
								ready_to_transmit = true;

								if ((status_byte & 0x20) == 0x20)
								{
									if (!((B.serialport.clk_rate == -1) && B.serialport.serial_control.Bit(7))) { ready_to_transmit = false; }
								}
								if ((status_byte & 0x40) == 0x40)
								{
									if (!((C.serialport.clk_rate == -1) && C.serialport.serial_control.Bit(7))) { ready_to_transmit = false; }
								}
								if ((status_byte & 0x80) == 0x80)
								{
									if (!((D.serialport.clk_rate == -1) && D.serialport.serial_control.Bit(7))) { ready_to_transmit = false; }
								}
							}

							if (ready_to_transmit)
							{
								x4_clock = 512 + transmit_speed * 8;
							}
							else
							{
								x4_clock = 64;
							}
						}
					}
					else if (is_pinging)
					{
						if (ping_byte == 0)
						{
							// first byte sent is 0xFE
							if (ping_player == 1)
							{
								if ((A.serialport.clk_rate == -1) && A.serialport.serial_control.Bit(7))
								{
									A.serialport.serial_clock = 1;
									A.serialport.going_out = (byte)(A.serialport.serial_data >> 7);
									A.serialport.coming_in = (byte)((0xFE >> bit_count) & 1);
								}

								received_byte |= (byte)(A.serialport.going_out << bit_count);
							}
							else if (ping_player == 2)
							{
								if ((B.serialport.clk_rate == -1) && B.serialport.serial_control.Bit(7))
								{
									B.serialport.serial_clock = 1;
									B.serialport.going_out = (byte)(B.serialport.serial_data >> 7);
									B.serialport.coming_in = (byte)((0xFE >> bit_count) & 1);
								}

								received_byte |= (byte)(B.serialport.going_out << bit_count);
							}
							else if (ping_player == 3)
							{
								if ((C.serialport.clk_rate == -1) && C.serialport.serial_control.Bit(7))
								{
									C.serialport.serial_clock = 1;
									C.serialport.going_out = (byte)(C.serialport.serial_data >> 7);
									C.serialport.coming_in = (byte)((0xFE >> bit_count) & 1);
								}

								received_byte |= (byte)(C.serialport.going_out << bit_count);
							}
							else
							{
								if ((D.serialport.clk_rate == -1) && D.serialport.serial_control.Bit(7))
								{
									D.serialport.serial_clock = 1;
									D.serialport.going_out = (byte)(D.serialport.serial_data >> 7);
									D.serialport.coming_in = (byte)((0xFE >> bit_count) & 1);
								}

								received_byte |= (byte)(D.serialport.going_out << bit_count);
							}

							bit_count--;
							x4_clock = 512;

							if (bit_count == -1)
							{
								// player one can start the transmission phase
								if (ping_player == 1)
								{
									begin_transmitting_cnt = 0;
									num_bytes_transmit = received_byte;
								}

								//Console.WriteLine(ping_player + " " + ping_byte + " " + status_byte + " " + received_byte);

								bit_count = 7;
								received_byte = 0;

								ping_byte++;
								x4_clock = 64;
								is_pinging = false;
							}
						}
						else
						{
							// the next 3 bytes are the status byte (which may be updated in between each transfer)
							if (ping_player == 1)
							{
								if ((A.serialport.clk_rate == -1) && A.serialport.serial_control.Bit(7))
								{
									A.serialport.serial_clock = 1;
									A.serialport.going_out = (byte)(A.serialport.serial_data >> 7);
									A.serialport.coming_in = (byte)((status_byte >> bit_count) & 1);
								}

								received_byte |= (byte)(A.serialport.going_out << bit_count);
							}
							else if (ping_player == 2)
							{
								if ((B.serialport.clk_rate == -1) && B.serialport.serial_control.Bit(7))
								{
									B.serialport.serial_clock = 1;
									B.serialport.going_out = (byte)(B.serialport.serial_data >> 7);
									B.serialport.coming_in = (byte)((status_byte >> bit_count) & 1);
								}

								received_byte |= (byte)(B.serialport.going_out << bit_count);
							}
							else if (ping_player == 3)
							{
								if ((C.serialport.clk_rate == -1) && C.serialport.serial_control.Bit(7))
								{
									C.serialport.serial_clock = 1;
									C.serialport.going_out = (byte)(C.serialport.serial_data >> 7);
									C.serialport.coming_in = (byte)((status_byte >> bit_count) & 1);
								}

								received_byte |= (byte)(C.serialport.going_out << bit_count);
							}
							else
							{
								if ((D.serialport.clk_rate == -1) && D.serialport.serial_control.Bit(7))
								{
									D.serialport.serial_clock = 1;
									D.serialport.going_out = (byte)(D.serialport.serial_data >> 7);
									D.serialport.coming_in = (byte)((status_byte >> bit_count) & 1);
								}

								received_byte |= (byte)(D.serialport.going_out << bit_count);
							}

							bit_count--;
							x4_clock = 512;

							if (bit_count == -1)
							{
								is_pinging = false;
								x4_clock = 64;

								// player one can start the transmission phase
								if ((received_byte == 0xAA) && (ping_player == 1))
								{
									begin_transmitting_cnt += 1;

									if ((begin_transmitting_cnt >= 1) && (ping_byte == 3))
									{
										pre_transmit = true;
										is_pinging = false;
										ready_to_transmit = false;
										transmit_byte = 0;
										bit_count = 7;
									}
								}

								if (((received_byte & 0x88) == 0x88) && (ping_byte <= 2))
								{
									status_byte |= (byte)(1 << (3 + ping_player));
								}

								if ((ping_player == 1) && (ping_byte == 3) && !pre_transmit)
								{
									transmit_speed = received_byte;
								}

								//Console.WriteLine(ping_player + " " + ping_byte + " " + status_byte + " " + received_byte);

								bit_count = 7;
								received_byte = 0;

								ping_byte++;

								if (ping_byte == 4)
								{
									ping_byte = 0;
									ping_player++;

									if (ping_player == 5) { ping_player = 1; }

									begin_transmitting_cnt = 0;

									status_byte &= 0xF0;
									status_byte |= (byte)ping_player;

									time_out_check = true;
									x4_clock = 64;
								}
							}
						}
					}
					else if (pre_transmit)
					{
						if (ready_to_transmit)
						{
							// send four byte of 0xCC to signal start of transmitting

							// fill the buffer
							A.serialport.serial_clock = 1;
							A.serialport.going_out = (byte)(A.serialport.serial_data >> 7);
							A.serialport.coming_in = (byte)((0xCC >> bit_count) & 1);

							if ((status_byte & 0x20) == 0x20)
							{
								B.serialport.serial_clock = 1;
								B.serialport.going_out = (byte)(B.serialport.serial_data >> 7);
								B.serialport.coming_in = (byte)((0xCC >> bit_count) & 1);
							}

							if ((status_byte & 0x40) == 0x40)
							{
								C.serialport.serial_clock = 1;
								C.serialport.going_out = (byte)(C.serialport.serial_data >> 7);
								C.serialport.coming_in = (byte)((0xCC >> bit_count) & 1);
							}

							if ((status_byte & 0x80) == 0x80)
							{
								D.serialport.serial_clock = 1;
								D.serialport.going_out = (byte)(D.serialport.serial_data >> 7);
								D.serialport.coming_in = (byte)((0xCC >> bit_count) & 1);
							}

							bit_count--;
							x4_clock = 512;

							if (bit_count == -1)
							{
								bit_count = 7;
								x4_clock = 64;
								ready_to_transmit = false;

								transmit_byte++;

								if (transmit_byte == 4)
								{
									pre_transmit = false;
									is_transmitting = true;
									transmit_byte = 0;
									buffer_parity = false;
								}
							}
						}
						else
						{
							if ((A.serialport.clk_rate == -1) && A.serialport.serial_control.Bit(7))
							{
								ready_to_transmit = true;

								if ((status_byte & 0x20) == 0x20)
								{
									if (!((B.serialport.clk_rate == -1) && B.serialport.serial_control.Bit(7))) { ready_to_transmit = false; }
								}
								if ((status_byte & 0x40) == 0x40)
								{
									if (!((C.serialport.clk_rate == -1) && C.serialport.serial_control.Bit(7))) { ready_to_transmit = false; }
								}
								if ((status_byte & 0x80) == 0x80)
								{
									if (!((D.serialport.clk_rate == -1) && D.serialport.serial_control.Bit(7))) { ready_to_transmit = false; }
								}
							}

							if (ready_to_transmit)
							{
								x4_clock = 512;
							}
							else
							{
								x4_clock = 64;
							}
						}
					}
					else
					{
						x4_clock = 64;

						// wiat for a gameboy to request a ping. Timeout and go to the next one if nothing happening for some time.
						if ((ping_player == 1) && ((A.serialport.serial_control & 0x81) == 0x80))
						{
							is_pinging = true;
							x4_clock = 512;
							time_out_check = false;
						}
						else if ((ping_player == 2) && ((B.serialport.serial_control & 0x81) == 0x80))
						{
							is_pinging = true;
							x4_clock = 512;
							time_out_check = false;
						}
						else if ((ping_player == 3) && ((C.serialport.serial_control & 0x81) == 0x80))
						{
							is_pinging = true;
							x4_clock = 512;
							time_out_check = false;
						}
						else if ((ping_player == 4) && ((D.serialport.serial_control & 0x81) == 0x80))
						{
							is_pinging = true;
							x4_clock = 512;
							time_out_check = false;
						}

						if (time_out_check)
						{
							ping_player++;

							if (ping_player == 5) { ping_player = 1; }

							status_byte &= 0xF0;
							status_byte |= (byte)ping_player;

							x4_clock = 64;
						}
					}
				}
				
				// if we hit a frame boundary, update video
				if (A.vblank_rise)
				{
					// update the controller state on VBlank
					A.controller_state = A_controller;

					// check if controller state caused interrupt
					A.do_controller_check();

					// send the image on VBlank
					A.SendVideoBuffer();

					A.vblank_rise = false;
					do_frame_fill = true;
				}
				if (B.vblank_rise)
				{
					// update the controller state on VBlank
					B.controller_state = B_controller;

					// check if controller state caused interrupt
					B.do_controller_check();

					// send the image on VBlank
					B.SendVideoBuffer();

					B.vblank_rise = false;
					do_frame_fill = true;
				}
				if (C.vblank_rise)
				{
					// update the controller state on VBlank
					C.controller_state = C_controller;

					// check if controller state caused interrupt
					C.do_controller_check();

					// send the image on VBlank
					C.SendVideoBuffer();

					C.vblank_rise = false;
					do_frame_fill = true;
				}
				if (D.vblank_rise)
				{
					// update the controller state on VBlank
					D.controller_state = D_controller;

					// check if controller state caused interrupt
					D.do_controller_check();

					// send the image on VBlank
					D.SendVideoBuffer();

					D.vblank_rise = false;
					do_frame_fill = true;
				}
			}
		}

		public void do_frame_2x2()
		{
			// advance one full frame
			for (int i = 0; i < 70224; i++)
			{
				A.do_single_step();
				B.do_single_step();
				C.do_single_step();
				D.do_single_step();

				if (_cableconnected_UD)
				{
					// the signal to shift out a bit is when serial_clock = 1
					if (((A.serialport.serial_clock == 1) || (A.serialport.serial_clock == 2)) && (A.serialport.clk_rate > 0) && !do_2_next_1)
					{
						A.serialport.going_out = (byte)(A.serialport.serial_data >> 7);

						if ((B.serialport.clk_rate == -1) && A.serialport.can_pulse)
						{
							B.serialport.serial_clock = A.serialport.serial_clock;
							B.serialport.going_out = (byte)(B.serialport.serial_data >> 7);
							B.serialport.coming_in = A.serialport.going_out;
						}

						A.serialport.coming_in = B.serialport.going_out;
						A.serialport.can_pulse = false;
					}
					else if (((B.serialport.serial_clock == 1) || (B.serialport.serial_clock == 2)) && (B.serialport.clk_rate > 0))
					{
						do_2_next_1 = false;

						B.serialport.going_out = (byte)(B.serialport.serial_data >> 7);

						if ((A.serialport.clk_rate == -1) && B.serialport.can_pulse)
						{
							A.serialport.serial_clock = B.serialport.serial_clock;
							A.serialport.going_out = (byte)(A.serialport.serial_data >> 7);
							A.serialport.coming_in = B.serialport.going_out;
						}

						B.serialport.coming_in = A.serialport.going_out;
						B.serialport.can_pulse = false;

						if (B.serialport.serial_clock == 2) { do_2_next_1 = true; }
					}
					else
					{
						do_2_next_1 = false;
					}

					// the signal to shift out a bit is when serial_clock = 1
					if (((C.serialport.serial_clock == 1) || (C.serialport.serial_clock == 2)) && (C.serialport.clk_rate > 0) && !do_2_next_2)
					{
						C.serialport.going_out = (byte)(C.serialport.serial_data >> 7);

						if ((D.serialport.clk_rate == -1) && C.serialport.can_pulse)
						{
							D.serialport.serial_clock = C.serialport.serial_clock;
							D.serialport.going_out = (byte)(D.serialport.serial_data >> 7);
							D.serialport.coming_in = C.serialport.going_out;
						}

						C.serialport.coming_in = D.serialport.going_out;
						C.serialport.can_pulse = false;
					}
					else if (((D.serialport.serial_clock == 1) || (D.serialport.serial_clock == 2)) && (D.serialport.clk_rate > 0))
					{
						do_2_next_2 = false;

						D.serialport.going_out = (byte)(D.serialport.serial_data >> 7);

						if ((C.serialport.clk_rate == -1) && D.serialport.can_pulse)
						{
							C.serialport.serial_clock = D.serialport.serial_clock;
							C.serialport.going_out = (byte)(C.serialport.serial_data >> 7);
							C.serialport.coming_in = D.serialport.going_out;
						}

						D.serialport.coming_in = C.serialport.going_out;
						D.serialport.can_pulse = false;

						if (D.serialport.serial_clock == 2) { do_2_next_2 = true; }
					}
					else
					{
						do_2_next_2 = false;
					}
				}
				else if (_cableconnected_LR)
				{
					// the signal to shift out a bit is when serial_clock = 1
					if (((A.serialport.serial_clock == 1) || (A.serialport.serial_clock == 2)) && (A.serialport.clk_rate > 0) && !do_2_next_1)
					{
						A.serialport.going_out = (byte)(A.serialport.serial_data >> 7);

						if ((D.serialport.clk_rate == -1) && A.serialport.can_pulse)
						{
							D.serialport.serial_clock = A.serialport.serial_clock;
							D.serialport.going_out = (byte)(D.serialport.serial_data >> 7);
							D.serialport.coming_in = A.serialport.going_out;
						}

						A.serialport.coming_in = D.serialport.going_out;
						A.serialport.can_pulse = false;
					}
					else if (((D.serialport.serial_clock == 1) || (D.serialport.serial_clock == 2)) && (D.serialport.clk_rate > 0))
					{
						do_2_next_1 = false;

						D.serialport.going_out = (byte)(D.serialport.serial_data >> 7);

						if ((A.serialport.clk_rate == -1) && D.serialport.can_pulse)
						{
							A.serialport.serial_clock = D.serialport.serial_clock;
							A.serialport.going_out = (byte)(A.serialport.serial_data >> 7);
							A.serialport.coming_in = D.serialport.going_out;
						}

						D.serialport.coming_in = A.serialport.going_out;
						D.serialport.can_pulse = false;

						if (D.serialport.serial_clock == 2) { do_2_next_1 = true; }
					}
					else
					{
						do_2_next_1 = false;
					}

					// the signal to shift out a bit is when serial_clock = 1
					if (((B.serialport.serial_clock == 1) || (B.serialport.serial_clock == 2)) && (B.serialport.clk_rate > 0) && !do_2_next_2)
					{
						B.serialport.going_out = (byte)(B.serialport.serial_data >> 7);

						if ((C.serialport.clk_rate == -1) && B.serialport.can_pulse)
						{
							C.serialport.serial_clock = B.serialport.serial_clock;
							C.serialport.going_out = (byte)(C.serialport.serial_data >> 7);
							C.serialport.coming_in = B.serialport.going_out;
						}

						B.serialport.coming_in = C.serialport.going_out;
						B.serialport.can_pulse = false;
					}
					else if (((C.serialport.serial_clock == 1) || (C.serialport.serial_clock == 2)) && (C.serialport.clk_rate > 0))
					{
						do_2_next_2 = false;

						C.serialport.going_out = (byte)(C.serialport.serial_data >> 7);

						if ((B.serialport.clk_rate == -1) && C.serialport.can_pulse)
						{
							B.serialport.serial_clock = C.serialport.serial_clock;
							B.serialport.going_out = (byte)(B.serialport.serial_data >> 7);
							B.serialport.coming_in = C.serialport.going_out;
						}

						C.serialport.coming_in = B.serialport.going_out;
						C.serialport.can_pulse = false;

						if (C.serialport.serial_clock == 2) { do_2_next_2 = true; }
					}
					else
					{
						do_2_next_2 = false;
					}
				}
				else if (_cableconnected_X)
				{
					// the signal to shift out a bit is when serial_clock = 1
					if (((C.serialport.serial_clock == 1) || (C.serialport.serial_clock == 2)) && (C.serialport.clk_rate > 0) && !do_2_next_1)
					{
						C.serialport.going_out = (byte)(C.serialport.serial_data >> 7);

						if ((A.serialport.clk_rate == -1) && C.serialport.can_pulse)
						{
							A.serialport.serial_clock = C.serialport.serial_clock;
							A.serialport.going_out = (byte)(A.serialport.serial_data >> 7);
							A.serialport.coming_in = C.serialport.going_out;
						}

						C.serialport.coming_in = A.serialport.going_out;
						C.serialport.can_pulse = false;
					}
					else if (((A.serialport.serial_clock == 1) || (A.serialport.serial_clock == 2)) && (A.serialport.clk_rate > 0))
					{
						do_2_next_1 = false;

						A.serialport.going_out = (byte)(A.serialport.serial_data >> 7);

						if ((C.serialport.clk_rate == -1) && A.serialport.can_pulse)
						{
							C.serialport.serial_clock = A.serialport.serial_clock;
							C.serialport.going_out = (byte)(C.serialport.serial_data >> 7);
							C.serialport.coming_in = A.serialport.going_out;
						}

						A.serialport.coming_in = C.serialport.going_out;
						A.serialport.can_pulse = false;

						if (A.serialport.serial_clock == 2) { do_2_next_1 = true; }
					}
					else
					{
						do_2_next_1 = false;
					}

					// the signal to shift out a bit is when serial_clock = 1
					if (((B.serialport.serial_clock == 1) || (B.serialport.serial_clock == 2)) && (B.serialport.clk_rate > 0) && !do_2_next_2)
					{
						B.serialport.going_out = (byte)(B.serialport.serial_data >> 7);

						if ((D.serialport.clk_rate == -1) && B.serialport.can_pulse)
						{
							D.serialport.serial_clock = B.serialport.serial_clock;
							D.serialport.going_out = (byte)(D.serialport.serial_data >> 7);
							D.serialport.coming_in = B.serialport.going_out;
						}

						B.serialport.coming_in = D.serialport.going_out;
						B.serialport.can_pulse = false;
					}
					else if (((D.serialport.serial_clock == 1) || (D.serialport.serial_clock == 2)) && (D.serialport.clk_rate > 0))
					{
						do_2_next_2 = false;

						D.serialport.going_out = (byte)(D.serialport.serial_data >> 7);

						if ((B.serialport.clk_rate == -1) && D.serialport.can_pulse)
						{
							B.serialport.serial_clock = D.serialport.serial_clock;
							B.serialport.going_out = (byte)(B.serialport.serial_data >> 7);
							B.serialport.coming_in = D.serialport.going_out;
						}

						D.serialport.coming_in = B.serialport.going_out;
						D.serialport.can_pulse = false;

						if (D.serialport.serial_clock == 2) { do_2_next_2 = true; }
					}
					else
					{
						do_2_next_2 = false;
					}
				}


				// if we hit a frame boundary, update video
				if (A.vblank_rise)
				{
					// update the controller state on VBlank
					A.controller_state = A_controller;

					// check if controller state caused interrupt
					A.do_controller_check();

					// send the image on VBlank
					A.SendVideoBuffer();

					A.vblank_rise = false;
					do_frame_fill = true;
				}
				if (B.vblank_rise)
				{
					// update the controller state on VBlank
					B.controller_state = B_controller;

					// check if controller state caused interrupt
					B.do_controller_check();

					// send the image on VBlank
					B.SendVideoBuffer();

					B.vblank_rise = false;
					do_frame_fill = true;
				}
				if (C.vblank_rise)
				{
					// update the controller state on VBlank
					C.controller_state = C_controller;

					// check if controller state caused interrupt
					C.do_controller_check();

					// send the image on VBlank
					C.SendVideoBuffer();

					C.vblank_rise = false;
					do_frame_fill = true;
				}
				if (D.vblank_rise)
				{
					// update the controller state on VBlank
					D.controller_state = D_controller;

					// check if controller state caused interrupt
					D.do_controller_check();

					// send the image on VBlank
					D.SendVideoBuffer();

					D.vblank_rise = false;
					do_frame_fill = true;
				}
			}

			// clear the screens as needed
			if (A.ppu.clear_screen)
			{
				A.clear_screen_func();
				do_frame_fill = true;
			}

			if (B.ppu.clear_screen)
			{
				B.clear_screen_func();
				do_frame_fill = true;
			}

			if (C.ppu.clear_screen)
			{
				C.clear_screen_func();
				do_frame_fill = true;
			}

			if (D.ppu.clear_screen)
			{
				D.clear_screen_func();
				do_frame_fill = true;
			}
		}

		public void GetControllerState(IController controller)
		{
			InputCallbacks.Call();
			A_controller = _controllerDeck.ReadPort1(controller);
			B_controller = _controllerDeck.ReadPort2(controller);
			C_controller = _controllerDeck.ReadPort3(controller);
			D_controller = _controllerDeck.ReadPort4(controller);
		}

		public int Frame => _frame;

		public string SystemId => VSystemID.Raw.GBL;

		public bool DeterministicEmulation { get; set; }

		public void ResetCounters()
		{
			_frame = 0;
			_lagcount = 0;
			_islag = false;
		}

		public void Dispose()
		{
			A.Dispose();
			B.Dispose();
			C.Dispose();
			D.Dispose();
		}

		public int[] _vidbuffer = new int[160 * 2 * 144 * 2];

		public int[] GetVideoBuffer()
		{
			return _vidbuffer;		
		}

		public void FillVideoBuffer()
		{
			// combine the 2 video buffers from the instances
			for (int i = 0; i < 144; i++)
			{
				for (int j = 0; j < 160; j++)
				{
					_vidbuffer[i * 320 + j] = A.frame_buffer[i * 160 + j];
					_vidbuffer[(i + 144) * 320 + j] = B.frame_buffer[i * 160 + j];
					_vidbuffer[(i + 144) * 320 + j + 160] = C.frame_buffer[i * 160 + j];
					_vidbuffer[i * 320 + j + 160] = D.frame_buffer[i * 160 + j];
				}
			}
		}

		public int VirtualWidth => 160 * 2;
		public int VirtualHeight => 144 * 2;
		public int BufferWidth => 160 * 2;
		public int BufferHeight => 144 * 2;
		public int BackgroundColor => unchecked((int)0xFF000000);
		public int VsyncNumerator => 262144;
		public int VsyncDenominator => 4389;

		public static readonly uint[] color_palette_BW = { 0xFFFFFFFF , 0xFFAAAAAA, 0xFF555555, 0xFF000000 };
		public static readonly uint[] color_palette_Gr = { 0xFFA4C505, 0xFF88A905, 0xFF1D551D, 0xFF052505 };

		public uint[] color_palette = new uint[4];

		public bool CanProvideAsync => false;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Sync)
			{
				throw new InvalidOperationException("Only Sync mode is supported_");
			}
		}

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			A.audio.GetSamplesSync(out var temp_samp_A, out var nsamp_A);
			B.audio.GetSamplesSync(out var temp_samp_B, out var nsamp_B);
			C.audio.GetSamplesSync(out var temp_samp_C, out var nsamp_C);
			D.audio.GetSamplesSync(out var temp_samp_D, out var nsamp_D);

			if (Link4xSettings.AudioSet == GBLink4xSettings.AudioSrc.A)
			{
				samples = temp_samp_A;
				nsamp = nsamp_A;
			}
			else if (Link4xSettings.AudioSet == GBLink4xSettings.AudioSrc.B)
			{
				samples = temp_samp_B;
				nsamp = nsamp_B;
			}
			else if (Link4xSettings.AudioSet == GBLink4xSettings.AudioSrc.C)
			{
				samples = temp_samp_C;
				nsamp = nsamp_C;
			}
			else if (Link4xSettings.AudioSet == GBLink4xSettings.AudioSrc.D)
			{
				samples = temp_samp_D;
				nsamp = nsamp_D;
			}
			else
			{
				samples = new short[0];
				nsamp = 0;
			}
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new NotSupportedException("Async is not available");
		}

		public void DiscardSamples()
		{
			A.audio.DiscardSamples();
			B.audio.DiscardSamples();
			C.audio.DiscardSamples();
			D.audio.DiscardSamples();
		}

		private void GetSamples(short[] samples)
		{

		}

		public void DisposeSound()
		{
			A.audio.DisposeSound();
			B.audio.DisposeSound();
			C.audio.DisposeSound();
			D.audio.DisposeSound();
		}
	}
}
