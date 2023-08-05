namespace AC3DataUnpacker
{
    internal class Program
    {
        static int ALIGNMENT = 0x800;
        static int BASE_ADDRESS = 0x10000;

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("This program does not have a UI, drag and drop the archive on the program.");
                Console.WriteLine("Press any key to close.");
                Console.ReadLine();
                return;
            }

            try
            {
                foreach (string arg in args)
                {
                    if (File.Exists(arg))
                    {
                        Unpack(arg);
                    }
                    else if (Directory.Exists(arg))
                    {
                        Repack(arg);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine();
                Console.WriteLine("An error has occurred.");
                Console.WriteLine("Press any key to close.");
                Console.ReadLine();
            }
        }

        static void Unpack(string path)
        {
            // Get out directory
            string? dir = Path.GetDirectoryName(path) ?? throw new Exception("Could not get directory name of path.");
            string name = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path).Substring(1);
            string outDir = Path.Combine(dir, $"{name}-{extension}");
            Directory.CreateDirectory(outDir);

            using (var fs = new FileStream(path, FileMode.Open))
            {
                // Store offsets and lengths as lists so we only iterate as many times as there are valid entries when extracting
                List<int> offsets = new List<int>(8192);
                List<int> lengths = new List<int>(8192);

                // Get offsets and lengths
                for (int i = 0; i < 8192; i++)
                {
                    // Create a single byte array instead of two for each
                    byte[] buffer = new byte[8];
                    fs.Read(buffer, 0, 8);

                    // Convert buffer bytes into start block and block count
                    int start_block = BitConverter.ToInt32(buffer, 0);
                    int block_count = BitConverter.ToInt32(buffer, 4);

                    // Get direct offset and length for easy use later
                    offsets.Add((start_block * ALIGNMENT) + BASE_ADDRESS);
                    lengths.Add(block_count * ALIGNMENT);
                }

                // Start extracting files
                for (int i = 0; i < offsets.Count; i++)
                {
                    // Seek to the offset and read the data into a new byte array
                    fs.Seek(offsets[i], SeekOrigin.Begin);
                    byte[] data = new byte[lengths[i]];
                    fs.Read(data, 0, lengths[i]);

                    // Write the data buffer
                    File.WriteAllBytes(Path.Combine(outDir, $"{i}"), data);
                }
            }
        }

        static void Repack(string path)
        {
            // Get out path
            string? dir = Path.GetDirectoryName(path) ?? throw new Exception("Could not get directory name of path.");
            string name = Path.GetFileName(path);
            string extension = ".BIN";
            int extensionIndex = name.LastIndexOf("-");
            if (extensionIndex != -1)
            {
                extension = $".{name.Substring(extensionIndex + 1)}";
                name = name.Substring(0, name.Length - extension.Length);
            }
            string outPath = Path.Combine(dir, $"{name}{extension}");

            using (var fs = new FileStream(outPath, FileMode.Create)) 
            {
                // Get all file paths
                var paths = Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly).OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f)));

                // Reserve the header to be filled later
                fs.Write(new byte[65536], 0, 65536);
                fs.Position = 0;

                // Store the last position for easily getting the next start block
                long data_pos = BASE_ADDRESS;
                foreach (string fpath in paths)
                {
                    byte[] data = File.ReadAllBytes(fpath); // Read file to repack
                    string filename = Path.GetFileNameWithoutExtension(fpath);
                    int id = int.Parse(filename);
                    if (id > 8192)
                    {
                        throw new Exception("The file ID must not be more than 8192.");
                    }

                    int block_length = data.Length; // Store block length we can correct it later

                    // See if alignment matches, if not add it to block_length
                    int remainder = block_length % ALIGNMENT;
                    if (remainder != 0)
                    {
                        block_length += block_length - remainder;
                    }

                    // Store start block relative to base address
                    int start_block = ((int)data_pos - BASE_ADDRESS) / ALIGNMENT;
                    int block_count = block_length / ALIGNMENT;

                    // Ensure blocks pad out to a number divisible by 16 (we do not add this to block count)
                    int pad_blocks = 0;
                    if (block_count % 16 != 0)
                    {
                        pad_blocks = 16 - block_count % 16;
                    }

                    // Get a byte array containing start_block and block_count in little endian
                    byte[] fieldbuffer = new byte[8];
                    fieldbuffer[7] = (byte)((block_count >> 24) & 0xff);
                    fieldbuffer[6] = (byte)((block_count >> 16) & 0xff);
                    fieldbuffer[5] = (byte)((block_count >> 8) & 0xff);
                    fieldbuffer[4] = (byte)(block_count & 0xff);
                    fieldbuffer[3] = (byte)((start_block >> 24) & 0xff);
                    fieldbuffer[2] = (byte)((start_block >> 16) & 0xff);
                    fieldbuffer[1] = (byte)((start_block >> 8) & 0xff);
                    fieldbuffer[0] = (byte)(start_block & 0xff);

                    // Pad the entry to store it at the same index as its id.
                    long id_jump = id * 8;
                    if (fs.Position != id_jump && id_jump > 0)
                    {
                        fs.Write(new byte[id_jump - fs.Position]);
                    }

                    // Write the field buffer
                    fs.Write(fieldbuffer, 0, 8);

                    // Store the field position so we know where to return to to write the next fields
                    long field_pos = fs.Position;

                    // Go to the data position and write it, pad out for block count, pad out for ensuring a block count divisible by 16
                    fs.Seek(data_pos, SeekOrigin.Begin);
                    fs.Write(data, 0, data.Length);
                    if (block_length != data.Length)
                    {
                        fs.Write(new byte[block_length - data.Length]);
                    }
                    if (pad_blocks != 0)
                    {
                        fs.Write(new byte[pad_blocks * ALIGNMENT]);
                    }
                    data_pos = fs.Position; // Set data position we will return to when next writing for an easy next start block
                    fs.Seek(field_pos, SeekOrigin.Begin); // Seek back to the fields
                }
            }
        }
    }
}