; SameBoy SGB bootstrap ROM
; Todo: use friendly names for HW registers instead of magic numbers
SECTION "BootCode", ROM0[$0]
Start:
; Init stack pointer
    ld sp, $fffe

; Clear memory VRAM
    ld hl, $8000

.clearVRAMLoop
    ldi [hl], a
    bit 5, h
    jr z, .clearVRAMLoop

; Init Audio
    ld a, $80
    ldh [$26], a
    ldh [$11], a
    ld a, $f3
    ldh [$12], a
    ldh [$25], a
    ld a, $77
    ldh [$24], a

; Init BG palette to white
    ld a, $0
    ldh [$47], a

; Load logo from ROM.
; A nibble represents a 4-pixels line, 2 bytes represent a 4x4 tile, scaled to 8x8.
; Tiles are ordered left to right, top to bottom.
    ld de, $104 ; Logo start
    ld hl, $8010 ; This is where we load the tiles in VRAM

.loadLogoLoop
    ld a, [de] ; Read 2 rows
    ld b, a
    call DoubleBitsAndWriteRow
    call DoubleBitsAndWriteRow
    inc de
    ld a, e
    xor $34 ; End of logo
    jr nz, .loadLogoLoop

; Load trademark symbol
    ld de, TrademarkSymbol
    ld c,$08
.loadTrademarkSymbolLoop:
    ld a,[de]
    inc de
    ldi [hl],a
    inc hl
    dec c
    jr nz, .loadTrademarkSymbolLoop

; Set up tilemap
    ld a,$19      ; Trademark symbol
    ld [$9910], a ; ... put in the superscript position
    ld hl,$992f   ; Bottom right corner of the logo
    ld c,$c       ; Tiles in a logo row
.tilemapLoop
    dec a
    jr z, .tilemapDone
    ldd [hl], a
    dec c
    jr nz, .tilemapLoop
    ld l,$0f ; Jump to top row
    jr .tilemapLoop
.tilemapDone

    ; Turn on LCD
    ld a, $91
    ldh [$40], a

    ld a, $f1 ; Packet magic, increases by 2 for every packet
    ldh [$80], a
    ld hl, $104 ; Header start
    
    xor a
    ld c, a ; JOYP

.sendCommand
    xor a
    ld [c], a
    ld a, $30
    ld [c], a
    
    ldh a, [$80]
    call SendByte
    push hl
    ld b, $e
    ld d, 0
    
.checksumLoop
    call ReadHeaderByte
    add d
    ld d, a
    dec b
    jr nz, .checksumLoop
    
    ; Send checksum
    call SendByte
    pop hl
    
    ld b, $e
.sendLoop
    call ReadHeaderByte
    call SendByte
    dec b
    jr nz, .sendLoop
    
    ; Done bit
    ld a, $20
    ld [c], a
    ld a, $30
    ld [c], a
    
    ; Update command
    ldh a, [$80]
    add 2
    ldh [$80], a
    
    ld a, $58
    cp l
    jr nz, .sendCommand
    
    ; Write to sound registers for DMG compatibility
    ld c, $13
    ld a, $c1
    ld [c], a
    inc c
    ld a, 7
    ld [c], a
    
    ; Init BG palette
    ld a, $fc
    ldh [$47], a
    
; Set registers to match the original SGB boot
IF DEF(SGB2)
    ld a, $FF
ELSE
    ld a, 1
ENDC
    ld hl, $c060
    
; Boot the game
    jp BootGame

ReadHeaderByte:
    ld a, $4F
    cp l
    jr c, .zero
    ld a, [hli]
    ret
.zero:
    inc hl
    xor a
    ret

SendByte:
    ld e, a
    ld d, 8
.loop
    ld a, $10
    rr e
    jr c, .zeroBit
    add a ; 10 -> 20
.zeroBit
    ld [c], a
    ld a, $30
    ld [c], a
    dec d
    ret z
    jr .loop

DoubleBitsAndWriteRow:
; Double the most significant 4 bits, b is shifted by 4
    ld a, 4
    ld c, 0
.doubleCurrentBit
    sla b
    push af
    rl c
    pop af
    rl c
    dec a
    jr nz, .doubleCurrentBit
    ld a, c
; Write as two rows
    ldi [hl], a
    inc hl
    ldi [hl], a
    inc hl
    ret

WaitFrame:
    push hl
    ld hl, $FF0F
    res 0, [hl]
.wait
    bit 0, [hl]
    jr z, .wait
    pop hl
    ret

TrademarkSymbol:
db $3c,$42,$b9,$a5,$b9,$a5,$42,$3c

SECTION "BootGame", ROM0[$fe]
BootGame:
    ldh [$50], a