-- Lua functions available in EmuHawk 2.11
-- https://tasvideos.org/Bizhawk

error("This is a definition file for Lua Language Server and not a usable script")

---@meta _

---A library for performing standard bitwise operations.
---@class bit
bit = {}

---Arithmetic shift right of 'val' by 'amt' bits
---
---Example:
---
---	local inbitars = bit.arshift( -1000, 4 );
---@param val integer
---@param amt integer
---@return integer
function bit.arshift(val, amt) end

---Bitwise AND of 'val' against 'amt'
---
---Example:
---
---	local uibitban = bit.band( 1000, 4 );
---@deprecated
---@param val integer
---@param amt integer
---@return integer
function bit.band(val, amt) end

---Bitwise NOT of 'val'
---
---Example:
---
---	local uibitbno = bit.bnot( 1000 );
---@deprecated
---@param val integer
---@return integer
function bit.bnot(val) end

---Bitwise OR of 'val' against 'amt'
---
---Example:
---
---	local uibitbor = bit.bor( 1000, 4 );
---@deprecated
---@param val integer
---@param amt integer
---@return integer
function bit.bor(val, amt) end

---Bitwise XOR of 'val' against 'amt'
---
---Example:
---
---	local uibitbxo = bit.bxor( 1000, 4 );
---@deprecated
---@param val integer
---@param amt integer
---@return integer
function bit.bxor(val, amt) end

---Byte swaps 'short', i.e. bit.byteswap_16(0xFF00) would return 0x00FF
---
---Example:
---
---	local usbitbyt = bit.byteswap_16( 100 );
---@param val integer
---@return integer
function bit.byteswap_16(val) end

---Byte swaps 'dword'
---
---Example:
---
---	local uibitbyt = bit.byteswap_32( 1000 );
---@param val integer
---@return integer
function bit.byteswap_32(val) end

---Byte swaps 'long'
---
---Example:
---
---	local ulbitbyt = bit.byteswap_64( 10000 );
---@param val integer
---@return integer
function bit.byteswap_64(val) end

---Returns result of bit 'pos' being set in 'num'
---
---Example:
---
---	if ( bit.check( -12345, 35 ) ) then
---		console.log( "Returns result of bit 'pos' being set in 'num'" );
---	end;
---@param num integer
---@param pos integer
---@return boolean
function bit.check(num, pos) end

---Clears the bit 'pos' in 'num'
---
---Example:
---
---	local lobitcle = bit.clear( 25, 35 );
---@param num integer
---@param pos integer
---@return integer
function bit.clear(num, pos) end

---Logical shift left of 'val' by 'amt' bits
---
---Example:
---
---	local uibitlsh = bit.lshift( 1000, 4 );
---@deprecated
---@param val integer
---@param amt integer
---@return integer
function bit.lshift(val, amt) end

---Left rotate 'val' by 'amt' bits
---
---Example:
---
---	local uibitrol = bit.rol( 1000, 4 );
---@param val integer
---@param amt integer
---@return integer
function bit.rol(val, amt) end

---Right rotate 'val' by 'amt' bits
---
---Example:
---
---	local uibitror = bit.ror( 1000, 4 );
---@param val integer
---@param amt integer
---@return integer
function bit.ror(val, amt) end

---Logical shift right of 'val' by 'amt' bits
---
---Example:
---
---	local uibitrsh = bit.rshift( 1000, 4 );
---@deprecated
---@param val integer
---@param amt integer
---@return integer
function bit.rshift(val, amt) end

---Sets the bit 'pos' in 'num'
---
---Example:
---
---	local uibitset = bit.set( 25, 35 );
---@param num integer
---@param pos integer
---@return integer
function bit.set(num, pos) end

