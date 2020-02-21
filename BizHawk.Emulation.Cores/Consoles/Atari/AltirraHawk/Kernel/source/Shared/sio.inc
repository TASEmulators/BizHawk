;	Altirra - Atari 800/800XL/5200 emulator
;	Modular Kernel ROM - SIO definitions
;	Copyright (C) 2008-2016 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

.ifndef f_SIO_INC
.def f_SIO_INC

SIOSuccess			= $01
SIOErrorTimeout		= $8A
SIOErrorNAK			= $8B
SIOErrorBadFrame	= $8C
SIOErrorOverrun		= $8E
SIOErrorChecksum	= $8F
SIOErrorDeviceError	= $90

.endif
