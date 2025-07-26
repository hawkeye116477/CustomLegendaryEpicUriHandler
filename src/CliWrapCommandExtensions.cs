using System;
using CliWrap;

namespace CustomLegendaryEpicUriHandler
{
    internal static class CliWrapCommandExtensions
    {
        internal static Command AddCommandToLog(this Command command)
        {
            var allEnvironmentVariables = "";
            if (command.EnvironmentVariables.Count > 0)
            {
                foreach (var env in command.EnvironmentVariables)
                {
                    allEnvironmentVariables += $"{env.Key}={env.Value} ";
                }
            }
            Console.WriteLine($"Executing command: {allEnvironmentVariables}{command.TargetFilePath} {command.Arguments}");
            return command;
        }
    }
}