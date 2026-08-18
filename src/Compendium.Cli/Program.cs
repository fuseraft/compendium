using System.Reflection;
using Compendium.Cli;

if (args.Length > 0 && args[0] == "--version")
{
    var informational = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "0.0.1";
    // Strip the SDK's auto-appended "+<git-sha>" build metadata — not
    // useful in a human-facing --version, and install.sh just echoes this.
    var version = informational.Split('+')[0];
    Console.WriteLine(version);
    return 0;
}

if (args.Length > 0 && args[0] == "init")
{
    return await InitCommand.RunAsync();
}

if (args.Length > 0 && args[0] == "new")
{
    return await NewCommand.RunAsync(args);
}

if (args.Length > 0 && args[0] == "chat")
{
    return await ChatCommand.RunAsync(args);
}

if (args.Length > 0 && args[0] == "ingest")
{
    return await IngestCommand.RunAsync(args);
}

Console.WriteLine("Usage:");
Console.WriteLine("  compendium init                                Configure your model provider");
Console.WriteLine("  compendium new <path>                           Create a new OKF bundle");
Console.WriteLine("  compendium chat --bundle <path>                 Chat with the Compendium agent");
Console.WriteLine("  compendium ingest --source <path> --bundle <path> [--type <Type>]");
Console.WriteLine("                                                   Convert source documents into OKF concepts");
return 1;
