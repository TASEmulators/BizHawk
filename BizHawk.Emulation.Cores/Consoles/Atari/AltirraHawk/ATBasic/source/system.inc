; Altirra BASIC - System includes
; Copyright (C) 2014 Avery Lee, All Rights Reserved.
;
; Copying and distribution of this file, with or without modification,
; are permitted in any medium without royalty provided the copyright
; notice and this notice are preserved.  This file is offered as-is,
; without any warranty.

;==========================================================================
; HARDWARE INCLUDES
;
;GTIA
m0pf	equ	$d000
m1pf	equ	$d001
m2pf	equ	$d002
m3pf	equ	$d003
p0pf	equ	$d004
p1pf	equ	$d005
p2pf	equ	$d006
p3pf	equ	$d007
m0pl	equ	$d008
m1pl	equ	$d009
m2pl	equ	$d00a
m3pl	equ	$d00b
p0pl	equ	$d00c
p1pl	equ	$d00d
p2pl	equ	$d00e
p3pl	equ	$d00f
hposp0	equ	$d000
hposp1	equ	$d001
hposp2	equ	$d002
hposp3	equ	$d003
hposm0	equ	$d004
hposm1	equ	$d005
hposm2	equ	$d006
hposm3	equ	$d007
sizep0	equ	$d008
sizep1	equ	$d009
sizep2	equ	$d00a
sizep3	equ	$d00b
sizem	equ	$d00c
grafp0	equ	$d00d
grafp1	equ	$d00e
grafp2	equ	$d00f
grafp3	equ	$d010
trig0	equ	$d010
grafm	equ	$d011
colpm0	equ	$d012
colpm1	equ	$d013
colpm2	equ	$d014
pal		equ	$d014
colpm3	equ	$d015
colpf0	equ	$d016
colpf1	equ	$d017
colpf2	equ	$d018
colpf3	equ	$d019
colbk	equ	$d01a
prior	equ	$d01b
vdelay	equ	$d01c
gractl	equ	$d01d
hitclr	equ	$d01e
consol	equ	$d01f

;POKEY
audf1	equ	$d200
audc1	equ	$d201
audf2	equ	$d202
audc2	equ	$d203
audf3	equ	$d204
audc3	equ	$d205
audf4	equ	$d206
audc4	equ	$d207
audctl	equ	$d208
stimer	equ	$d209
random	equ	$d20a
skres	equ	$d20a
serout	equ	$d20d
irqen	equ	$d20e
irqst	equ	$d20e
skctl	equ	$d20f

;PIA
porta	equ	$d300
portb	equ	$d301
pactl	equ	$d302
pbctl	equ	$d303

;ANTIC
dmactl	equ	$d400
chactl	equ	$d401
dlistl	equ	$d402
dlisth	equ	$d403
vscrol	equ	$d405
pmbase	equ	$d407
chbase	equ	$d409
wsync	equ	$d40a
vcount	equ	$d40b
nmien	equ	$d40e
nmires	equ	$d40f
nmist	equ	$d40f

;==========================================================================
; OS INCLUDES (800)
;
casini	= $0002			;cassette initialization vector
ramlo	= $0004			;
tramsz	= $0006			;temporary ram size / cart A flag
tstdat	= $0007			;RAM test data register / cart B flag
warmst	= $0008			;warmstart flag
boot?	= $0009			;boot flag; 0 if none, 1 for disk, 2 for cassette
dosvec	= $000a
dosini	= $000c
appmhi	= $000e
pokmsk	= $0010
brkkey	= $0011			;set on [BREAK]
rtclok	= $0012
bufadr	= $0015			;indirect buffer address pointer (temp for disk buffer)
		; $0016
iccomt	= $0017			;CIO: command byte
ziocb	= $0020			;zero-page IOCB
ichidz	= $0020			;Zero page IOCB: device index ($FF = not open)
icdnoz	= $0021			;Zero page IOCB: device number
iccomz	= $0022			;Zero page IOCB: command byte
icstaz	= $0023			;Zero page IOCB: status byte
icbalz	= $0024			;Zero page IOCB: address of device/filename spec lo
icbahz	= $0025			;Zero page IOCB: address of device/filename spec hi
icptlz	= $0026			;Zero page IOCB: put byte address lo (-1)
icpthz	= $0027			;Zero page IOCB: put byte address hi (-1)
icbllz	= $0028			;Zero page IOCB: buffer length/byte count lo (-1)
icblhz	= $0029			;Zero page IOCB: buffer length/byte count hi (-1)
icax1z	= $002a			;Zero page IOCB: device-specific information 1
icax2z	= $002b			;Zero page IOCB: device-specific information 2
icidno	= $002e			;CIO: call X register save/restore
ciochr	= $002f			;CIO: call A register save/restore
status  = $0030			;SIO: temporay status byte
chksum	= $0031			;SIO: temporary checksum byte (used by IRQ routines)
bufrlo	= $0032			;SIO: buffer pointer lo (incremented by IRQ routines)
bufrhi	= $0033			;SIO: buffer pointer hi (incremented by IRQ routines)
bfenlo	= $0034			;SIO: buffer end lo
bfenhi	= $0035			;SIO: buffer end hi
bufrfl	= $0038			;SIO: buffer full flag
recvdn	= $0039			;SIO: receive completed flag
xmtdon	= $003a			;SIO: transmit completed flag
chksnt	= $003b			;SIO: checksum sent flag
nocksm	= $003c			;SIO: no-checksum flag
bptr	= $003d			;Cassette: Buffer pointer
ftype	= $003e			;Cassette: Interrecord gap (IRG) type; bit7=1 means continuous mode
feof	= $003f			;Cassette: EOF flag
critic	= $0042			;critical flag for vbi
ckey	= $004a			;cassette boot key
atract	= $004d			;screen attract counter
drkmsk	= $004e			;screen attract mask
colrsh	= $004f			;screen attract color shift
lmargn	= $0052			;left margin column
rmargn	= $0053			;right margin column
rowcrs	= $0054			;cursor row
colcrs	= $0055			;cursor column
		; $0056
dindex	= $0057			;display mode index (0-15)
savmsc	= $0058			;lowest address of screen region
oldchr	= $005d			;cursor character save/restore
oldadr	= $005e			;cursor memory address
palnts	= $0062			;[XL/XE] PAL/NTSC flag; 0=NTSC
logcol	= $0063			;logical line column
adress	= $0064			;temporary storage (display code)
		; $0065
ramtop	= $006a			;ram size
bufcnt	= $006b
holdch	= $007c			;temporary key hold area prior to shift/control lock logic

fr0		= $00d4			;floating point register 0
fr1		= $00e0			;floating point register 1
cix		= $00f2
inbuff	= $00f3

vdslst	= $0200			;display list interrupt vector
vprced	= $0202			;serial bus proceed interrupt vector
vinter	= $0204			;serial bus interrupt vector
vbreak	= $0206			;BRK instruction vector
vkeybd	= $0208			;keyboard interrupt vector
vserin	= $020a			;serial input ready interrupt vector
vseror	= $020c			;serial output ready interrupt vector
vseroc	= $020e			;serial output completed interrupt vector
vtimr1	= $0210			;pokey timer 1 interrupt vector
vtimr2	= $0212			;pokey timer 2 interrupt vector
vtimr4	= $0214			;pokey timer 4 interrupt vector
vimirq	= $0216			;immediate IRQ vector
cdtmv1	= $0218			;countdown timer 1
cdtmv2	= $021a			;countdown timer 2
cdtmv3	= $021c			;countdown timer 3
cdtmv4	= $021e			;countdown timer 4
cdtmv5	= $0220			;countdown timer 5
vvblki	= $0222			;vertical blank immediate vector
vvblkd	= $0224			;vertical blank deferred vector
cdtma1	= $0226			;timer 1 vector
cdtma2	= $0228			;timer 2 vector
cdtmf3	= $022a			;timer 3 flag
srtimr	= $022b			;autorepeat timer
cdtmf4	= $022c			;timer 4 flag
intemp	= $022d			;temp value used by SETVBV
cdtmf5	= $022e			;timer 5 flag
sdmctl	= $022f			;shadow for DMACTL ($D400)
sdlstl	= $0230			;shadow for DLISTL ($D402)
sdlsth	= $0231			;shadow for DLISTH ($D403)
sskctl	= $0232			;shadow for SKCTL ($D20F)
brkky	= $0236			;break key interrupt vector (OS B+ only)
cdevic	= $023a			;SIO: command frame device ID
ccomnd	= $023b			;SIO: command frame command ID
caux1	= $023c			;SIO: command aux byte 1
caux2	= $023d			;SIO: command aux byte 2
temp	= $023e			;SIO: temp space
errflg	= $023f			;SIO: error flag
dflags	= $0240			;disk boot flags
dbsect	= $0241			;disk boot sector count
bootad	= $0242			;disk boot address
coldst	= $0244			;cold start flag
gprior	= $026f			;shadow for PRIOR ($D01B)
paddl0	= $0270			;shadow for POT0 ($D200)
paddl1	= $0271			;shadow for POT1 ($D201)
paddl2	= $0272			;shadow for POT2 ($D202)
paddl3	= $0273			;shadow for POT3 ($D203)
paddl4	= $0274			;shadow for POT4 ($D204)
paddl5	= $0275			;shadow for POT5 ($D205)
paddl6	= $0276			;shadow for POT6 ($D206)
paddl7	= $0277			;shadow for POT7 ($D207)
stick0	= $0278			;shadow for PORTA lo ($D300)
stick1	= $0279			;shadow for PORTA hi ($D300)
stick2	= $027a			;shadow for PORTB lo ($D302)
stick3	= $027b			;shadow for PORTB hih ($D302)
ptrig0	= $027c			;paddle 0 trigger
ptrig1	= $027d			;paddle 1 trigger
ptrig2	= $027e			;paddle 2 trigger
ptrig3	= $027f			;paddle 3 trigger
ptrig4	= $0280			;paddle 4 trigger
ptrig5	= $0281			;paddle 5 trigger
ptrig6	= $0282			;paddle 6 trigger
ptrig7	= $0283			;paddle 7 trigger
strig0	= $0284			;shadow for TRIG0 ($D001)
strig1	= $0285			;shadow for TRIG1 ($D002)
strig2	= $0286			;shadow for TRIG2 ($D003)
strig3	= $0287			;shadow for TRIG3 ($D004)
wmode	= $0289			;Cassette: Write mode (00 = read, 80 = write)
blim	= $028a			;Cassette: Buffer limit
txtmsc	= $0294			;text window pointer
		; $0295
logmap	= $02b2			;line logical map
shflok	= $02be			;shift/control lock flags
botscr	= $02bf			;number of text rows in text window
pcolr0	= $02c0			;shadow for COLPM0 ($D012)
pcolr1	= $02c1			;shadow for COLPM1 ($D013)
pcolr2	= $02c2			;shadow for COLPM2 ($D014)
pcolr3	= $02c3			;shadow for COLPM3 ($D015)
color0	= $02c4			;shadow for COLPF0 ($D016)
color1	= $02c5			;shadow for COLPF1 ($D017)
color2	= $02c6			;shadow for COLPF2 ($D018)
color3	= $02c7			;shadow for COLPF3 ($D019)
color4	= $02c8			;shadow for COLBK ($D01A)
dsctln	= $02d5			;disk sector length (1200XL and newer only)
		; $02d6
noclik	= $02db			;key click disable (XL/XE only)
runad	= $02e0			;DOS run address
		; $02e1
memtop	= $02e5			;highest location for programs and data
memlo	= $02e7			;base of application memory
dvstat	= $02ea			;DISK: Status request buffer area (4 bytes)
		; $02eb
		; $02ec
		; $02ed
crsinh	= $02f0			;Display/Editor: cursor inhibit flag
keydel	= $02f1			;keyboard debounce delay (set to 3 vblanks per key)
ch1		= $02f2			;last keyboard code read
chact	= $02f3			;shadow for CHACTL ($D401)
chbas	= $02f4			;shadow for CHBASE ($D409)
atachr	= $02fb
ch		= $02fc			;keyboard FIFO byte
dspflg	= $02fe			;enable/disable of control codes by screen editor (E:)
ssflag	= $02ff			;display/screen editor suspend flag
ddevic	= $0300			;serial bus ID
dunit	= $0301			;device number
dcomnd	= $0302			;command byte
dstats	= $0303			;status byte
dbuflo	= $0304			;buffer address lo
dbufhi	= $0305			;buffer address hi
dtimlo	= $0306			;disk timeout value
dbytlo	= $0308			;byte count lo
dbythi	= $0309			;byte count hi
daux1	= $030a			;sector number lo
daux2	= $030b			;sector number hi
timer1	= $030c			;baud rate determination - first timer value
timer2	= $0310			;baud rate determination - second timer value
timflg	= $0317			;SIO: operation timeout flag (set by countdown timer 1 IRQ)
stackp	= $0318			;SIO: stack pointer save
hatabs	= $031a			;handler table
ichid	= $0340			;IOCB #0 handler ID
icdno	= $0341			;IOCB #0 device number
iccmd	= $0342			;IOCB #0 command byte
icsta	= $0343			;IOCB #0 status
icbal	= $0344			;IOCB #0 buffer address lo
icbah	= $0345			;IOCB #0 buffer address hi
icptl	= $0346			;IOCB #0 PUT address lo
icpth	= $0347			;IOCB #0 PUT address hi
icbll	= $0348			;IOCB #0 buffer length/byte count lo
icblh	= $0349			;IOCB #0 buffer length/byte count hi
icax1	= $034a			;IOCB #0 auxiliary information lo
icax2	= $034b			;IOCB #0 auxiliary information hi
icax3	= $034c			;
icax4	= $034d			;
icax5	= $034e			;
icax6	= $034f			;
basicf	= $03f8			;XL/XE: BASIC flag (0 = enabled)
casbuf	= $03fd			;Cassette buffer (03FD-047F)
plyarg	= $05e0			;FP: Polynomial evaluation temp register
fpscr	= $05e6			;FP: Temp evaluation register (used by LOG/LOG10)

afp		= $d800
fasc	= $d8e6
ifp		= $d9aa
fpi		= $d9d2
zfr0	= $da44
zf1		= $da46
zfl		= $da48
ldbufa	= $da51
fsub	= $da60
fadd	= $da66
fmul	= $dadb
fdiv	= $db28
skpspc	= $dba1
isdigt	= $dbaf
normalize = $dc00
plyevl	= $dd40
fld0r	= $dd89
fld0p	= $dd8d
fld1r	= $dd98
fld1p	= $dd9c
fst0r	= $dda7
fst0p	= $ddab
fmove	= $ddb6
exp		= $ddc0
exp10	= $ddcc
redrng	= $de95
log		= $decd
log10	= $ded1
half	= $df6c

fpconst_half    = $df6c
fpconst_atncoef	= $dfae
fpconst_pi4		= $dff0

ciov	= $e456
blkbdv	= $e471

CIOCmdOpen			= $03
CIOCmdGetRecord		= $05
CIOCmdGetChars		= $07
CIOCmdPutRecord		= $09
CIOCmdPutChars		= $0B
CIOCmdClose			= $0C
CIOCmdGetStatus		= $0D
CIOCmdSpecial		= $0E	;$0E and up is escape
