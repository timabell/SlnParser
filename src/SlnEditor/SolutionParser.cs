﻿using SlnEditor.Contracts;
using SlnEditor.Contracts.Exceptions;
using SlnEditor.Contracts.Helper;
using SlnEditor.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SlnEditor
{
    /// <inheritdoc />
    public sealed class SolutionParser : ISolutionParser
    {
        private readonly IList<IEnrichSolution> _solutionEnrichers;

        /// <summary>
        ///     Creates a new instance of <see cref="SolutionParser" />
        /// </summary>
        public SolutionParser()
        {
            _solutionEnrichers = new List<IEnrichSolution>
            {
                new EnrichSolutionWithProjects(),
                new EnrichSolutionWithSolutionConfigurationPlatforms(),
                /*
                 * NOTE: It's important that this happens _after_ the 'EnrichSolutionWithProjects',
                 * because we need the parsed projects before we can map the configurations to them
                 */
                new EnrichSolutionWithProjectConfigurationPlatforms(),
                new EnrichSolutionWithSolutionFolderFiles(),
                new EnrichSolutionWithSolutionGuid(),
            };
        }

        /// <inheritdoc />
        public ISolution Parse(string solutionFileName)
        {
            if (string.IsNullOrWhiteSpace(solutionFileName))
                throw new ArgumentException($"'{nameof(solutionFileName)}' cannot be null or whitespace.",
                    nameof(solutionFileName));

            var solutionFile = new FileInfo(solutionFileName);
            return Parse(solutionFile);
        }

        /// <inheritdoc />
        public void Write(ISolution solution, string outputSolutionFilePath)
        {
            if (solution is null) throw new ArgumentNullException(nameof(solution));
            if (string.IsNullOrWhiteSpace(outputSolutionFilePath))
                throw new ArgumentException($"'{nameof(outputSolutionFilePath)}' cannot be null or whitespace.",
                    nameof(outputSolutionFilePath));

            File.WriteAllText(outputSolutionFilePath, solution.Write());
        }

        /// <inheritdoc />
        public ISolution Parse(FileInfo solutionFile)
        {
            if (solutionFile is null)
                throw new ArgumentNullException(nameof(solutionFile));
            if (!solutionFile.Exists)
                throw new FileNotFoundException("Provided Solution-File does not exist", solutionFile.FullName);
            if (!solutionFile.Extension.Equals(".sln"))
                throw new InvalidDataException("The provided file is not a solution file!");

            try
            {
                var allLines = File.ReadAllLines(solutionFile.FullName);
                var name = Path.GetFileNameWithoutExtension(solutionFile.FullName);
                var solution = ParseInternal(allLines, solutionFile, name);
                return solution;
            }
            catch (Exception exception)
            {
                throw new ParseSolutionFailedException(solutionFile, exception);
            }
        }

        /// <inheritdoc />
        public bool TryParse(string solutionFileName, out ISolution? solution)
        {
            if (string.IsNullOrWhiteSpace(solutionFileName))
                throw new ArgumentException($"'{nameof(solutionFileName)}' cannot be null or whitespace.",
                    nameof(solutionFileName));

            var solutionFile = new FileInfo(solutionFileName);
            return TryParse(solutionFile, out solution);
        }

        /// <inheritdoc />
        public bool TryParse(FileInfo solutionFile, out ISolution? solution)
        {
            if (solutionFile is null)
                throw new ArgumentNullException(nameof(solutionFile));
            solution = null;

            try
            {
                solution = Parse(solutionFile);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public ISolution ParseText(string content)
        {
            var separators = new[] { "\r\n", "\r", "\n" };
            var lines = content.Split( separators, StringSplitOptions.None ); // https://stackoverflow.com/questions/1547476/split-a-string-on-newlines-in-net/1547483#1547483
            return ParseInternal(lines);
        }

        private ISolution ParseInternal(string[] allLines, FileInfo? solutionFile = null, string? solutionName = null)
        {
            var solution = new Solution
            {
                Name = solutionName,
                File = solutionFile,
            };
            var allLinesTrimmed = allLines
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .ToList();

            foreach (var enricher in _solutionEnrichers)
                enricher.Enrich(solution, allLinesTrimmed);

            foreach (var line in allLines)
                ProcessLine(line, solution);

            return solution;
        }

        private static void ProcessLine(string line, Solution solution)
        {
            ProcessSolutionFileFormatVersion(line, solution);
            ProcessVisualStudioVersion(line, solution);
            ProcessMinimumVisualStudioVersion(line, solution);
        }

        private static void ProcessSolutionFileFormatVersion(string line, Solution solution)
        {
            if (!line.StartsWith("Microsoft Visual Studio Solution File, ")) return;

            /*
             * 54 characters, because...
             * "Microsoft Visual Studio Solution File, Format Version " is 54 characters long
            */
            var fileFormatVersion = string.Concat(line.Skip(54));
            solution.FileFormatVersion = fileFormatVersion;
        }

        private static void ProcessVisualStudioVersion(string line, Solution solution)
        {
            if (!line.StartsWith("VisualStudioVersion = ")) return;

            // because "VisualStudioVersion = " is 22 characters long
            var visualStudioVersion = string.Concat(line.Skip(22));

            solution.VisualStudioVersion.Version = visualStudioVersion;
        }

        private static void ProcessMinimumVisualStudioVersion(string line, ISolution solution)
        {
            if (!line.StartsWith("MinimumVisualStudioVersion = ")) return;

            // because "MinimumVisualStudioVersion = " is 29 characters long
            var minimumVisualStudioVersion = string.Concat(line.Skip(29));

            solution.VisualStudioVersion.MinimumVersion = minimumVisualStudioVersion;
        }
    }
}
