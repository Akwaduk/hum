using System;
using System.CommandLine;
using System.Threading.Tasks;
using hum.Commands;

namespace hum
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("Hum - A CLI tool for provisioning and managing web applications")
            {
                new InitCommand(),
                new ConfigCommand(),
                new TemplateCommand()
            };

            return await rootCommand.InvokeAsync(args);
        }
    }
}
