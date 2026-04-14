-- Lua functions available in EmuHawk 2.11
-- https://tasvideos.org/Bizhawk

error("This is a definition file for Lua Language Server and not a usable script")

---@meta _

---A library exposing standard .NET string methods
---@class bizstring
bizstring = {}

---Converts the number to a string representation of the binary value of the given number
---
---Example:
---
---	local stbizbin = bizstring.binary( -12345 );
---@param num integer
---@return string
function bizstring.binary(num) end

---Returns whether or not str contains str2
---
---Example:
---
---	if ( bizstring.contains( "Some string", "Some") ) then
---		console.log( "Returns whether or not str contains str2" );
---	end;
---@param str string
---@param str2 string
---@return boolean
function bizstring.contains(str, str2) end

---Reads a string from an array-like table of bytes. The encoding parameter determines which scheme is used (and it will then be converted to Lua's native encoding if necessary).
---
---Example:
---
---		local str = bizstring.decode(memory.read_bytes_as_array(0x1234, 0x20, "WRAM"), "shift_jis");
---@param bytes table
---@param encoding? string Defaults to `"utf-8"`
---@return string
function bizstring.decode(bytes, encoding) end

---Encodes a string to a byte array (table). The encoding parameter determines which scheme is used (and it will first be converted from Lua's native encoding if necessary).
---
---Example:
---
---		local bytes = bizstring.encode("こんにちは", "shift_jis");
---@param str string
---@param encoding? string Defaults to `"utf-8"`
---@return table
function bizstring.encode(str, encoding) end

---Returns whether str ends wth str2
---
---Example:
---
---	if ( bizstring.endswith( "Some string", "string") ) then
---		console.log( "Returns whether str ends wth str2" );
---	end;
---@param str string
---@param str2 string
---@return boolean
function bizstring.endswith(str, str2) end

---Folds (wraps) a string over multiple lines by naively chopping it at the given width. A newline is NOT appended to the end, the result will end with a newline iff the input ended with one. The width parameter is in UTF-16 code units, and the string is folded at that width regardless of how wide the text appears or whether it already contains newlines or other whitespace. The separator parameter, if passed, will be used instead of a newline.
---
---Example:
---
---		console.writeline(bizstring.fold("ABCDEFGHIKLMNOPQRSTUVWXYZ", 5));
---@param str string
---@param width integer
---@param separator? string
---@return string
function bizstring.fold(str, width, separator) end

---Converts the number to a string representation of the hexadecimal value of the given number
---
---Example:
---
---	local stbizhex = bizstring.hex( -12345 );
---@param num integer
---@return string
function bizstring.hex(num) end

---Converts the number to a string representation of the octal value of the given number
---
---Example:
---
---	local stbizoct = bizstring.octal( -12345 );
---@param num integer
---@return string
function bizstring.octal(num) end

---Appends zero or more of pad_char to the end (right) of str until it's at least length chars long. If pad_char is not a string exactly one char long, its first char will be used, or ' ' if it's empty.
---
---Example:
---
---	local s = bizstring.pad_end("hm", 5, 'm'); -- "hmmmm"
---@param str string
---@param length integer
---@param pad_char string
---@return string
function bizstring.pad_end(str, length, pad_char) end

---Prepends zero or more of pad_char to the start (left) of str until it's at least length chars long. If pad_char is not a string exactly one char long, its first char will be used, or ' ' if it's empty.
---
---Example:
---
---	local s = bizstring.pad_start(tostring(0x1A3792D4), 11, ' '); -- "  439849684"
---@param str string
---@param length integer
---@param pad_char string
---@return string
function bizstring.pad_start(str, length, pad_char) end

---Returns a string that represents str with the given position and count removed
---
---Example:
---
---	local stbizrem = bizstring.remove( "Some string", 4, 5 );
---@param str string
---@param position integer
---@param count integer
---@return string
function bizstring.remove(str, position, count) end

---Returns a string that replaces all occurrences of str2 in str1 with the value of replace
---
---Example:
---
---	local stbizrep = bizstring.replace( "Some string", "Some", "Replaced" );
---@param str string
---@param str2 string
---@param replace string
---@return string
function bizstring.replace(str, str2, replace) end

---Splits str into a Lua-style array using the given separator (consecutive separators in str will NOT create empty entries in the array). If the separator is not a string exactly one char long, ',' will be used.
---
---Example:
---
---	local nlbizspl = bizstring.split( "Some, string", ", " );
---@param str string
---@param separator string
---@return table
function bizstring.split(str, separator) end

---Returns whether str starts with str2
---
---Example:
---
---	if ( bizstring.startswith( "Some string", "Some") ) then
---		console.log( "Returns whether str starts with str2" );
---	end;
---@param str string
---@param str2 string
---@return boolean
function bizstring.startswith(str, str2) end

---Returns a string that represents a substring of str starting at position for the specified length
---
---Example:
---
---	local stbizsub = bizstring.substring( "Some string", 6, 3 );
---@param str string
---@param position integer
---@param length integer
---@return string
function bizstring.substring(str, position, length) end

---Returns an lowercase version of the given string
---
---Example:
---
---	local stbiztol = bizstring.tolower( "Some string" );
---@param str string
---@return string
function bizstring.tolower(str) end

---Returns an uppercase version of the given string
---
---Example:
---
---	local stbiztou = bizstring.toupper( "Some string" );
---@param str string
---@return string
function bizstring.toupper(str) end

---returns a string that trims whitespace on the left and right ends of the string
---
---Example:
---
---	local stbiztri = bizstring.trim( "Some trim string	 " );
---@param str string
---@return string
function bizstring.trim(str) end

