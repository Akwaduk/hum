using System;
using System.CommandLine;
using System.Threading.Tasks;
using hum.Commands;

namespace hum
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {            var rootCommand = new RootCommand("hum - A CLI tool for provisioning and managing web applications")
            {
                new CreateCommand(),
                new InitCommand(),
                new ConfigCommand(),
                new TemplateCommand(),
                new DoctorCommand(),
                new InventoryCommand(),
                new AnsibleConfigCommand()
            };

            return await rootCommand.InvokeAsync(args);
        }
    }
}
