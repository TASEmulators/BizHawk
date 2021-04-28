print("##########################################################")
getUrl = comm.httpGetGetUrl()
print("GET URL:  " .. getUrl)

postUrl = comm.httpGetPostUrl()
print("POST URL: " ..  postUrl)

print("\nChecking GET URL change")
error = false
comm.httpSetGetUrl('a')
if (getUrl ~= comm.httpGetGetUrl()) then
	comm.httpSetGetUrl(getUrl)
	error = (getUrl ~= comm.httpGetGetUrl())
else
	error = true
end

if error == false then 
	print("Get URL was successfully changed")
else
	print("Error while changing Get URL")
end


print("\nChecking POST URL change")
error = false
comm.httpSetPostUrl('a')
if (postUrl ~= comm.httpGetPostUrl()) then
	comm.httpSetPostUrl(postUrl)
	error = (postUrl ~= comm.httpGetPostUrl())
else
	error = true
end

if error == false then 
	print("Post URL was successfully changed")
else
	print("Error while changing Post URL")
end

print("\nChecking GET request")
getResponse = comm.httpGet("http://tasvideos.org/BizHawk.html")
if string.find(getResponse, "Bizhawk") then
	print("GET seems to work")
else
	print("Either the Bizhawk site is down or the GET does not work")
end

print("\nChecking memory mapped filed")

size = comm.mmfScreenshot()
if size > 0 then
	print("Memory mapped file was successfully written")
else
	print("Failed to write memory mapped file")
end

mmf_filename = comm.mmfGetFilename()
print("MMF filename: " .. mmf_filename)
comm.mmfSetFilename("deleteme.tmp")
error = false
if (mmf_filename ~= comm.mmfGetFilename()) then
	comm.mmfSetFilename(mmf_filename)
	error = (mmf_filename ~= comm.mmfGetFilename())
else
	error = true
end
if error == false then 
	print("MMF filename successfully changed")
else
	print("MMF filename change failed")
end

print("Writing to MMF")

message = "ABC"
resp_n = tonumber(comm.mmfWrite(mmf_filename, message))
if (resp_n ~= string.len(message)) then
	print("Failed to write to MMF")
else
	resp = comm.mmfRead(mmf_filename, string.len(message))
	if (resp ~= message) then
		print("Failed to read from MMF")
	else
		print("MMF read and read OK")
	end 
end


print("\nTests finished")
print("Please run TestCommunication_All.lua with the supplied Python server for a more comprehensive test")
