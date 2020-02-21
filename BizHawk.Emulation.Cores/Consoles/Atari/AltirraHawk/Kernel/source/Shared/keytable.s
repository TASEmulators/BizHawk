;	Altirra - Atari 800/800XL/5200 emulator
;	Modular Kernel ROM - Keyboard Scan Code Table
;	Copyright (C) 2008-2016 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

KeyCodeToATASCIITable:
	;Special codes in this table (values important for compat):
	;	$80 - invalid key
	;	$81 - inverse video
	;	$82 - caps lock
	;	$83 - shift caps lock
	;	$84 - control caps lock

	;lowercase
	dta		$6C, $6A, $3B, $80, $80, $6B, $2B, $2A
	dta		$6F, $80, $70, $75, $9B, $69, $2D, $3D
	dta		$76, $80, $63, $80, $80, $62, $78, $7A
	dta		$34, $80, $33, $36, $1B, $35, $32, $31
	dta		$2C, $20, $2E, $6E, $80, $6D, $2F, $81
	dta		$72, $80, $65, $79, $7F, $74, $77, $71
	dta		$39, $80, $30, $37, $7E, $38, $3C, $3E
	dta		$66, $68, $64, $80, $82, $67, $73, $61
	
	;SHIFT
	dta		$4C, $4A, $3A, $80, $80, $4B, $5C, $5E
	dta		$4F, $80, $50, $55, $9B, $49, $5F, $7C
	dta		$56, $80, $43, $80, $80, $42, $58, $5A
	dta		$24, $80, $23, $26, $1B, $25, $22, $21
	dta		$5B, $20, $5D, $4E, $80, $4D, $3F, $80
	dta		$52, $80, $45, $59, $9F, $54, $57, $51
	dta		$28, $80, $29, $27, $9C, $40, $7D, $9D
	dta		$46, $48, $44, $80, $83, $47, $53, $41
	
	;CTRL
	dta		$0C, $0A, $7B, $80, $80, $0B, $1E, $1F
	dta		$0F, $80, $10, $15, $9B, $09, $1C, $1D
	dta		$16, $80, $03, $80, $80, $02, $18, $1A
	dta		$80, $80, $9B, $80, $1B, $80, $FD, $80
	dta		$00, $20, $60, $0E, $80, $0D, $80, $80
	dta		$12, $80, $05, $19, $9E, $14, $17, $11
	dta		$80, $80, $80, $80, $FE, $80, $7D, $FF
	dta		$06, $08, $04, $80, $84, $07, $13, $01
