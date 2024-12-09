#include "bizhawk.h"

ECL_EXPORT bool Init(int argc, char **argv)
{
	log_cb = biz_log_cb;
    libretro_runloop_active = 0;

	pix_bytes = 4;
	defaultw = PUAE_VIDEO_WIDTH;
	defaulth = PUAE_WINDOW_HEIGHT_PAL;
	retrow_crop = retrow = PUAE_VIDEO_WIDTH;
	retroh_crop = retroh = PUAE_WINDOW_HEIGHT_PAL;
	
	retro_set_audio_sample_batch(biz_audio_cb);
	init_output_audio_buffer(2048);
	umain(argc, argv);
	m68k_go(1, 0);

	libretro_runloop_active = 1;

	return true;
}

void SetJoyButtonRaw(int port, int button, int state)
{
	if (state)
		joybutton[port] |= 1 << button;
	else
		joybutton[port] &= ~(1 << button);
}

void SetJoyDirectionRaw(int port, int direction, int state)
{
	if (state)
		joydir[port] |= direction;
}

ECL_EXPORT void FrameAdvance(MyFrameInfo* f)
{
	bool is_ntsc = minfirstline == VBLANK_ENDLINE_NTSC;
    f->base.Width = PUAE_VIDEO_WIDTH;
    f->base.Height = is_ntsc ? PUAE_WINDOW_HEIGHT_NTSC : PUAE_WINDOW_HEIGHT_PAL;
	thisframe_y_adjust = minfirstline;
	visible_left_border = retro_max_diwlastword - retrow;

	sound_buffer = f->base.SoundBuffer;

	for (int port = 0; port <= 1; port++)
	{
		Controller *controller = (port == 0) ? &f->Port1 : &f->Port2;
		cd32_pad_enabled[port] = 0;
		joydir[port] = 0;

		SetJoyButtonRaw   (port, JOYBUTTON_1, controller->Buttons.b1);
		SetJoyButtonRaw   (port, JOYBUTTON_2, controller->Buttons.b2);
		SetJoyButtonRaw   (port, JOYBUTTON_3, controller->Buttons.b3);
		SetJoyDirectionRaw(port, DIR_LEFT,    controller->Buttons.left);
		SetJoyDirectionRaw(port, DIR_RIGHT,   controller->Buttons.right);
		SetJoyDirectionRaw(port, DIR_UP,      controller->Buttons.up);
		SetJoyDirectionRaw(port, DIR_DOWN,    controller->Buttons.down);

		switch (controller->Type)
		{
			case CONTROLLER_JOYSTICK:
				joymousecounter(port);
				break;
			case CONTROLLER_CD32PAD:
				cd32_pad_enabled[port] = 1;
				SetJoyButtonRaw(port, JOYBUTTON_CD32_PLAY,   controller->Buttons.play);
				SetJoyButtonRaw(port, JOYBUTTON_CD32_RWD,    controller->Buttons.rewind);
				SetJoyButtonRaw(port, JOYBUTTON_CD32_FFW,    controller->Buttons.forward);
				SetJoyButtonRaw(port, JOYBUTTON_CD32_GREEN,  controller->Buttons.green);
				SetJoyButtonRaw(port, JOYBUTTON_CD32_YELLOW, controller->Buttons.yellow);
				SetJoyButtonRaw(port, JOYBUTTON_CD32_RED,    controller->Buttons.red);
				SetJoyButtonRaw(port, JOYBUTTON_CD32_BLUE,   controller->Buttons.blue);
				joymousecounter(port);
				break;
			case CONTROLLER_MOUSE:
				mouse_delta[port][AXIS_HORIZONTAL] += controller->MouseX - last_mouse_x[port];
				mouse_delta[port][AXIS_VERTICAL  ] += controller->MouseY - last_mouse_y[port];
				break;
		}
	}

	for (int i = 0; i < KEY_COUNT; i++)
		if (f->Keys[i] != last_key_state[i])
			inputdevice_do_keyboard(i, f->Keys[i]);
	memcpy(last_key_state, f->Keys, KEY_COUNT);

	if (f->Action == ACTION_EJECT)
	{
		disk_eject(f->CurrentDrive);
		log_cb(RETRO_LOG_INFO, "EJECTED FD%d\n", f->CurrentDrive);
	}
	else if (f->Action == ACTION_INSERT)
	{
		disk_eject(f->CurrentDrive);
		disk_insert_force(f->CurrentDrive, f->FileName, true);
		log_cb(RETRO_LOG_INFO, "INSERTED FD%d: \"%s\"\n", f->CurrentDrive, f->FileName);
	}

	m68k_go(1, 1);
	upload_output_audio_buffer();
	f->base.Samples = sound_sample_count;
	memcpy(f->base.VideoBuffer, retro_bmp, f->base.Width * f->base.Height * pix_bytes);
	sound_buffer = NULL;

	for (int port = 0; port <= 1; port++)
	{
		Controller *controller = (port == 0) ? &f->Port1 : &f->Port2;

		if (controller->Type == CONTROLLER_MOUSE)
		{
			last_mouse_x[port] = controller->MouseX;
			last_mouse_y[port] = controller->MouseY;
		}
	}
}

ECL_EXPORT void GetMemoryAreas(MemoryArea *m)
{
	m[0].Data  = chipmem_bank.baseaddr;
	m[0].Name  = "Chip RAM";
	m[0].Size  = chipmem_bank.allocated_size;
	m[0].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_PRIMARY;

	m[1].Data  = bogomem_bank.baseaddr;
	m[1].Name  = "Slow RAM";
	m[1].Size  = bogomem_bank.allocated_size;
	m[1].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE;

	m[2].Data  = fastmem_bank[0].baseaddr;
	m[2].Name  = "Fast RAM";
	m[2].Size  = fastmem_bank[0].allocated_size;
	m[2].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE;
}

void (*LEDCallback)();
ECL_EXPORT void SetLEDCallback(void (*callback)())
{
	LEDCallback = callback;
}

void (*InputCallback)();
ECL_EXPORT void SetInputCallback(void (*callback)())
{
	InputCallback = callback;
}
