		opt		h-
		
		icl		'hardware.inc'
		icl		'kerneldb.inc'
		icl		'cio.inc'
		icl		'sio.inc'

dskinv	= $e453
ciov = $e456

		org		$0700
		opt		f+

		dta		$00						;$0700 ($00)
		dta		3						;$0701 ($03)
		dta		a($0700)				;$0702 ($0700)
		dta		a(init)					;$0704 ($1540)
		jmp		boot					;$0706 ($0714)
bcb_maxfiles	dta		3				;$0709 ($03)
bcb_drivebits	dta		1				;$070A ($03)
bcb_allocdirc	dta		0				;$070B ($00) (unused)
bcb_secbuf		dta		a(0)			;$070C ($1A7C)
bcb_bootflag	dta		$00				;$070E ($01)
bcb_firstsec	dta		a(0)			;$070F ($0004) first sector of DOS.SYS
bcb_linkoffset	dta		125				;$0711 ($7D)
bcb_loadaddr	dta		a(0)			;$0712 ($07CB)

.proc message1
				;23456789012345678901234567890123456789
		dta		'Cannot boot since there is no DOS',$9B
		dta		'present on this disk. -press any key-'
.endp

.proc message2
		dta		$9c,$1c,$9c
.endp

boot:
		ldx		#0
		mwa		#message1 icbal
		mwa		#[.len message1] icbll
		lda		#CIOCmdPutChars
		jsr		DoIoCmdX
		ldx		#$10
		mwa		#kdev icbal,x
		mva		#$04 icax1,x
		lda		#CIOCmdOpen
		jsr		DoIoCmdX
		mwa		#0 icbll,x
		lda		#CIOCmdGetChars
		jsr		DoIoCmdX
		lda		#CIOCmdClose
		jsr		DoIoCmdX
		ldx		#0
		mva		#<message2 icbal,x
		mva		#[.len message2] icbll,x
		lda		#CIOCmdPutChars
		jsr		DoIoCmdX
init:
		sec
		rts

.proc DoIoCmdX
		sta		iccmd,x
		jmp		ciov
.endp

.proc kdev
		dta		'K',$9B
.endp

		org		$0880
		end
