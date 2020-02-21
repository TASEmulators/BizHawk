//	Altirra - Atari 800/800XL/5200 emulator
//	Native network interface library - VXLAN tunnel
//	Copyright (C) 2009-2015 Avery Lee
//
//	This program is free software; you can redistribute it and/or modify
//	it under the terms of the GNU General Public License as published by
//	the Free Software Foundation; either version 2 of the License, or
//	(at your option) any later version.
//
//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License
//	along with this program; if not, write to the Free Software
//	Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.

#ifndef f_AT_ATNETWORKSOCKETS_VXLANTUNNEL_H
#define f_AT_ATNETWORKSOCKETS_VXLANTUNNEL_H

class IATNetSockVxlanTunnel : public IVDRefCount {
public:
};

void ATCreateNetSockVxlanTunnel(uint32 tunnelAddr, uint16 tunnelSrcPort, uint16 tunnelDstPort, IATEthernetSegment *ethSeg, uint32 ethClockIndex, IATNetSockVxlanTunnel **pp);

#endif
