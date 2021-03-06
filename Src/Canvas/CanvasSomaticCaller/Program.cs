﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NDesk.Options;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace CanvasSomaticCaller
{
    class Program
    {
        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: CanvasSomaticCaller.exe [OPTIONS]+");
            Console.WriteLine("Make discrete-valued copy number calls for a somatic sample.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

        static int Main(string[] args)
        {
            CanvasCommon.Utilities.LogCommandLine(args);
            string inFile = null;
            string outFile = null;
            string variantFrequencyFile = null;
            string referenceFolder = null;
            string name = "SAMPLE";
            string truthDataPath = null;
            string somaticVCFPath = null;
            bool needHelp = false;
            string bedPath = null;
            string ploidyBedPath = null;
            string ffpeOutliersPath = null;
            bool isEnrichment = false;
            bool isDbsnpVcf = false;
            double? localSDmetric = null;
            double minimumCallSize;
            int qualityFilterThreshold = 0;
            // Parameters, for parameter-sweep, somatic model training:
            bool isTrainMode = false;
            float? userPurity = null;
            float? userPloidy = null;
            CanvasCommon.CanvasSomaticClusteringMode somaticClusteringMode = CanvasCommon.CanvasSomaticClusteringMode.Density;
            string parameterconfigPath = Path.Combine(Isas.Shared.Utilities.GetAssemblyFolder(typeof(Program)), "SomaticCallerParameters.json");

            OptionSet p = new OptionSet()
            {
                { "i|infile=", "file containing bins, their counts, and assigned segments (obtained from CanvasPartition.exe)",  v => inFile = v },
                { "v|varfile=", "file containing variant frequencies (obtained from CanvasSNV.exe)", v => variantFrequencyFile = v },
                { "o|outfile=", "file name prefix to ouput copy number calls to outfile.vcf", v => outFile = v },
                { "r|reference=", "folder that contains both genome.fa and GenomeSize.xml", v => referenceFolder = v },
                { "n|name=", "sample name for output VCF header (optional)", v => name = v },
                { "t|truth=", "path to vcf/bed with CNV truth data (optional)", v => truthDataPath = v },
                { "h|help", "show this message and exit", v => needHelp = v != null },
                { "e|enrichment", "flag indicating this is enrichment data", v => isEnrichment = v != null },
                { "s|somaticvcf=", "somatic vcf file - optionally used for purity estimation", v => somaticVCFPath = v },
                { "b|bedfile=", "bed file containing regions to exclude from calling", v => bedPath = v},
                { "p|ploidyBedFile=", "bed file specifying reference ploidy (e.g. for sex chromosomes) (optional)", v => ploidyBedPath = v},
                { "f|localSDFile=", "text file with localSD metric (calculate within CanvasClean) (optional)", v => ffpeOutliersPath = v},
                { "d|dbsnpvcf", "flag indicating a dbSNP VCF file is used to generate the variant frequency file", v => isDbsnpVcf = v != null },
                { "M|minimumcall=", "INTERNAL: minimum call size", v => minimumCallSize = int.Parse(v) },
                { "q|qualitythreshold=", $"quality filter threshold (default {qualityFilterThreshold})", v => qualityFilterThreshold = int.Parse(v) },
                { "c|parameterconfig=", $"parameter configuration path (default {parameterconfigPath})", v => parameterconfigPath = v },
                { "u|definedpurity=", "INTERNAL: user pre-defined purity", v => userPurity = float.Parse(v) },
                { "l|definedploidy=", "INTERNAL: user pre-defined ploidy", v => userPloidy = float.Parse(v) },
                { "a|trainmodel=", "INTERNAL: user pre-defined ploidy", v => isTrainMode = v != null }
            };

            List<string> extraArgs = p.Parse(args);

            if (extraArgs.Count > 0)
            {
                Console.WriteLine("Error: Argument '{0}' not understood", extraArgs[0]);
                needHelp = true;
            }

            if (needHelp)
            {
                ShowHelp(p);
                return 0;
            }

            if (inFile == null || outFile == null || referenceFolder == null)
            {
                ShowHelp(p);
                return 0;
            }

            if (!File.Exists(inFile))
            {
                Console.WriteLine("Canvas error: File {0} does not exist! Exiting.", inFile);
                return 1;
            }

            if (!File.Exists(variantFrequencyFile))
            {
                Console.WriteLine("Canvas error: File {0} does not exist! Exiting.", variantFrequencyFile);
                return 1;
            }

            if (!File.Exists(Path.Combine(referenceFolder, "GenomeSize.xml")))
            {
                Console.WriteLine("Canvas error: File {0} does not exist! Exiting.", Path.Combine(referenceFolder, "GenomeSize.xml"));
                return 1;
            }

            if (qualityFilterThreshold < 0)
                throw new ArgumentException($"Quality filter threshold must be greater than or equal to zero. Value was {qualityFilterThreshold}");

            if (!File.Exists(parameterconfigPath))
            {
                Console.WriteLine("Canvas error: File {0} does not exist! Exiting.", parameterconfigPath);
                return 1;
            }

            DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(SomaticCallerParameters));
            string parameterconfigFile = File.ReadAllText(parameterconfigPath);
            MemoryStream ms = new MemoryStream(Encoding.ASCII.GetBytes(parameterconfigFile));
            SomaticCallerParameters somaticCallerParametersJSON = (SomaticCallerParameters)js.ReadObject(ms);

            SomaticCaller caller = new SomaticCaller();
            caller.somaticCallerParameters = somaticCallerParametersJSON;
            caller.TruthDataPath = truthDataPath;
            caller.SomaticVCFPath = somaticVCFPath;
            caller.IsEnrichment = isEnrichment;
            caller.IsDbsnpVcf = isDbsnpVcf;
            caller.userPurity = userPurity;
            caller.userPloidy = userPloidy;
            caller.IsTrainingMode = isTrainMode;
            caller.QualityFilterThreshold = qualityFilterThreshold;

            // Set parameters:

            if (!string.IsNullOrEmpty(ploidyBedPath))
            {
                caller.LoadReferencePloidy(ploidyBedPath);
            }
            if (!string.IsNullOrEmpty(ffpeOutliersPath))
            {
                localSDmetric = CanvasCommon.CanvasIO.ReadLocalSDFromTextFile(ffpeOutliersPath);
            }
            
            caller.LoadBedFile(bedPath);
            return caller.CallVariants(inFile, variantFrequencyFile, outFile, referenceFolder, name, localSDmetric, somaticClusteringMode);
        }
    }
}
