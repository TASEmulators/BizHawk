;	Altirra - Atari 800/800XL/5200 emulator
;	Modular Kernel ROM - Kernel Database Definitions
;	Copyright (C) 2008-2016 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

.ifndef f_KERNELDB_INC
.def f_KERNELDB_INC

casini	= $0002			;cassette initialization vector
ramlo	= $0004			;
tramsz	= $0006			;temporary ram size / cart A flag
tstdat	= $0007			;RAM test data register / cart B flag
						;(also CMCMD for T:)
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
icax3z	= $002c			;Zero page IOCB: device-specific information 3
icax4z	= $002d			;Zero page IOCB: device-specific information 4
icax5z	= $002e			;Zero page IOCB: device-specific information 5
icidno	= $002e			;CIO: call X register save/restore
icax6z	= $002f			;Zero page IOCB: device-specific information 6
ciochr	= $002f			;CIO: call A register save/restore
status  = $0030			;SIO: temporay status byte
chksum	= $0031			;SIO: temporary checksum byte (used by IRQ routines)
bufrlo	= $0032			;SIO: buffer pointer lo (incremented by IRQ routines)
bufrhi	= $0033			;SIO: buffer pointer hi (incremented by IRQ routines)
bfenlo	= $0034			;SIO: buffer end lo
bfenhi	= $0035			;SIO: buffer end hi
ltemp	= $0036			;Relocating loader: loader temp address (XL/XE)
		; $0037
bufrfl	= $0038			;SIO: buffer full flag
recvdn	= $0039			;SIO: receive completed flag
xmtdon	= $003a			;SIO: transmit completed flag
chksnt	= $003b			;SIO: checksum sent flag
nocksm	= $003c			;SIO: no-checksum flag
bptr	= $003d			;Cassette: Buffer pointer
ftype	= $003e			;Cassette: Interrecord gap (IRG) type; bit7=1 means continuous mode
feof	= $003f			;Cassette: EOF flag
soundr	= $0041			;SIO: noisy audio flag
critic	= $0042			;critical flag for vbi
zchain	= $004a			;Peripheral Handler: zero-page chain address
		; $004b
atract	= $004d			;screen attract counter
drkmsk	= $004e			;screen attract mask
colrsh	= $004f			;screen attract color shift
tmpchr	= $0050			;Screen Editor
hold1	= $0051			;Display/Editor: temporary storage
lmargn	= $0052			;left margin column
rmargn	= $0053			;right margin column
rowcrs	= $0054			;cursor row
colcrs	= $0055			;cursor column
		; $0056
dindex	= $0057			;display mode index
savmsc	= $0058			;lowest address of screen region
oldrow	= $005a			;
oldcol	= $005b			;
oldchr	= $005d			;cursor character save/restore
oldadr	= $005e			;cursor memory address
		; $005f
palnts	= $0062			;PAL/NTSC flag
logcol	= $0063			;Display/Editor: Logical column (0-119)
adress	= $0064			;temporary storage (display code)
		; $0065
toadr	= $0066			;temporary storage (display code)
a1		= toadr
		; $0067
frmadr	= $0068			;temporary storage (display code)
		; $0069
ramtop	= $006a			;ram size
bufcnt	= $006b
bufstr	= $006c			;row/column of start of logical line
bitmsk	= $006e			;Screen Editor
shfamt	= $006f			;Screen Editor
rowac	= $0070			;Screen Editor: line drawing
		; $0071			;
colac	= $0072			;Screen Editor: line drawing
		; $0073			;
endpt	= $0074			;Screen Editor: line drawing
		; $0075			;
deltar	= $0076			;Screen Editor: delta row (line drawing)
deltac	= $0077			;Screen Editor: delta column (line drawing)
		; $0078			;
keydef	= $0079			;XL/XE: Keyboard definition table
		; $007a
swpflg	= $007b			;current display var state ($00 = main, $FF = split screen)
holdch	= $007c			;temporary key hold area prior to shift/control lock logic
countr	= $007e			;Screen Editor: line length
		; $007f
fr0		= $00d4			;FP: Accumulator 0
_fr3	= $00da			;FP: Accumulator 3 (officially FRE)
fr1		= $00e0			;FP: Accumulator 1
fr2		= $00e6			;FP: Accumulator 2
_fpcocnt= $00ec			;FP: temporary storage - polynomial coefficient counter
_fptemp0= $00ed			;FP: temporary storage - transcendental temporary (officially EEXP)
_fptemp1= $00ee			;FP: temporary storage - transcendental temporary (officially NSIGN)
;esign	  $00ef			;BASIC: Used in sqr() routine -- must not be touched by FP elementary functions
;fchrflg  $00f0			;BASIC: Used in sin() routine -- must not be touched by any FP functions
cix		= $00f2			;FP: Character index
inbuff	= $00f3			;FP: ASCII conversion buffer
		; $00f4
		; $00f5			;FP: temporary storage -- also temporarily used by BASIC power routine
		; $00f6			;FP: temporary storage
ztemp4	= $00f7			;FP: temporary storage -- also temporarily used by BASIC power routine
		; $00f8			;FP: temporary storage
		; $00f9			;FP: temporary storage
		; $00fa			;FP: temporary storage
degflg	= $00fb			;FP: degree/radian flag (0=radians, 6=degrees)
flptr	= $00fc			;FP: pointer for floating-point loads and stores
fptr2	= $00fe			;FP: pointer for polynomial evaluation

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
lcount	= $0233			;Relocating loader: (XL/XE)
lpenh	= $0234			;light pen horizontal pos
lpenv	= $0235			;light pen vertical pos
brkky	= $0236			;break key interrupt vector (OS B+ only)
vpirq	= $0238			;PBI device interrupt vector (XL/XE)
		; $0239
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
reclen	= $0245			;Relocating loader: record length (XL/XE)
dsktim	= $0246			;Disk Handler: Disk operation timeout
pdvmsk	= $0247			;PBI device mask (XL/XE)
shpdvs	= $0248			;PBI device selection register shadow ($D1FF) (XL/XE)
pdmsk	= $0249			;PBI device interrupt mask (XL/XE)
reladr	= $024a			;Relocating loader: temp address (XL/XE)
		; $024b

.if _KERNEL_816
vabte	= $024f			;(816) Emulation ABORT vector
		; $0250
vcope	= $0251			;(816) Emulation COP vector
		; $0252
vabtn	= $0253			;(816) Native ABORT vector
		; $0254
		; $0255
vcopn	= $0256			;(816) Native COP vector
		; $0257
		; $0258
vnmin	= $0259			;(816) Native NMI vector
		; $025a
		; $025b
virqn	= $025c			;(816) Native IRQ vector
		; $025d
		; $025e
vbrkn	= $025f			;(816) Native BREAK vector
		; $0260
		; $0261
vcop0	= $0262			;(816) Native COP #0 vector
		; $0263
		; $0264
vcopu	= $0265			;(816) Native COP #1-127 vector
		; $0266
		; $0267
vcopc	= $0268			;(816) Native COP #128-255 vector
		; $0269
		; $026a
.endif

fine	= $026e			;fine scrolling flag (XL/XE)
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
stick2	= $027A			;shadow for PORTB lo ($D302)
stick3	= $027B			;shadow for PORTB hih ($D302)
ptrig0	= $027c			;paddle trigger 0
ptrig1	= $027d			;paddle trigger 1
ptrig2	= $027e			;paddle trigger 2
ptrig3	= $027f			;paddle trigger 3
ptrig4	= $0280			;paddle trigger 4
ptrig5	= $0281			;paddle trigger 5
ptrig6	= $0282			;paddle trigger 6
ptrig7	= $0283			;paddle trigger 7
strig0	= $0284			;shadow for TRIG0 ($D001)
strig1	= $0285			;shadow for TRIG1 ($D002)
strig2	= $0286			;shadow for TRIG2 ($D003)
strig3	= $0287			;shadow for TRIG3 ($D004)
hibyte	= $0288			;Relocating loader (XL/XE)
wmode	= $0289			;Cassette: Write mode (00 = read, 80 = write)
blim	= $028a			;Cassette: Buffer limit
jveck	= $028c			;XL/XE: IRQ jump vector
newadr	= $028e			;Relocating loader (XL/XE)
txtrow	= $0290
txtcol	= $0291
		; $0292
tindex	= $0293			;text mode index
txtmsc	= $0294			;text window pointer
		; $0295
cretry	= $029c			;SIO: command retries (XL/XE)
hold3	= $029d			;Screen Editor
hold2	= $029f			;Screen Editor
dmask	= $02a0			;Display/Editor: Graphics merge mask
tmplbt	= $02a1			
escflg	= $02a2			;Display/Editor: Escape next character
tabmap	= $02a3			;Display/Editor: Bitfield indicating tabs (note reversed bit positions)
logmap	= $02b2			;Display/Editor: Logical line start map (4 bytes)
invflg	= $02b6			;Keyboard Handler: inverse flag ($00/$80)
tmprow	= $02b8			;Screen Editor
tmpcol	= $02b9			;Screen Editor
		; $02ba
scrflg	= $02bb			;Display/Editor: Scroll counter
hold4	= $02bc			;Screen Editor
dretry	= $02bd			;SIO: device retries (XL/XE)
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
runadr	= $02c9			;Relocating loader: run address (XL/XE)
		; $02ca
hiused	= $02cb			;Relocating loader: Next available memory location (out) (XL/XE)
		; $02cc
zhiuse	= $02cd			;Relocating loader: Next available zero-page address (out) (XL/XE)
		; $02ce
gbytea	= $02cf			;Relocating loader: GET BYTE address (XL/XE)
		; $02d0
loadad	= $02d1			;Relocating loader: Load address (XL/XE)
		; $02d2
zloada	= $02d3			;Relocating loader: Zero-page load address (XL/XE)
		; $02d4
dsctln	= $02d5			;Disk sector size (XL/XE)
keyrep	= $02da			;Keyboard repeat rate (XL/XE)
helpfg	= $02dc			;Help flag (XL/XE)
pbpnt	= $02de			;Printer: Buffer index (XL/XE location)
pbufsz	= $02df			;Printer: Record size (XL/XE location)
ramsiz	= $02e4			;ram size in pages
memtop	= $02e5			;highest location for programs and data
memlo	= $02e7			;base of application memory
hndlod	= $02e9			;CIO: Handler load flag (XL/XE)
dvstat	= $02ea			;DISK: Status request buffer area (4 bytes)
		; $02eb
		; $02ec
		; $02ed
cbaudl	= $02ee			;Cassette baud rate as POKEY divisor. Set to nominal ($05CC) by C: init.
cbaudh	= $02ef			;
crsinh	= $02f0			;Display/Editor: cursor inhibit flag
keydel	= $02f1			;keyboard debounce delay (set to 3 vblanks per key)
ch1		= $02f2			;last keyboard code read
chact	= $02f3			;shadow for CHACTL ($D401)
chbas	= $02f4			;shadow for CHBASE ($D409)
rowinc	= $02f8			;Screen Editor: row direction (XL/XE location)
colinc	= $02f9			;Screen Editor: col direction (XL/XE location)
atachr	= $02fb			;Screen Editor
ch		= $02fc			;keyboard FIFO byte
fildat	= $02fd			;Screen Editor: fill color
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
timer1	= $030c			;SIO: baud rate determination - first timer value
addcor	= $030e			;SIO: baud rate determination - correction value
casflg	= $030f			;SIO: cassette I/O flag (0=normal, nonzero=cassette)
timer2	= $0310			;SIO: baud rate determination - second timer value
temp1	= $0312			;SIO: baud rate determination - temp
		; $0313
ptimot	= $0314			;Printer: Timeout (XL/XE location)
temp3	= $0315			;SIO: baud rate determination - temp
timflg	= $0317			;SIO: operation timeout flag (set by countdown timer 1 IRQ)
stackp	= $0318			;SIO: stack pointer save
hatabs	= $031a			;handler table
pupbt1	= $033d			;(XL/XE) Power-up boot flag #1 - $5C
pupbt2	= $033e			;(XL/XE) Power-up boot flag #2 - $93
pupbt3	= $033f			;(XL/XE) Power-up boot flag #3 - $25
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
prnbuf	= $03c0			;printer buffer
ckey	= $03e9			;cassette boot key
basicf	= $03f8			;XL/XE: ROM BASIC flag. 0 = enabled
gintlk	= $03fa			;XL/XE: Cartridge interlock (clone of TRIG3). 
chlink	= $03fb			;XL/XE: Relocated loader chain
casbuf	= $03fd			;Cassette buffer (03FD-047F)
lbuff	= $0580			;
plyarg	= $05e0			;FP: Polynomial evaluation temp register
fpscr	= $05e6			;FP: Temp evaluation register (used by LOG/LOG10)

.endif
