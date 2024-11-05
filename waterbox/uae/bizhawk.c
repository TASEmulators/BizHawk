#include "bizhawk.h"

ECL_EXPORT bool Init(int argc, char **argv)
{
	last_mouse_x = 0;
	last_mouse_y = 0;
    libretro_runloop_active = 0;
	pix_bytes = 4;
	defaultw = PUAE_VIDEO_WIDTH;
	defaulth = PUAE_VIDEO_HEIGHT_PAL;
	retrow = PUAE_VIDEO_WIDTH;
	retrow_crop = retrow;
	retroh_crop = retroh;
	log_cb = biz_log_cb;
	
	retro_set_audio_sample_batch(biz_audio_cb);
	init_output_audio_buffer(2048);
	umain(argc, argv);
	m68k_go(1, 0);

	libretro_runloop_active = 1;

	return true;
}

void SetCD32ButtonState(int port, int button, int state)
{
	if (state)
		joybutton[port] |= 1 << button;
	else
		joybutton[port] &= ~(1 << button);
}

ECL_EXPORT void FrameAdvance(MyFrameInfo* f)
{
	
	cd32_pad_enabled[0] = 1;
	cd32_pad_enabled[1] = 1;
	bool is_ntsc = minfirstline == VBLANK_ENDLINE_NTSC;
    f->base.Width = PUAE_VIDEO_WIDTH;
    f->base.Height = is_ntsc ? PUAE_VIDEO_HEIGHT_NTSC : PUAE_VIDEO_HEIGHT_PAL;
	sound_buffer = f->base.SoundBuffer;
	thisframe_y_adjust = minfirstline;
	visible_left_border = retro_max_diwlastword - retrow;

	setjoystickstate(PORT_0, AXIS_VERTICAL,
		f->JoystickState.up   ? JOY_MIN :
		f->JoystickState.down ? JOY_MAX : JOY_MID, 1);
	setjoystickstate(PORT_0, AXIS_HORIZONTAL,
		f->JoystickState.left  ? JOY_MIN :
		f->JoystickState.right ? JOY_MAX : JOY_MID, 1);
	setjoybuttonstate(PORT_0, JOYBUTTON_1, f->JoystickState.b1);
	setjoybuttonstate(PORT_0, JOYBUTTON_2, f->JoystickState.b2);
	setjoybuttonstate(PORT_0, JOYBUTTON_3, f->JoystickState.b3);

	SetCD32ButtonState(PORT_0, JOYBUTTON_CD32_PLAY,   f->JoystickState.b4);
	SetCD32ButtonState(PORT_0, JOYBUTTON_CD32_RWD,    f->JoystickState.b5);
	SetCD32ButtonState(PORT_0, JOYBUTTON_CD32_FFW,    f->JoystickState.b6);
	SetCD32ButtonState(PORT_0, JOYBUTTON_CD32_GREEN,  f->JoystickState.b7);
	SetCD32ButtonState(PORT_0, JOYBUTTON_CD32_YELLOW, f->JoystickState.b8);
	SetCD32ButtonState(PORT_0, JOYBUTTON_CD32_RED,    f->JoystickState.b9);
	SetCD32ButtonState(PORT_0, JOYBUTTON_CD32_BLUE,   f->JoystickState.b10);

	setmousebuttonstate(PORT_0, MOUSE_LEFT,      f->MouseButtons & 1);
	setmousebuttonstate(PORT_0, MOUSE_RIGHT,     f->MouseButtons & 2);
	setmousebuttonstate(PORT_0, MOUSE_MIDDLE,    f->MouseButtons & 4);
	setmousestate(      PORT_0, AXIS_HORIZONTAL, f->MouseX - last_mouse_x, MOUSE_RELATIVE);
	setmousestate(      PORT_0, AXIS_VERTICAL,   f->MouseY - last_mouse_y, MOUSE_RELATIVE);

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
	memcpy(f->base.VideoBuffer, retro_bmp, sizeof(retro_bmp) / sizeof(retro_bmp[0]));
	last_mouse_x = f->MouseX;
	last_mouse_y = f->MouseY;
	sound_buffer = NULL;
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

void (*InputCallback)();

ECL_EXPORT void SetInputCallback(void (*callback)())
{
	InputCallback = callback;
}
