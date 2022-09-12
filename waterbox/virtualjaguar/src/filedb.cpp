//
// filedb.cpp - File database
//
// by James Hammons
// (C) 2010 Underground Software
//
// JLH = James Hammons <jlhamm@acm.org>
//
// Who  When        What
// ---  ----------  ------------------------------------------------------------
// JLH  02/15/2010  Created this file
//

#include "filedb.h"


#if 0
struct RomIdentifier
{
	const uint32_t crc32;
	const char name[128];
//	const uint8_t compatibility;
	const uint32_t flags;
};

enum FileFlags { FF_ROM=1, FF_ALPINE=2, FF_BIOS=4, FF_REQ_DSP=8, FF_REQ_BIOS=16, FF_NON_WORKING=32, FF_BAD_DUMP=64, FF_VERIFIED=128 };
#endif

// Should have another flag for whether or not it requires DSP, BIOS,
// whether it's a .rom, it's a BIOS, etc...
// ... And now we do! :-D

// How the CRCs work:
// If the cart has an RSA signature, we do a CRC on the whole file.
// If the cart has a universal header, we do a CRC on the file minus the UH.
// This is to ensure that we can detect something properly (like an Alpine ROM)
// that somebody slapped a universal header on.

RomIdentifier romList[] = {
	{ 0x0509C85E, "Raiden (World) (alt)", FF_ROM | FF_REQ_BIOS },
	{ 0x08849D0F, "Hyper Force (World)", FF_ALPINE | FF_VERIFIED },
	{ 0x08F15576, "Iron Soldier (World) (v1.04)", FF_ROM | FF_VERIFIED },
	{ 0x0957A072, "Kasumi Ninja (World)", FF_ROM | FF_VERIFIED },
	{ 0x0AC83D77, "NBA Jam T.E. (World)", FF_ROM | FF_VERIFIED },
	{ 0x0EC5369D, "Evolution - Dino Dudes (World)", FF_ROM | FF_VERIFIED },
	{ 0x0F6A1C2C, "Ultra Vortek (World)", FF_ROM | FF_VERIFIED },
	{ 0x0FDCEB66, "Brutal Sports Football (World)", FF_ROM | FF_BAD_DUMP },
	{ 0x14915F20, "White Men Can't Jump (World)", FF_ROM | FF_VERIFIED },
	{ 0x1660F070, "Power Drive Rally (World)", FF_ROM | FF_VERIFIED },
	{ 0x1A20C5C4, "Protector (World)", FF_ROM | FF_VERIFIED | FF_REQ_DSP },
	{ 0x1E451446, "Trevor McFur in the Crescent Galaxy (World)", FF_ROM | FF_VERIFIED },
	{ 0x20936557, "Space War 2000", FF_ALPINE | FF_VERIFIED },
	{ 0x27594C6A, "Defender 2000 (World)", FF_ROM | FF_VERIFIED },
	{ 0x2BAA92A1, "Space War 2000 (World) (OVERDUMP)", FF_ALPINE },
	{ 0x2E17D5DA, "Bubsy in Fractured Furry Tales (World)", FF_ROM | FF_VERIFIED },
	{ 0x31812799, "Raiden (World)", FF_ROM | FF_VERIFIED },
	{ 0x3241AB6A, "Towers II", FF_ALPINE },
	{ 0x348E6449, "Double Dragon V - The Shadow Falls (World)", FF_ROM | FF_VERIFIED },
	{ 0x3615AF6A, "Fever Pitch Soccer (World) (En,Fr,De,Es,It)", FF_ROM | FF_VERIFIED },
	{ 0x38A130ED, "Troy Aikman NFL Football (World)", FF_ROM | FF_VERIFIED },
	{ 0x40E1A1D0, "Air Cars (World)", FF_ROM | FF_VERIFIED },
	{ 0x4471BFA0, "Skyhammer (World)", FF_ALPINE | FF_VERIFIED },
	{ 0x47EBC158, "Theme Park (World)", FF_ROM | FF_VERIFIED },
	{ 0x4899628F, "Hover Strike (World)", FF_ROM | FF_VERIFIED },
	{ 0x4A08A2BD, "SuperCross 3D (World)", FF_ROM | FF_BAD_DUMP },
	{ 0x544E7A01, "Downfall (World)", FF_ROM | FF_VERIFIED },
	{ 0x55A0669C, "[BIOS] Atari Jaguar Developer CD (World)", FF_BIOS },
	{ 0x58272540, "Syndicate (World)", FF_ROM | FF_VERIFIED },
	{ 0x5A101212, "Sensible Soccer - International Edition (World)", FF_ROM | FF_VERIFIED },
	{ 0x5B6BB205, "Ruiner Pinball (World)", FF_ROM | FF_VERIFIED },
	{ 0x5CFF14AB, "Pinball Fantasies (World)", FF_ROM | FF_VERIFIED },
	{ 0x5DDF9724, "Protector - Special Edition (World)", FF_ALPINE | FF_VERIFIED },
	{ 0x5E2CDBC0, "Doom (World)", FF_ROM | FF_VERIFIED | FF_REQ_DSP },
	{ 0x5F2C2774, "Battle Sphere (World)", FF_ROM | FF_VERIFIED | FF_REQ_DSP },
	{ 0x61C7EEC0, "Zero 5 (World)", FF_ROM | FF_VERIFIED },
	{ 0x61EE6B62, "Arena Football '95", FF_ALPINE | FF_VERIFIED },
	{ 0x67F9AB3A, "Battle Sphere Gold (World)", FF_ROM | FF_REQ_DSP },
	{ 0x687068D5, "[BIOS] Atari Jaguar CD (World)", FF_BIOS },
	{ 0x6B2B95AD, "Tempest 2000 (World)", FF_ROM | FF_VERIFIED },
	{ 0x6EB774EB, "Worms (World)", FF_ROM | FF_VERIFIED },
	{ 0x6F8B2547, "Super Burnout (World)", FF_ROM | FF_VERIFIED },
	{ 0x732FFAB6, "Soccer Kid (World)", FF_ROM | FF_VERIFIED },
	{ 0x817A2273, "Pitfall - The Mayan Adventure (World)", FF_ROM | FF_VERIFIED },
	{ 0x83A3FB5D, "Towers II", FF_ROM | FF_VERIFIED },
	{ 0x85919165, "Superfly DX (v1.1)", FF_ROM | FF_VERIFIED },
	{ 0x892BC67C, "Flip Out! (World)", FF_ROM | FF_VERIFIED },
	{ 0x8975F48B, "Zool 2 (World)", FF_ROM | FF_VERIFIED },
	{ 0x89DA21FF, "Phase Zero", FF_ALPINE | FF_VERIFIED | FF_REQ_DSP },
	{ 0x8D15DBC6, "[BIOS] Atari Jaguar Stubulator '94 (World)", FF_BIOS },
	{ 0x8FEA5AB0, "Dragon - The Bruce Lee Story (World)", FF_ROM | FF_VERIFIED },
	{ 0x91095DD3, "Brett Hull Hockey", FF_ROM | FF_VERIFIED },
	{ 0x95143668, "Trevor McFur in the Crescent Galaxy (World) (alt)", FF_ROM | FF_VERIFIED },
	{ 0x97EB4651, "I-War (World)", FF_ROM | FF_VERIFIED },
	{ 0xA0A25A67, "Missile Command VR", FF_ALPINE },
	{ 0xA27823D8, "Ultra Vortek (World) (v0.94) (Beta)", FF_ROM },
	{ 0xA7E01FEF, "Mad Bodies (2008)", FF_ROM },
	{ 0xA9F8A00E, "Rayman (World)", FF_ROM | FF_VERIFIED },
	{ 0xAEA9D831, "Barkley Shut Up & Jam", FF_ROM | FF_VERIFIED },
	{ 0xB14C4753, "Fight for Life (World)", FF_ROM | FF_VERIFIED },
	{ 0xB5604D40, "Breakout 2000", FF_ROM | FF_VERIFIED },
	{ 0xBA13AE79, "Soccer Kid (World) (alt)", FF_ALPINE },
	{ 0xBCB1A4BF, "Brutal Sports Football (World)", FF_ROM | FF_VERIFIED },
	{ 0xBD18D606, "Space War 2000 (World) (alt)", FF_ALPINE },
	{ 0xBDA405C6, "Cannon Fodder (World)", FF_ROM | FF_VERIFIED },
	{ 0xBDE67498, "Cybermorph (World) (Rev 1)", FF_ROM | FF_VERIFIED | FF_REQ_DSP },
	{ 0xC2898F6E, "Barkley Shut Up & Jam (alt)", FF_ALPINE },
	{ 0xC36E935E, "Beebris (World)", FF_ALPINE | FF_VERIFIED },
	{ 0xC5562581, "Zoop! (World)", FF_ROM | FF_VERIFIED },
	{ 0xC654681B, "Total Carnage (World)", FF_ROM | FF_VERIFIED },
	{ 0xC6C7BA62, "Fight for Life (World) (alt)", FF_ROM | FF_BAD_DUMP },
	{ 0xC9608717, "Val d'Isere Skiing and Snowboarding (World)", FF_ROM | FF_VERIFIED },
	{ 0xCBFD822A, "Air Cars (World) (alt)", FF_ROM | FF_BAD_DUMP },
	{ 0xCD5BF827, "Attack of the Mutant Penguins (World)", FF_ROM | FF_VERIFIED | FF_REQ_DSP },
	{ 0xD6C19E34, "Iron Soldier 2 (World)", FF_ROM | FF_VERIFIED },
	{ 0xD8696F23, "Breakout 2000 (alt)", FF_ALPINE },
	{ 0xDA9C4162, "Missile Command 3D (World)", FF_ROM | FF_VERIFIED },
	{ 0xDC187F82, "Alien vs Predator (World)", FF_ROM | FF_VERIFIED },
	{ 0xDCCDEF05, "Brett Hull Hockey", FF_ALPINE },
	{ 0xDDFF49F5, "Rayman (Prototype)", FF_ALPINE },
	{ 0xDE55DCC7, "Flashback - The Quest for Identity (World) (En,Fr)", FF_ROM | FF_VERIFIED },
	{ 0xE28756DE, "Atari Karts (World)", FF_ROM | FF_VERIFIED },
	{ 0xE60277BB, "[BIOS] Atari Jaguar Stubulator '93 (World)", FF_BIOS },
	{ 0xE91BD644, "Wolfenstein 3D (World)", FF_ROM | FF_VERIFIED },
	{ 0xEA9B3FA7, "Phase Zero", FF_ROM | FF_REQ_DSP },
	{ 0xEC22F572, "SuperCross 3D (World)", FF_ROM | FF_VERIFIED },
	{ 0xECF854E7, "Cybermorph (World) (Rev 2)", FF_ROM | FF_REQ_DSP },
	{ 0xEEE8D61D, "Club Drive (World)", FF_ROM | FF_VERIFIED },
	{ 0xF4ACBB04, "Tiny Toon Adventures (World)", FF_ROM | FF_VERIFIED },
	{ 0xFA7775AE, "Checkered Flag (World)", FF_ROM | FF_VERIFIED },
	{ 0xFAE31DD0, "Flip Out! (World) (alt)", FF_ROM },
	{ 0xFB731AAA, "[BIOS] Atari Jaguar (World)", FF_BIOS },
// is this really a BIOS???
// No, it's really a cart, complete with RSA header. So need to fix so it can load.
	{ 0xFDF37F47, "Memory Track Cartridge (World)", FF_ROM | FF_VERIFIED },
	{ 0xF7756A03, "Tripper Getem (World)", FF_ROM | FF_VERIFIED },
	{ 0xFFFFFFFF, "***END***", 0 }
};
