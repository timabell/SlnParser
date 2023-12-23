using SlnEditor.Models;
using System.Linq;
using System.Text;

namespace SlnEditor.Writers
{
    internal static class SolutionWriter
    {
        public static string Write(Solution solution)
        {
            var sb = new StringBuilder();
            sb.AppendLine("");
            sb.AppendLine($"Microsoft Visual Studio Solution File, Format Version {solution.FileFormatVersion}");
            sb.Append(solution.VisualStudioVersion.Render());
            foreach (var project in solution.Projects)
            {
                sb.AppendLine(
                    $"Project(\"{{{project.TypeGuid.ToString().ToUpper()}}}\") = \"{project.Name}\", \"{project.Path}\", \"{{{project.Id.ToString().ToUpper()}}}\"");
                if (project is SolutionFolder solutionFolder && solutionFolder.Files.Any())
                {
                    sb.AppendLine("\tProjectSection(SolutionItems) = preProject");
                    foreach (var file in solutionFolder.Files)
                    {
                        sb.AppendLine($"\t\t{file} = {file}");
                    }

                    sb.AppendLine("\tEndProjectSection");
                }

                sb.AppendLine("EndProject");
            }

            sb.AppendLine("Global");

            if (solution.ConfigurationPlatforms.Any())
            {
                sb.AppendLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
                foreach (var platform in solution.ConfigurationPlatforms)
                {
                    sb.AppendLine($"\t\t{platform.Name} = {platform.Name}");
                }

                sb.AppendLine("\tEndGlobalSection");
            }

            if (solution.Projects.OfType<Project>().Any(p => p.ConfigurationPlatforms.Any()))
            {
                sb.AppendLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
                foreach (var project in solution.Projects.OfType<Project>())
                {
                    foreach (var platform in project.ConfigurationPlatforms)
                    {
                        sb.AppendLine(
                            $"\t\t{{{project.Id.ToString().ToUpper()}}}.{platform.Name} = {platform.Configuration}|{platform.Platform}");
                    }
                }

                sb.AppendLine("\tEndGlobalSection");
            }

            if (solution.RootProjects.OfType<SolutionFolder>().Any(f => f.Projects.Any()))
            {
                sb.AppendLine("\tGlobalSection(NestedProjects) = preSolution");
                foreach (var project in solution.Projects.OfType<SolutionFolder>())
                {
                    foreach (var subProject in project.Projects)
                    {
                        sb.AppendLine(
                            $"\t\t{{{subProject.Id.ToString().ToUpper()}}} = {{{project.Id.ToString().ToUpper()}}}");
                    }
                }

                sb.AppendLine("\tEndGlobalSection");
            }

            if (solution.SolutionProperties.HideSolutionNode.HasValue)
            {
                sb.AppendLine("\tGlobalSection(SolutionProperties) = preSolution");
                sb.AppendLine($"\t\tHideSolutionNode = {solution.SolutionProperties.HideSolutionNode.ToString().ToUpper()}");
                sb.AppendLine("\tEndGlobalSection");
            }

            if (solution.Guid != null)
            {
                sb.AppendLine("\tGlobalSection(ExtensibilityGlobals) = postSolution");
                sb.AppendLine($"\t\tSolutionGuid = {{{solution.Guid.ToString().ToUpper()}}}");
                sb.AppendLine("\tEndGlobalSection");
            }

            sb.AppendLine("EndGlobal");
            return sb.ToString();
        }
    }
}
