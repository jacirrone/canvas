﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using GzipWriter = SequencingFiles.GzipWriter;
using GzipReader = SequencingFiles.GzipReader;

namespace CanvasSNV
{
    class Program
    {
        // Arguments: chromosome, NormalVCFPath, TumorBamPath, OutputPath
        static int Main(string[] arguments)
        {
            CanvasCommon.Utilities.LogCommandLine(arguments);
            if (arguments.Length < 4)
            {
                Console.WriteLine("Usage: Chromosome NormalVCFPath TumorBAMPath OutputPath");
                return 1;
            }
            
            string chromosome = arguments[0];
            string vcfPath = arguments[1];
            string bamPath = arguments[2];
            string outputPath = arguments[3];

            // Handle some special cases, if the "chromosome" is a special string:
            switch (chromosome.ToLowerInvariant())
            {
                case "histogram":
                    return BuildEmpiricalHistograms(vcfPath, bamPath, outputPath);
                case "regionhistogram":
                    return BuildRegionHistograms(vcfPath, bamPath, outputPath);
                default:
                    // Ordinary chromosome name.
                    break;
            }

            // Standard logic: Process one chromosome, write output to the specified file path:
            SNVReviewer processor = new SNVReviewer();
            processor.Chromosome = chromosome;
            return processor.Main(vcfPath, bamPath, outputPath);
        }

        static void OnLog(string message)
        {
            Console.WriteLine(message);
        }
        static void OnError(string message)
        {
            Console.Error.WriteLine(message);
        }

        static int BuildEmpiricalHistograms(string oracleVCFPath, string empiricalVariantFrequencyFolder, string outputPath)
        {
            HistogramVF builder = new HistogramVF();
            return builder.BuildHistogramByCN(oracleVCFPath, empiricalVariantFrequencyFolder, outputPath);
        }

        static int BuildRegionHistograms(string oracleVCFPath, string empiricalVariantFrequencyFolder, string outputPath)
        {
            HistogramVF builder = new HistogramVF();
            return builder.SummarizeStatsByRegion(oracleVCFPath, empiricalVariantFrequencyFolder, outputPath);
        }

    }
}