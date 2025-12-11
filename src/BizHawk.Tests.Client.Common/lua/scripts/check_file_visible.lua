local f = io.open("check_file_visible.lua", "r")
if f == nil then
	print("fail")
else
	io.close(f)
	print("pass")
end
