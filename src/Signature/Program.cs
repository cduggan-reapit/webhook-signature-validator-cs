using Signature.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();
app.Configure(config =>
{
    config.AddCommand<TestSignatureCommand>("test");
});

return await app.RunAsync(args);