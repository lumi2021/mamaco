using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Mamaco;
using Microsoft.CodeAnalysis;
using Tq.Realizer.Builder;

internal class Program
{
    public static void Main(string[] args)
    {
        
        if (args.Length == 0) PrintHelpAndExit(1);

        switch (args[0])
        {
            case "build":
                Builder.Build(args[1..]);
                break;
            
            case "help" or "-h" or "-help" or "--help":
                PrintHelpAndExit();
                break;
        }
    }

    [DoesNotReturn]
    private static void PrintHelpAndExit(int error = 0)
    {
        Console.WriteLine(@"
Usage: mamaco <command> [options]

Commands:
    build
    help, -h, -help, --help
");
        
        Environment.Exit(error);
    }
}

