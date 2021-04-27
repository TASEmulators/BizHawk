; SameBoy DMG bootstrap ROM
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

; Init BG palette
    ld a, $54
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

    ld a, 30
    ldh [$ff42], a
    
    ; Turn on LCD
    ld a, $91
    ldh [$40], a

    ld d, (-119) & $FF
    ld c, 15
    
.animate
    call WaitFrame
    ld a, d
    sra a
    sra a
    ldh [$ff42], a
    ld a, d
    add c
    ld d, a
    ld a, c
    cp 8
    jr nz, .noPaletteChange
    ld a, $A8
    ldh [$47], a
.noPaletteChange
    dec c
    jr nz, .animate
    ld a, $fc
    ldh [$47], a
    
    ; Play first sound
    ld a, $83
    call PlaySound
    ld b, 5
    call WaitBFrames
    ; Play second sound
    ld a, $c1
    call PlaySound
    


; Wait ~1 second
    ld b, 60
    call WaitBFrames
    
; Set registers to match the original DMG boot
    ld hl, $01B0
    push hl
    pop af
    ld hl, $014D
    ld bc, $0013
    ld de, $00D8
    
; Boot the game
    jp BootGame


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

WaitBFrames:
    call WaitFrame
    dec b
    jr nz, WaitBFrames
    ret

PlaySound:
    ldh [$13], a
    ld a, $87
    ldh [$14], a
    ret


TrademarkSymbol:
db $3c,$42,$b9,$a5,$b9,$a5,$42,$3c

SECTION "BootGame", ROM0[$fe]
BootGame:
    ldh [$50], a