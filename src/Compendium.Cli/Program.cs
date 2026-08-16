using Compendium.Cli;

if (args.Length > 0 && args[0] == "init")
{
    return await InitCommand.RunAsync();
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
Console.WriteLine("  compendium chat --bundle <path>                 Chat with the Compendium agent");
Console.WriteLine("  compendium ingest --source <path> --bundle <path> [--type <Type>]");
Console.WriteLine("                                                   Convert source documents into OKF concepts");
return 1;
