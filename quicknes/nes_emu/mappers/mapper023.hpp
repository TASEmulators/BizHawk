/* Copyright notice for this file:
 *  Copyright (C) 2004-2006 Shay Green
 *  Copyright (C) 2007 CaH4e3
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 * 
 * General code is from FCEUX https://sourceforge.net/p/fceultra/code/HEAD/tree/fceu/trunk/src/boards/vrc2and4.cpp
 * IRQ portion is from existing VRC6/VRC7 by Shay Green
 * This mapper was ported by retrowertz for Libretro port of QuickNES.
 * 3-19-2018
 *
 * VRC-2/VRC-4 Konami
 */

#pragma once
#include "Nes_Mapper.h"

typedef Mapper_VRC2_4<false, false> Mapper023;