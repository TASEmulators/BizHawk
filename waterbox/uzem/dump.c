/*
	Copyright (c) 2009 Eric Anderton
        
	Permission is hereby granted, free of charge, to any person
	obtaining a copy of this software and associated documentation
	files (the "Software"), to deal in the Software without
	restriction, including without limitation the rights to use,
	copy, modify, merge, publish, distribute, sublicense, and/or
	sell copies of the Software, and to permit persons to whom the
	Software is furnished to do so, subject to the following
	conditions:

	The above copyright notice and this permission notice shall be
	included in all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
	EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
	OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
	NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
	HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
	WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
	FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
	OTHER DEALINGS IN THE SOFTWARE.
*/

#include <windows.h>
#include <winioctl.h>
#include <stdio.h>

struct SDPartitionEntry{
    BYTE state;
    BYTE startHead;
    WORD startCylinder;
    BYTE type;
    BYTE endHead;
    WORD endCylinder;
    DWORD sectorOffset;
    DWORD sectorCount;
};    
                     
// Partition 1 example
/*
entry.state = 0x00;
entry.startHead = 0x03;
entry.startCylinder = 0x003D;
entry.type = 0x06;
entry.endHead = 0x0D;
entry.endCylinder = 0xDBED;
entry.sectorOffset = 0x000000F9;
entry.sectorCount = 0x001E5F07;
*/

//Code mostly borrowed from: http://support.microsoft.com/kb/138434

#define SECTORS_PER_WRITE 4096

char drivePath[] = "\\\\.\\X:";

int main(int argc,char** argv)
{
   HANDLE  hCD, hFile;
   DWORD   dwNotUsed;
      
	if (argc<3){
        printf("Disk Image dumper - creates binary images of disks, suitable for SD media.\n");
        printf("(c) 2009 Eric Anderton\n");
        printf("\nUsage: dump DRIVELETTER FILENAME\n");
        exit(1);
    }
    
    if(strlen(argv[1]) > 1){
        printf("Invalid drive letter.\n");
        exit(1);
    }
    
    // set the drive letter in the path specification
    drivePath[4] = argv[1][0];

    hFile = CreateFile (argv[2],GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);

    hCD = CreateFile (drivePath, GENERIC_READ,FILE_SHARE_READ|FILE_SHARE_WRITE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL,NULL);    
                     
    if (hCD != INVALID_HANDLE_VALUE){
        DISK_GEOMETRY         disk;
        PARTITION_INFORMATION partition;
        PARTITION_INFORMATION_MBR mbr;
            
        // Get sector size of compact disc
        if (DeviceIoControl (hCD, IOCTL_DISK_GET_DRIVE_GEOMETRY,
                           NULL, 0, &disk, sizeof(disk),
                           &dwNotUsed, NULL))
        {
            LPBYTE lpSector;
            DWORD dwSize = disk.BytesPerSector;  // 2 sectors
            DWORD dwReadSize = dwSize*SECTORS_PER_WRITE;
            __int64 cylinders = *((__int64*)&disk);
            __int64 sectors = cylinders * disk.TracksPerCylinder * disk.SectorsPerTrack;
            __int64 totalSize = sectors*dwSize;
            __int64 i;

            printf("Cylinders %lld\nTracks Per Cylinder: %d\nSectors Per Track %d\nSector Size: %d\n",cylinders,disk.TracksPerCylinder,disk.SectorsPerTrack,dwSize);
            printf("Total Sectors: %lld\n",sectors);
            printf("Media Size: %lld\n",totalSize);

            // Allocate buffer to hold sectors from compact disc. Note that
            // the buffer will be allocated on a sector boundary because the
            // allocation granularity is larger than the size of a sector on a
            // compact disk.
            lpSector = (LPBYTE)VirtualAlloc (NULL, dwReadSize,MEM_COMMIT|MEM_RESERVE, PAGE_READWRITE);
            
            SDPartitionEntry entry;        
            // query system about partition and fill out the partition structure
            if(DeviceIoControl(hCD, IOCTL_DISK_GET_PARTITION_INFO, NULL, 0, &partition, sizeof(PARTITION_INFORMATION), &dwNotUsed, NULL)){
                entry.state = 0x00;
                entry.startCylinder = 0;//(*(__int64*)(&partition.StartingOffset))/dwSize;
                
                entry.startHead = 0x00; //TODO
                entry.startCylinder = 0x0000; //TODO
                entry.type = partition.PartitionType;
                entry.endHead = 0x00; //TODO
                entry.endCylinder = 0x0000; //TODO
                entry.sectorOffset = partition.HiddenSectors;
                entry.sectorCount = (*(__int64*)(&partition.PartitionLength))/dwSize;
                printf("----------\n");
                printf("state: %0.4X\n",entry.state);
               // printf("startHead: %0.4X\n",entry.startHead);
               // printf("startCylinder: %0.4X\n",entry.startCylinder);
                printf("type: %0.2X\n",entry.type);
              //  printf("endHead: %0.4X\n",entry.endHead);
              //  printf("endCylinder: %0.4X\n",entry.endCylinder);
                printf("sectorCount: %0.8X\n",entry.sectorCount);
                printf("sectorOffset: %0.8X\n",entry.sectorOffset);
            }
            else{
                printf("Error reading parition info.\n");
                exit(1);
            }
            
            // build a replica of the MBR for a single-partition image (common for SD media)
            memset(lpSector,0,dwSize);
            memcpy(lpSector + 0x1BE,&entry,sizeof(SDPartitionEntry));                

            // Executable Marker
            lpSector[0x1FE] = 0x55;
            lpSector[0x1FF] = 0xAA;
            
            WriteFile (hFile, lpSector, dwSize, &dwNotUsed, NULL);
            
            // write out hidden sectors (empty)
            memset(lpSector,0,dwSize);
            for(i = 1; i < entry.sectorOffset; i++){
                WriteFile (hFile, lpSector, dwSize, &dwNotUsed, NULL);
            }
                                              
            // iteratively read all the sectors for the disk image           
            printf("Writing...");
            for(i = 0; i < sectors/SECTORS_PER_WRITE; i++){
                 // Read sectors from the disc and write them to a file.
                 ReadFile (hCD, lpSector, dwReadSize, &dwNotUsed, NULL);
                 WriteFile (hFile, lpSector, dwReadSize, &dwNotUsed, NULL);
            }
            DWORD leftovers = sectors-i;
            if(leftovers > 0){
                 dwReadSize = leftovers*dwSize;
                 ReadFile (hCD, lpSector, dwReadSize, &dwNotUsed, NULL);
                 WriteFile (hFile, lpSector, dwReadSize, &dwNotUsed, NULL);            
            }
            VirtualFree (lpSector, 0, MEM_RELEASE);
        } 
        CloseHandle (hCD);
        CloseHandle (hFile);
    }
}
