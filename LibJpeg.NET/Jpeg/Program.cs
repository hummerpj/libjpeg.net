﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;

using LibJpeg.Classic;
using cdJpeg;

namespace Jpeg
{
    public partial class Program
    {
        class Options
        {
            public string InputFileName = "";
            public string OutputFileName = "";
        }

        class CompressOptions : Options
        {
            public int Quality = 75;
            public bool ForceBaseline = false;
            public J_DCT_METHOD DCTMethod = JpegConstants.JDCT_DEFAULT;
            public bool Debug = false;
            public bool Grayscale = false;
            public bool Optimize = false;
            public bool Progressive = false;
            public string Qslots = "";
            public string Qtables = "";
            public int RestartInterval = 0;
            public int RestartInRows = 0;
            public string Sample = "";
            public int SmoothingFactor = 0;
        }

        class DecompressOptions : Options
        {
            public IMAGE_FORMATS OutputFormat = IMAGE_FORMATS.FMT_BMP;

            public bool QuantizeColors = false;
            public int DesiredNumberOfColors = 256;

            public J_DCT_METHOD DCTMethod = JpegConstants.JDCT_DEFAULT;
            public J_DITHER_MODE DitherMode = J_DITHER_MODE.JDITHER_FS;

            public bool Debug = false;
            public bool Fast = false;
            public bool Grayscale = false;
            public bool NoSmooth = false;
            public bool OnePass = false;

            public bool Scaled = false;
            public int ScaleNumerator = 1;
            public int ScaleDenominator = 1;
        }

        static bool m_printedVersion = false;
        static string m_programName;    /* program name for error messages */

        public static void Main(string[] args)
        {
            m_programName = Path.GetFileName(Environment.GetCommandLineArgs()[0]);

            Options options = parseArguments(args);
            if (options == null)
                return;

            /* Open the input file. */
            using (FileStream inputFile = openInputFile(options.InputFileName))
            {
                if (inputFile == null)
                    return;

                /* Open the output file. */
                using (FileStream outputFile = createOutputFile(options.OutputFileName))
                {
                    if (outputFile == null)
                        return;

                    CompressOptions compressOptions = options as CompressOptions;
                    if (compressOptions != null)
                        compress(inputFile, compressOptions, outputFile);

                    DecompressOptions decompressOptions = options as DecompressOptions;
                    if (decompressOptions != null)
                        decompress(inputFile, decompressOptions, outputFile);
                }
            }
        }


        static Options parseArguments(string[] argv)
        {
            if (argv.Length <= 1)
            {
                //usage();
                return null;
            }

            bool modeCompress;
            if (argv[0] == "-c")
                modeCompress = true;
            else if (argv[0] == "-d")
                modeCompress = false;
            else
                return null;

            if (modeCompress)
                return parseSwitchesForCompression(argv);
            else
                return parseSwitchesForDecompression(argv);
        }

        static FileStream openInputFile(string fileName)
        {
            try
            {
                return new FileStream(fileName, FileMode.Open);
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("{0}: can't open {1}", m_programName, fileName));
                Console.WriteLine(e.Message);
                return null;
            }
        }

        static FileStream createOutputFile(string fileName)
        {
            try
            {
                return new FileStream(fileName, FileMode.Create);
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("{0}: can't open {1}", m_programName, fileName));
                Console.WriteLine(e.Message);
                return null;
            }
        }

        

        /// <summary>
        /// complain about bad command line
        /// </summary>
        static void usageForCompression()
        {
            Console.WriteLine(string.Format("usage: {0} [switches] inputfile outputfile", m_programName));

            Console.WriteLine("Switches (names may be abbreviated):");
            Console.WriteLine("  -quality N     Compression quality (0..100; 5-95 is useful range)");
            Console.WriteLine("  -grayscale     Create monochrome JPEG file");
            Console.WriteLine("  -optimize      Optimize Huffman table (smaller file, but slow compression)");
            Console.WriteLine("  -progressive   Create progressive JPEG file");
            Console.WriteLine("Switches for advanced users:");
            Console.WriteLine(string.Format("  -dct int       Use integer DCT method {0}", (JpegConstants.JDCT_DEFAULT == J_DCT_METHOD.JDCT_ISLOW ? " (default)" : "")));
            Console.WriteLine(string.Format("  -dct fast      Use fast integer DCT (less accurate) {0}", (JpegConstants.JDCT_DEFAULT == J_DCT_METHOD.JDCT_IFAST ? " (default)" : "")));
            Console.WriteLine(string.Format("  -dct float     Use floating-point DCT method {0}", (JpegConstants.JDCT_DEFAULT == J_DCT_METHOD.JDCT_FLOAT ? " (default)" : "")));
            Console.WriteLine("  -restart N     Set restart interval in rows, or in blocks with B");
            Console.WriteLine("  -smooth N      Smooth dithered input (N=1..100 is strength)");
            Console.WriteLine("  -outfile name  Specify name for output file");
            Console.WriteLine("  -verbose  or  -debug   Emit debug output");
            Console.WriteLine("Switches for wizards:");
            Console.WriteLine("  -baseline      Force baseline quantization tables");
            Console.WriteLine("  -qtables file  Use quantization tables given in file");
            Console.WriteLine("  -qslots N[,...]    Set component quantization tables");
            Console.WriteLine("  -sample HxV[,...]  Set component sampling factors");
        }

        /// <summary>
        /// Complain about bad command line
        /// </summary>
        static void usageForDecompression()
        {
            Console.Write("usage: {0} [switches] inputfile outputfile", m_programName);
            Console.WriteLine("Switches (names may be abbreviated):");
            Console.WriteLine("  -colors N      Reduce image to no more than N colors");
            Console.WriteLine("  -fast          Fast, low-quality processing");
            Console.WriteLine("  -grayscale     Force grayscale output");
            Console.WriteLine("  -scale M/N     Scale output image by fraction M/N, eg, 1/8");
            Console.WriteLine("  -os2           Select BMP output format (OS/2 style)");
            Console.WriteLine("Switches for advanced users:");
            Console.WriteLine("  -dct int       Use integer DCT method {0}", (JpegConstants.JDCT_DEFAULT == J_DCT_METHOD.JDCT_ISLOW ? " (default)" : ""));
            Console.WriteLine("  -dct fast      Use fast integer DCT (less accurate) {0}", (JpegConstants.JDCT_DEFAULT == J_DCT_METHOD.JDCT_IFAST ? " (default)" : ""));
            Console.WriteLine("  -dct float     Use floating-point DCT method {0}", (JpegConstants.JDCT_DEFAULT == J_DCT_METHOD.JDCT_FLOAT ? " (default)" : ""));
            Console.WriteLine("  -dither fs     Use F-S dithering (default)");
            Console.WriteLine("  -dither none   Don't use dithering in quantization");
            Console.WriteLine("  -dither ordered  Use ordered dither (medium speed, quality)");
            Console.WriteLine("  -map FILE      Map to colors used in named image file");
            Console.WriteLine("  -nosmooth      Don't use high-quality upsampling");
            Console.WriteLine("  -onepass       Use 1-pass quantization (fast, low quality)");
            Console.WriteLine("  -outfile name  Specify name for output file");
            Console.WriteLine("  -verbose  or  -debug   Emit debug output");
        }
    }
}