using BitMiracle.LibTiff.Classic;

/// <summary>
/// Program to convert and compress TIFF images based on provided thresholds.
/// </summary>
internal class Program
{
    /// <summary>
    /// The main entry point for the program.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    private static void Main(string[] args)
    {
        // Display help if no arguments or "--help" is provided
        if (args.Length == 0 || args[0] == "--help")
        {
            PrintHelp();
            return;
        }

        // Check for the correct number of arguments
        if (args.Length < 5)
        {
            Console.WriteLine("Usage: <input filename> <output filename> <threshold1> <threshold2> <threshold3>");
            Environment.Exit(1);
        }

        string inputFile = args[0];
        string outputFile = args[1];
        int threshold2 = 0;
        int threshold3 = 0;
        int YRes = 0;

        // Attempt to parse thresholds from the arguments
        if (!int.TryParse(args[2], out int threshold1) ||
            !int.TryParse(args[3], out threshold2) ||
            !int.TryParse(args[4], out threshold3))
        {
            Console.WriteLine("Thresholds must be integer numbers.");
            Environment.Exit(2);
        }

        // Validate that the thresholds are within the acceptable range
        if (threshold1 < 0 || threshold1 > 255 ||
            threshold2 < 0 || threshold2 > 255 ||
            threshold3 < 0 || threshold3 > 255)
        {
            Console.WriteLine("Thresholds must be between 0 and 255.");
            Environment.Exit(3);
        }

        // Check if the input file exists, if not, create a gradient TIFF for testing purposes
        if (!File.Exists(inputFile))
        {
            if (!int.TryParse(args[5], out int xRes) ||
                !int.TryParse(args[6], out YRes))
            {
                Console.WriteLine("Resolutions must be integer numbers.");
                Environment.Exit(7);
            }

            CreateGradientTiff(inputFile, 1000, 1000, xRes, YRes);
        }

        // Convert and compress the TIFF file based on the provided thresholds
        ConvertTiff(inputFile, outputFile, threshold1, threshold2, threshold3);
    }

    /// <summary>
    /// Prints help information to the console.
    /// </summary>
    static void PrintHelp()
    {
        Console.WriteLine("Program usage:");
        Console.WriteLine("  <input filename> <output filename> <threshold1> <threshold2> <threshold3> [<xRes> optional] [<yRes> optional]");
        Console.WriteLine("  --help: Displays this help information");
        Console.WriteLine();
        Console.WriteLine("Where:");
        Console.WriteLine("  <input filename> is the path to the input TIFF file, if the file does not exist a gradient will be created ->");
        Console.WriteLine("     (for testing purposes) of size 1000 x 1000 at the indicated xRes and yRes resolutions");
        Console.WriteLine("  <output filename> is the path to save the output TIFF file");
        Console.WriteLine("  <threshold1>, <threshold2>, <threshold3> are thresholds for compression (integer numbers between 0 and 255)");
    }

    /// <summary>
    /// Compresses a byte array using the provided thresholds.
    /// </summary>
    /// <param name="original">The original byte array.</param>
    /// <param name="threshold_1">Threshold 1.</param>
    /// <param name="threshold_2">Threshold 2.</param>
    /// <param name="threshold_3">Threshold 3.</param>
    /// <returns>A compressed byte array.</returns>
    private static byte[] CompressArray(byte[] original, int threshold_1, int threshold_2, int threshold_3)
    {
        // Calculate the new length of the compressed array
        int compressedLength = (original.Length + 3) / 4; // Round up to include remaining bytes
        byte[] compressed = new byte[compressedLength];

        for (int i = 0; i < compressed.Length; i++)
        {
            // Calculate the indices for the original bytes
            int index1 = i * 4;
            int index2 = index1 + 1;
            int index3 = index1 + 2;
            int index4 = index1 + 3;

            // Get values using 0 if the index is out of range
            byte value_1 = index1 < original.Length ? MapValue(original[index1], threshold_1, threshold_2, threshold_3) : (byte)0;
            byte value_2 = index2 < original.Length ? MapValue(original[index2], threshold_1, threshold_2, threshold_3) : (byte)0;
            byte value_3 = index3 < original.Length ? MapValue(original[index3], threshold_1, threshold_2, threshold_3) : (byte)0;
            byte value_4 = index4 < original.Length ? MapValue(original[index4], threshold_1, threshold_2, threshold_3) : (byte)0;

            // Combine the values into a single byte
            compressed[i] = CombineValues(value_1, value_2, value_3, value_4);
        }

        return compressed;
    }

    /// <summary>
    /// Combines four byte values into a single byte.
    /// </summary>
    /// <param name="byte_value_1">First byte value.</param>
    /// <param name="byte_value_2">Second byte value.</param>
    /// <param name="byte_value_3">Third byte value.</param>
    /// <param name="byte_value_4">Fourth byte value.</param>
    /// <returns>The combined single byte value.</returns>

    private static byte CombineValues(byte byte_value_1, byte byte_value_2, byte byte_value_3, byte byte_value_4)
    {
        return (byte)(byte_value_1 << 6 | byte_value_2 << 4 | byte_value_3 << 2 | byte_value_4);
    }

    private static byte MapValue(byte value, int s_1, int s_2, int s_3)
    {
        if (value == 0)
            return 0;
        else if (value > s_1 && value <= s_2)
            return 1; // 01 in bits
        else if (value > s_2 && value <= s_3)
            return 2; // 10 in bits
        else if (value > s_3 && value <= 255)
            return 3; // 11 in bits
        else
            throw new ArgumentException("Invalid value");
    }

    /// <summary>
    /// Converts a TIFF image file using specified thresholds and saves it to a new file.
    /// </summary>
    /// <param name="inputFile">The input TIFF file path.</param>
    /// <param name="outputFile">The output TIFF file path.</param>
    /// <param name="threshold_1">First threshold value.</param>
    /// <param name="threshold_2">Second threshold value.</param>
    /// <param name="threshold_3">Third threshold value.</param>
    private static void ConvertTiff(string inputFile, string outputFile, int threshold_1, int threshold_2, int threshold_3)
    {
        // Open the input TIFF file or throw an exception if it cannot be opened.
        using Tiff inputTiff = Tiff.Open(inputFile, "r") ?? throw new FileNotFoundException("Unable to open the original TIFF file.", inputFile);

        // Retrieve image properties such as width and height.
        int width = inputTiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
        int height = inputTiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
        int xRes = inputTiff.GetField(TiffTag.XRESOLUTION)[0].ToInt();
        int yRes = inputTiff.GetField(TiffTag.YRESOLUTION)[0].ToInt();
        var scanLineSize = inputTiff.ScanlineSize();
        int newscanLineSize = (scanLineSize + 3) / 4; // Arrotonda per eccesso per includere tutti i pixel

        var originalBuffer = new byte[height][];

        var compressedBuffer = new byte[height][];
        // Buffer per l'immagine compressa

        // Read the image into a buffer for each line.
        for (int i = 0; i < height; i++)
        {
            originalBuffer[i] = new byte[scanLineSize];
            inputTiff.ReadScanline(originalBuffer[i], i);
        }

        // Open the output TIFF file or throw an exception if it cannot be created.
        using Tiff outputTiff = Tiff.Open(outputFile, "w") ?? throw new FileNotFoundException("Unable to create the output TIFF file.", outputFile);
        outputTiff.SetField(TiffTag.IMAGEWIDTH, width);
        outputTiff.SetField(TiffTag.IMAGELENGTH, height);
        outputTiff.SetField(TiffTag.BITSPERSAMPLE, 2);
        outputTiff.SetField(TiffTag.SAMPLESPERPIXEL, 1);
        outputTiff.SetField(TiffTag.XRESOLUTION, xRes);
        outputTiff.SetField(TiffTag.YRESOLUTION, yRes);
        outputTiff.SetField(TiffTag.ROWSPERSTRIP, height);
        outputTiff.SetField(TiffTag.COMPRESSION, Compression.NONE);
        outputTiff.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
        outputTiff.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB);
        outputTiff.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);

        for (int i = 0; i < height; i++)
        {
            // Compress the original image data with the provided thresholds.
            compressedBuffer[i] = CompressArray(originalBuffer[i], threshold_1, threshold_2, threshold_3);
            outputTiff.WriteScanline(compressedBuffer[i], i);
        }
    }

    /// <summary>
    /// Creates a gradient TIFF image file for testing purposes.
    /// </summary>
    /// <param name="outputFile">The output TIFF file path.</param>
    /// <param name="width">The width of the image.</param>
    /// <param name="height">The height of the image.</param>
    /// <param name="xResolution">The horizontal resolution.</param>
    /// <param name="yResolution">The vertical resolution.</param>
    private static void CreateGradientTiff(string outputFile, int width, int height, int xResolution, int YResolution)
    {
        // Open the output TIFF file or throw an exception if it cannot be created.
        using Tiff tiff = Tiff.Open(outputFile, "w") ?? throw new FileNotFoundException("Unable to create the TIFF file.", outputFile);

        // Create a buffer to hold pixel values for one line of the image.
        byte[] buffer = new byte[width];

        // Set TIFF properties for the gradient image.
        tiff.SetField(TiffTag.IMAGEWIDTH, width);
        tiff.SetField(TiffTag.IMAGELENGTH, height);
        tiff.SetField(TiffTag.BITSPERSAMPLE, 8);
        tiff.SetField(TiffTag.SAMPLESPERPIXEL, 1);
        tiff.SetField(TiffTag.ROWSPERSTRIP, height);
        tiff.SetField(TiffTag.COMPRESSION, Compression.NONE);
        tiff.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
        tiff.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB);
        tiff.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
        tiff.SetField(TiffTag.XRESOLUTION, xResolution);
        tiff.SetField(TiffTag.YRESOLUTION, YResolution);

        // Write the gradient image line by line.
        for (int y = 0; y < height; y++)
        {
            // Fill the buffer with gradient pixel values.
            for (int x = 0; x < width; x++)
            {
                byte gradientValue = (byte)(255 * x / width); // Calculate gradient value.
                buffer[x] = gradientValue;
            }

            // Write the buffer as a scanline in the TIFF image.
            if (!tiff.WriteScanline(buffer, y))
            {
                throw new Exception("Error writing line " + y);
            }
        }
    }
}