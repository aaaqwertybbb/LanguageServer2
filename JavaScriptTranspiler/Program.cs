// In terminal I can run:
// dotnet "C:\Users\hunte\Repos\New folder (7)\LanguageServer2\JavaScriptTranspiler\bin\Release\net10.0\publish\JavaScriptTranspiler.dll"


// Ensure an argument was actually passed to avoid an IndexOutOfRangeException
if (args.Length == 0)
{
    Console.WriteLine("First command line argument for the folder of JS files was not provided.");
    return;
}
else if (args.Length > 1)
{
    Console.WriteLine("Only the first command line argument is used at the moment.");
    return;
}


// Capture the very first argument
string folderPath = args[0];
if (!Path.Exists(folderPath))
{
    Console.WriteLine($"The path {folderPath} does not exist.");
    return;
}


Console.WriteLine("TODO");






