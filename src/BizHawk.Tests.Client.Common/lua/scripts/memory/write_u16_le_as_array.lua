write_values = {101, 102}
memory.write_u16_le_as_array(0, write_values)
read_values = memory.read_u16_le_as_array(0, #write_values)

match = true
if #write_values ~= #read_values then
	print(string.format("Read incorrect number of values. Expected %i, got %i", #write_values, #read_values))
	match = false
end
for i = 1, #write_values do
	if read_values[i] ~= write_values[i] then
		match = false
		print(string.format("Incorrect value read back at Lua table index %i. Expected %i, got %i", i, write_values[i], read_values[i]))
	end
end
if match then print("pass") end
