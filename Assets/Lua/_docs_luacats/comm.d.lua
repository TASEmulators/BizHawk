-- Lua functions available in EmuHawk 2.11
-- https://tasvideos.org/Bizhawk

error("This is a definition file for Lua Language Server and not a usable script")

---@meta _

---A library for communicating with other programs
---@class comm
comm = {}

---returns a list of implemented functions
---@return string
function comm.getluafunctionslist() end

---makes a HTTP GET request
---@param url string
---@return string
function comm.httpGet(url) end

---Gets HTTP GET URL
---@return string
function comm.httpGetGetUrl() end

---Gets HTTP POST URL
---@return string
function comm.httpGetPostUrl() end

---makes a HTTP POST request
---@param url string
---@param payload string
---@return string
function comm.httpPost(url, payload) end

---HTTP POST screenshot
---@return string
function comm.httpPostScreenshot() end

---Sets HTTP GET URL
---@param url string
function comm.httpSetGetUrl(url) end

---Sets HTTP POST URL
---@param url string
function comm.httpSetPostUrl(url) end

---Sets HTTP timeout in milliseconds
---@param timeout integer
function comm.httpSetTimeout(timeout) end

---tests HTTP connections
---@return string
function comm.httpTest() end

---tests the HTTP GET connection
---@return string
function comm.httpTestGet() end

---Copy a section of the memory to a memory mapped file
---@param mmf_filename string
---@param addr integer
---@param length integer
---@param domain string
---@return integer
function comm.mmfCopyFromMemory(mmf_filename, addr, length, domain) end

---Copy a memory mapped file to a section of the memory
---@param mmf_filename string
---@param addr integer
---@param length integer
---@param domain string
function comm.mmfCopyToMemory(mmf_filename, addr, length, domain) end

---Gets the filename for the screenshots
---@return string
function comm.mmfGetFilename() end

---Reads a string from a memory mapped file
---@param mmf_filename string
---@param expectedSize integer
---@return string
function comm.mmfRead(mmf_filename, expectedSize) end

---Reads bytes from a memory mapped file
---@param mmf_filename string
---@param expectedSize integer
---@return table # Zero-indexed array.
function comm.mmfReadBytes(mmf_filename, expectedSize) end

---Saves screenshot to memory mapped file
---@return integer
function comm.mmfScreenshot() end

---Sets the filename for the screenshots
---@param filename string
function comm.mmfSetFilename(filename) end

---Writes a string to a memory mapped file
---@param mmf_filename string
---@param outputString string
---@return integer
function comm.mmfWrite(mmf_filename, outputString) end

---Write bytes to a memory mapped file
---@param mmf_filename string
---@param byteArray table
---@return integer
function comm.mmfWriteBytes(mmf_filename, byteArray) end

---returns the IP and port of the Lua socket server
---@return string
function comm.socketServerGetInfo() end

---returns the IP address of the Lua socket server
---@return string
function comm.socketServerGetIp() end

---returns the port of the Lua socket server
---@return integer
function comm.socketServerGetPort() end

---socketServerIsConnected
---@return boolean
function comm.socketServerIsConnected() end

---Receives a message from the Socket server. Since BizHawk 2.6.2, all responses must be of the form $"{msg.Length:D} {msg}" i.e. prefixed with the length in base-10 and a space.
---@return string
function comm.socketServerResponse() end

---sends a screenshot to the Socket server
---@return string
function comm.socketServerScreenShot() end

---sends a screenshot to the Socket server and retrieves the response
---@return string
function comm.socketServerScreenShotResponse() end

---sends a string to the Socket server
---@param SendString string
---@return integer
function comm.socketServerSend(SendString) end

---sends bytes to the Socket server
---@param byteArray table
---@return integer
function comm.socketServerSendBytes(byteArray) end

---sets the IP address of the Lua socket server
---@param ip string
function comm.socketServerSetIp(ip) end

---sets the port of the Lua socket server
---@param port integer
function comm.socketServerSetPort(port) end

---sets the timeout in milliseconds for receiving messages
---@param timeout integer
function comm.socketServerSetTimeout(timeout) end

---returns the status of the last Socket server action
---@return boolean
function comm.socketServerSuccessful() end

