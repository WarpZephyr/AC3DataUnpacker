# AC3DataUnpacker
Unpacks and repacks the AC3DATA.BIN archive in Armored Core 3.  
When repacking all files to repack must be in the root folder you provide.  
Deeper folders will not be accessed.  

File names must be IDs between 0 and 8191, extensions can still be added.  

# Technical Notes
The archive has a file entry table with a set size of 8192 entries.  
Since this is from a PS2 game the entries are in Little Endian.  
The entries are two 32-bit integers, start block and block count.  
The alignment used for the blocks is 0x800.  
Entries base start block starting from 0x10000, so 0 starts at that address.  
The block count is the number of 0x800 blocks an entry covers.  

There seems to be empty entries inbetween normal ones, I am not sure why.  
If a block count is not divisible by 16, extra 0x800 padding blocks are added to ensure data is padded as if it were.  
These extra padding blocks are not factored in the block count of an entry.  

There is no file names as far as I can tell.  
They appear to go by ID, which is determined by index.  
If an index does not exist as an ID it is written null as a dummy.