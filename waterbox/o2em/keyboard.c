/*
 *   O2EM Free Odyssey2 / Videopac+ Emulator
 *
 *   Created by Daniel Boris <dboris@comcast.net>  (c) 1997,1998
 *
 *   Developed by Andre de la Rocha   <adlroc@users.sourceforge.net>
 *             Arlindo M. de Oliveira <dgtec@users.sourceforge.net>
 *
 *   http://o2em.sourceforge.net
 *
 *
 *
 *   Keyboard emulation
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "types.h"
#include "cpu.h"
#include "config.h"
#include "vmachine.h"
#include "vdc.h"
#include "audio.h"
#include "voice.h"
#include "vpp.h"
#include "keyboard.h"
#include "score.h"

void do_reset(void)
{
	init_cpu();
	init_roms();
	init_vpp();
	clearscr();
}

void do_highscore(void)
{
	set_score(app_data.scoretype, app_data.scoreaddress, app_data.default_highscore);
}
