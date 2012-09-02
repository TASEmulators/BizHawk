--Castlevania: Rondo of Blood

Size = 3
fill = 0x64FFC8C8
line = 0xFFFFC8C8

function drawObjInfo()

	for i = 0,88 do
		
		objX = mainmemory.read_u8(0xBC8+i)
		objY = mainmemory.read_u8(0xCD0+i)
		objVisibleX = mainmemory.read_u8(0xB70+i)*255
		objVisibleY = mainmemory.read_u8(0xC78+i)*255
		objDamage = mainmemory.read_u8(0x1300+i)   --how much damage it will do if it collides with you
		objHealth = mainmemory.read_u8(0x12A8+i-2) --something is wrong, -2 makes it right in some cases but makes others wrong
		objStatus = mainmemory.read_u8(0x1250+i)   --collision status
		adjustedX = objX - objVisibleX
		adjustedY = objY - objVisibleY
		--clipping
		if(adjustedX > 0 and adjustedY > 0) then
				gui.text(
					adjustedX * Size, 
					adjustedY * Size, 
					objX 
					.. " " .. objY 
--					.. " " .. "#" .. i
--					.. " D" .. objDamage
--					.. " HP" .. objHealth
--					.. " S" .. objStatus
				)	
		end
	end
end

function drawHitBoxes() 

	for i = 0,88  do
		
		objX = mainmemory.read_u8(0xBC8+i)
		objY = mainmemory.read_u8(0xCD0+i)
		objVisibleX = mainmemory.read_u8(0xB70+i)*255
		objVisibleY = mainmemory.read_u8(0xC78+i)*255
		adjustedX = objX - objVisibleX
		adjustedY = objY - objVisibleY
		objOffsetX = mainmemory.read_u8(0x1930+i)
		objDirection = mainmemory.read_u8(0xAC0+i)
		objUnknownY = mainmemory.read_u8(0x19E0+i)
		hitSizeX = mainmemory.read_u8(0x1988+i)
		hitSizeY = mainmemory.read_u8(0x1A38+i)
		
		if(objDirection == 1) then                     --9484
			objOffsetX = bit.bxor(0xFF,objOffsetX)            --9488
			objOffsetX = objOffsetX+1                    --948A
		end

		off00 = 0 												             --942B
		if (bit.band(0x80, objOffsetX) == 128) then         --942F
			of00=bit.band(0xFF,(off00-1))                     --9431
		end
		
		hitBoxCornerX = bit.band(0xFF,(objX + objOffsetX))	 --9434
		hitBoxCornerY = bit.band(0xFF,(objY + objUnknownY)) --944F
		
		objStatus = mainmemory.read_u8(0x1250+i)-- generally: 0 inert, 1 benign, 2 damageable/possibly damaging
		
		if(objStatus == 0) then
			fill = 0x600000FF
			line = 0xC0000000
		elseif(objStatus == 1) then
			fill = 0x3C00FF00
			line = 0x8000FF00
		elseif(objStatus == 2) then
			fill = 0x50FF0000
			line = 0x80FF0000
		end
		
		--clipping
		if(adjustedX > 0 and adjustedY > 0) then			
			gui.drawBox(
				hitBoxCornerX-hitSizeX,
				hitBoxCornerY+hitSizeY,
				hitBoxCornerX+hitSizeX,
				hitBoxCornerY-hitSizeY,
				line,
				fill
			)
		end
	end
end

function dispProperties(num)

	fill = 0x80FFC8C8

	ObjDamageInfo = {
		0x13b0,--5 means is being damaged
		0x1a90,--invulnerability countdown
		0xf90,--if 0, animate.  seems to be used as a counter to temporarily freeze objs when damaged
		0xb18,--flashing if equal to 0x10, white if equal to 0x86
		0x1ae8,
		0x1f18,
		0x11f8,
		0x12a8--health
	}
		
	for i = 1,8  do
		gui.text((10+(i*20)) * size,100 * size, mainmemory.read_u8(ObjDamageInfo[i]+num))
	end
	
	hitBoxInfo = {
		0x1988,
		0x1A38,
		0x1930,
		0xAC0
	}
	
	for i = 1,4  do
		gui.text((10+(i*20)) * size,120 * size, mainmemory.read_u8(hitBoxInfo[i]+num))
	end
	
	ObjPositionInfo = {
		0xB70, --offscreen or not
		0xBC8, --x position
		0xC20,
		0xC78,
		0xCD0, --y position
		0xD28,
	}
	
	for i = 1,6  do
		gui.text((10+(i*20)) * size,140 * size, mainmemory.read_u8(ObjPositionInfo[i]+num))
	end
	
	collisionInfo = {
		0x710, --compensated x
		0x720, --compensated y
		0x730  --compensated "visible"
	}
	
	for i = 1,3  do
		gui.text((10+(i*20)) * size,160 * size, mainmemory.read_u8(collisionInfo[i]+num))
	end
end

while true do

	line = 0xFFFFC8C8
	--display info about a given object
--	dispProperties(0)

	drawHitBoxes()
	drawObjInfo()
	emu.frameadvance()
	--invincibility cheat
--	memory.writebyte(0x13B0, 0)
	
end

--[[ Notes

--Object Damage

A1ED LDY $33B0,X   ;"can damage"
A1F0 LDA $A26D,Y
A1F3 STA $3A90,X   ;invulnerability countdown
A1F6 LDA $A27D,X
A1F9 STA $2F90,X   ;whether to animate the obj or not
A1FC LDA $2B18,X   ;flashing or not
A1FF STA $3AE8,X
A202 LDA #$86      ;make the palette white
A204 STA $2B18,X 
A207 LDA $3F18,X
A20A ORA #$03
A20C STA $31F8,X
A20F LDA $32A8,X
A212 SEC 
A213 SBC $00       ;subtract the amount of damage to do
A215 STA $32A8,X   ;x is the object slot to modify

--Where the player's health is decremented

A1F2 LDA $33B0     ;check if the player is vulnerable
A1F5 BEQ $A234
A1F7 LDA $33B0  
A1FA bit.band #$07
A1FC CMP #$05   
A1FE BNE $A20B  
A200 LDA $36D3
A203 BNE $A20B
A205 STZ $33B0  
A20B LDA $98       ;player health
A20D SEC 
A20E SBC $32A8     ;subtract amount of damage received
A211 STA $98

--Where the damage received (32A8) is set

9546 STA $33B0,X
9549 LDA $33B0
954C BNE $955A
954E LDA $3358,X
9551 STA $33B0
9554 LDA $3300,X   ;the amount of damage an object will deal to you
9557 STA $32A8

--Where the hitbox offset is calculated

9413 LDX #$00
9415 LDA $3250,X   ;load the hitbox type
9418 BEQ $9460     ;we quit if type 0
941A LDA $3930,X   ;x offset
941D STA $01       ;store it in 01
941F LDA $2AC0,X   ;direction facing, left is 1, right is 0
9422 BEQ $942B     ;we are facing...
9424 LDA $01       ;left, load offset
9426 EOR #$FF
9428 INC
9429 STA $01       ;store the altered offset
942B STZ $00     
942D LDA $01       ;get the either altered or unaltered offset
942F BPL $9433     
9431 DEC $00
9433 CLC
9434 ADC $2BC8,X   ;add x position to either altered or unaltered offset
9437 STA $2710,X   ;store that as the hitbox corner
943A LDA $2B70,X   ;is the object visible
943D ADC $00       ;correct it depending on the altered x offset
943F STA $2730,X
9442 STZ $00
9444 LDA $39E0,X   
9447 BPL $944B     
9449 DEC $00
944B LDA $2CD0,X   ;y position
944E CLC
944F ADC $39E0,X   
9452 STA $2720,X   ;store hitbox y corner
9455 LDA $2C78,X   ;indicator that the object is y visible 
9458 ADC $00
945A ORA $2730,X
945D STA $2730,X
9460 INX
9461 CPX #$10      ;we can only do 16 objects
9463 BCC $9415     ;start again if we have objects left
9465 RTS
9466 STZ $33B0,X   ;zero the "can damage" byte
9469 LDA $3250,X   ;objStatus / collision status
946C BNE $9471     ;if the status is 0 we continue
946E JMP $9507     ;otherwise we quit

--X and Y represent our two objects

9471 LDA $3988,X   ;x hitbox size
9474 ORA $3A38,X   ;y hitbox size
9477 BNE $947C     
9479 JMP $9507
947C LDA $3930,X   ;x offset
947F STA $01       
9481 LDA $2AC0,X   ;objDirection
9484 BEQ $948D     ;branch if we are facing right
9486 LDA $01       ;left
9488 EOR #$FF
948A INC
948B STA $01
948D STZ $00       ;right, zero our temp byte
948F LDA $01       ;load the possibly manipulated offset
9491 BPL $9495     
9493 DEC $00
9495 CLC
9496 ADC $2BC8,X   ;add object x position to the possibly manipulated offset
9499 STA $26FF     
949C LDA $2B70,X   ;objVisibleX
949F ADC $00       
94A1 BNE $9507     ;quit if the result is non-zero
94A3 STZ $00
94A5 LDA $39E0,X   ;y related
94A8 BPL $94AC
94AA DEC $00
94AC CLC
94AD ADC $2CD0,X   ;obj y position
94B0 STA $2700
94B3 LDA $2C78,X   ;objVisibleY
94B6 ADC $00
94B8 BNE $9507
94BA LDA $3250,X   ;objStatus / collision status
94BD BPL $94C2
94BF CLY
94C0 BRA $94C4
94C2 LDY #$0F      ;16 objects max

94C4 LDA $3250,Y   ;objStatus / collision status
94C7 BEQ $9504     ;if objStatus is zero, go to the next object
94C9 LDA $2730,Y   ;compensated "visible"
94CC BNE $9504
94CE LDA $3988,X   ;x hitbox size of 1st object
94D1 CLC
94D2 ADC $3988,Y   ;x hitbox size of 2nd object
94D5 STA $2701   
94D8 LDA $3A38,X   ;y hitbox size of 1st object
94DB CLC
94DC ADC $3A38,Y   ;y hitbox size of 2nd object
94DF STA $2702   
94E2 LDA $26FF     
94E5 SEC
94E6 SBC $2710,Y   ;subtract compensated x position for the current object
94E9 BCS $94EE   
94EB EOR #$FF
94ED INC
94EE CMP $2701     
94F1 BCS $9504
94F3 LDA $2700     
94F6 SEC
94F7 SBC $2720,Y   ;subtract compensated y position for the current object
94FA BCS $94FF
94FC EOR #$FF
94FE INC
94FF CMP $2702
9502 BCC $9508
9504 DEY
9505 BPL $94C4
9507 RTS

]]