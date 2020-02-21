;	Altirra - Atari 800/800XL/5200 emulator
;	Additions - 850 R: Handler Boot Utility
;	Copyright (C) 2008-2017 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

		icl		'hardware.inc'
		icl		'kerneldb.inc'
		icl		'cio.inc'

ciov	equ		$e456
siov	equ		$e459

		org		$80
recv_done	.ds		1
xmit_done	.ds		1
command_buf	.ds		5
resp_buf	.ds		2
cmd_retries	.ds		1
dev_retries	.ds		1
data_len	.ds		2
data_buf	.ds		2
vec_save	.ds		6
chk_ptr		.ds		2
checksum	.ds		1

		org		$3c00

;==========================================================================
.proc	main
		lda		#<msg_banner
		ldy		#>msg_banner
		jsr		PutMessage
		
		;check if HATABS has R: already
		ldx		#0
		lda		#'R'
hatabs_check:
		cmp		hatabs,x
		bne		not_r
		lda		#<msg_already_loaded
		ldy		#>msg_already_loaded
		jmp		PutMessage
not_r:
		inx
		inx
		inx
		cpx		#36
		bne		hatabs_check

		;try poll command
		ldx		#11
		mva:rpl	poll_cmd,x ddevic,x-
		jsr		siov
		tya
		bmi		poll_fail
		
		;poll was answered -- do normal bootstrap
		ldx		#11
		mva:rpl	$0500,x ddevic,x-
		jsr		siov
		tya
		bpl		run_relocator
		
fail_exit:
		lda		#<msg_load_failed
		ldy		#>msg_load_failed
		jmp		PutMessage
		
poll_fail:
		;attempt a blind read of the 850 relocator
		ldx		#4
		mva:rpl	reloc850_cmd,x command_buf,x-
		ldy		#$02
		lda		#$00
		jsr		SerialDoCmd
		bcs		fail_exit
		
		;compute checksum
		lda		SerialInputReadyHandler.read_addr+1
		sta		chk_ptr+1
		lda		#0
		sta		chk_ptr
		
		ldy		SerialInputReadyHandler.read_addr
		sne:dec	chk_ptr+1
		dey
		
		lda		(chk_ptr),y
		sta		checksum
		
		lda		#0
		clc
		jmp		chk_start
		
chk_loop2:
		plp
chk_loop:
		adc		(chk_ptr),y
chk_start:
		dey
		bne		chk_loop
		adc		(chk_ptr),y
		dey
		ldx		chk_ptr+1
		dex
		stx		chk_ptr+1
		php
		cpx		#4
		bne		chk_loop2
		plp
		adc		#0
		cmp		checksum
		bne		fail_exit

run_relocator:
		jsr		$0506
		
		;fake cold start around cold init so we don't reinit DOS
		lda		warmst
		pha
		mva		#0 warmst
		jsr		do_init
		pla
		sta		warmst
		
		lda		#<msg_load_succeeded
		ldy		#>msg_load_succeeded
		jmp		PutMessage

do_init:
		jmp		(dosini)

poll_cmd:
		dta		$51,$01,$3F,$40,$00,$05,$40,$00,$0C,$00,$01,$00
		
reloc850_cmd:
		dta		$51,$21,$00,$00,$72
		
msg_banner:
		dta		'Altirra 850 R: Handler Loader V0.1',$9B
		
msg_already_loaded:
		dta		'R: handler already loaded.',$9B

msg_load_failed:
		dta		'R: handler load failed.',$9B
		
msg_load_succeeded:
		dta		'R: handler load succeeded.',$9B
.endp

;==========================================================================
; Input:
;	Y:A = message
;
.proc PutMessage
		sta		icbal
		sty		icbah
		mva		#CIOCmdPutRecord iccmd
		ldx		#1
		sta		icblh
		dex
		sta		icbll
		jmp		ciov
.endp

;==========================================================================
.proc SerialDoCmd
		sta		data_len
		sty		data_len+1
		
		ldx		#5
		sei
vecset_loop:
		mva		vserin,x vec_save,x
		mva		vec_tab,x vserin,x
		dex
		bpl		vecset_loop
		cli

		lda		#2
		sta		dev_retries

retry_command:
		lda		#14
		sta		cmd_retries

retry_send:
		jsr		SerialInit

		;enter critical section
		inc		critic

		;assert command line
		lda		#$34
		sta		pbctl
		
		;transmit command
		jsr		SerialTransmit
				
		;receive ACK
		ldy		#>resp_buf
		lda		#<resp_buf
		jsr		SerialReceiveSetAddress
		
		ldx		#3
		lda		#1
		ldy		#0
		jsr		SerialReceive
		
		lda		resp_buf
		cmp		#'A'
		beq		cmd_ok
	
		dec		cmd_retries
		bne		retry_send
		
		sec
		jmp		xit
		
cmd_ok:
		ldx		#64
		lda		#1
		ldy		#0
		jsr		SerialReceive
		
		lda		resp_buf+1
		cmp		#'C'
		beq		resp_ok
		
		dec		dev_retries
		bne		retry_command
		sec
		jmp		xit
		
resp_ok:
		lda		#$00
		ldy		#$05
		jsr		SerialReceiveSetAddress
		lda		data_len
		ldy		data_len+1
		ldx		#$40
		jsr		SerialReceive
		
		clc
xit:
		dec		critic
		
		ldx		#5
		sei
		mva:rpl	vec_save,x vserin,x-
		cli
		
		lda		#$a0
		sta		audc4
		
		lda		recv_done
		rts
		
vec_tab:
		dta		a(SerialInputReadyHandler)
		dta		a(SerialOutputReadyHandler)
		dta		a(SerialOutputCompleteHandler)
.endp

;==========================================================================
.proc SerialInit
		ldx		#8
		mva:rpl	pokey_tab,x $d200,x-
		lda		#$87
		and		sskctl
		sta		sskctl
		sta		skctl
		rts
		
pokey_tab:
		dta		$00,$a0,$00,$a0,$28,$a8,$00,$a0,$28
.endp

;==========================================================================
.proc SerialTransmit
		lda		#command_buf+1
		sta		SerialOutputReadyHandler.cmd_ptr
		lda		#0
		sta		xmit_done
		
		sei
		lda		sskctl
		and		#$87
		ora		#$20
		sta		sskctl
		sta		skctl
		
		lda		#$10
		ora		pokmsk
		sta		pokmsk
		sta		irqen
		cli
		
		lda		command_buf
		sta		serout
		
		lda:req	xmit_done
		rts
.endp

;==========================================================================
;	Y:A		Receive address
;
.proc SerialReceiveSetAddress
		sta		SerialInputReadyHandler.read_addr
		sty		SerialInputReadyHandler.read_addr+1
		rts
.endp

;==========================================================================
; Input:
;	Y:A		Receive length
;	X		Timeout (vblanks)
;
.proc SerialReceive
		sta		SerialInputReadyHandler.remaining_lo
		sty		SerialInputReadyHandler.remaining_hi
		txa
		clc
		adc		rtclok+2
		tax
		
		lda		#0
		sta		recv_done
		
		sei
		lda		sskctl
		and		#$87
		ora		#$10
		sta		sskctl
		sta		skctl
		
		lda		#$20
		ora		pokmsk
		sta		pokmsk
		sta		irqen
		cli
		
		;deassert command line
		lda		#$3c
		sta		pbctl

		;wait for timeout or receive done
wait_loop:
		cpx		rtclok+2
		beq		done
		lda		recv_done
		beq		wait_loop
		clc
done:
		sei
		lda		#$df
		and		pokmsk
		sta		pokmsk
		sta		irqen
		cli
		rts
.endp

;==========================================================================
.proc SerialInputReadyHandler
		lda		serin
		sta.w	$0500
read_addr = *-2
		inw		read_addr
		lda		remaining_lo
		sne:dec	remaining_hi
		dec		remaining_lo
		lda		#0
remaining_lo = *-1
		ora		#0
remaining_hi = *-1
		beq		transfer_complete
		pla
		rti
		
transfer_complete:
		lda		#$20
		sta		recv_done
		ora		pokmsk
		sta		pokmsk
		sta		irqen
		pla
		rti
.endp

;==========================================================================
.proc SerialOutputReadyHandler
		lda.b	command_buf
cmd_ptr = *-1
		sta		serout

		inc		cmd_ptr
		lda		cmd_ptr
		cmp		#command_buf+5
		bne		not_done
		
		lda		pokmsk
		and		#$ef
		ora		#$08
		sta		pokmsk
		sta		irqen
not_done:
		pla
		rti
.endp

;==========================================================================
.proc SerialOutputCompleteHandler
		lda		#$f7
		sta		xmit_done
		and		pokmsk
		sta		pokmsk
		sta		irqen
		pla
		rti
.endp

;==========================================================================
		run		main
