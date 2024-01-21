# AC3DataUnpacker
Unpacks and repacks:  
AC2DATA.BIN in Armored Core 2.  
AC25DATA.BIN in Armored Core 2 Another Age.  
AC3DATA.BIN in Armored Core 3.  

When repacking, all files to repack must be in the root folder you provide.  
Deeper folders will not be accessed.  

File names must be IDs between 0 and 4095 for AC2DATA.BIN.  
File names must be IDs between 0 and 8191 for AC25DATA.BIN or AC3DATA.BIN.  
If the name of the folder provided contains "AC2DATA" it will be treated as AC2DATA which only supports 4096 files instead of 8192.  
Extensions can be added to files as extensions will be removed when repacking.  

AC25DATA repacking had a different amount of padding. I'm not sure why. It may not work when repacked.

# Technical Notes
The AC DATA archive format is simplistic.  
It is in Little Endian since it is used in PS2 games.  
The entries are two 32-bit integers each, with a start block and block count.  
The start block counts up beginning from the base address after all the entries.  
The alignment used for the blocks is 0x800. Each block is 0x800 in size.  

The index an entry is at is it's file ID.  
There will be empty entries with 0 set for the start block and block count.  

In AC2DATA 4096 entries are always present with a base address of 0x8000.  
In AC25DATA and AC3DATA 8192 entries are always present with a base address of 0x10000.  

For some reason the amount of padding between the files pads to a 0x8000 block usually.  
This may not be correct though, as for some reason a block padded to 0x458000 in the original file instead of 0x450000 in AC25DATA.