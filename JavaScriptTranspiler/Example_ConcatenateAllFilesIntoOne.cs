/*

AI generated "concatenation of all JS files in a directory" (I told it some more specifics).
I gotta figure out what exactly I'm doing.
I wanted to get this base logic ready.
I think I'm gonna stream the files to the JavaScript parser?
And then as I parse it I'll generate a separate file that contains text exactly the way I want it?

// In terminal I can run:
// dotnet "C:\Users\hunte\Repos\New folder (7)\LanguageServer2\JavaScriptTranspiler\bin\Release\net10.0\publish\JavaScriptTranspiler.dll"


// Ensure an argument was actually passed to avoid an IndexOutOfRangeException
if (args.Length != 2)
{
    Console.WriteLine($"2 command line arguments are expected (first is the path to the JS source code, second is the path to where the transpiled output goes) (of the 2 required there were '{args.Length}' arguments provided).");
    return;
}


// Capture the very first argument
string sourceFolderPath = args[0];
string outputFolderPath = args[1];
if (!Path.Exists(sourceFolderPath))
{
    Console.WriteLine($"The source path {sourceFolderPath} does not exist.");
    return;
}
if (!Path.Exists(outputFolderPath))
{
    Console.WriteLine($"The output path {outputFolderPath} does not exist.");
    return;
}

// 1. Define your hardcoded array of files (adjust filenames to match your exact project structure)
string[] prioritizedFilenames = [
    "fieldBuffer.js",
    "header_editorGlobal_header.js",
    "widgetGlobal.js",
    "menuGlobal.js",
    "dialogGlobal.js",
    "trackedSyntaxTypes.js",
    "treeViewComponent.js",
    "dialogImplementationsGlobal.js",
    "listComponent.js",
    "listTypes.js",
    "editorMaxHeap.js",
    "editorGlobal.js",
    "javascriptFeatures.js",
    "explorerGlobal.js",
    "applicationRendererRoot.js"
];


var sourceDirectory = new DirectoryInfo(sourceFolderPath);
var outputDirectory = new DirectoryInfo(outputFolderPath);

// Define your combined output file name inside the output directory
string combinedOutputPath = Path.Combine(outputFolderPath, "combined_output.js");

try
{
    // 2. Scan the directory once and map files by their Name (case-insensitive) for fast lookups
    var allJsFiles = sourceDirectory.EnumerateFiles("*.js")
        .ToDictionary(f => f.Name, f => f, StringComparer.OrdinalIgnoreCase);

    // Track which files have been processed to avoid duplicates
    var processedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    // 3. Build the final ordered execution queue
    var executionQueue = new List<FileInfo>();

    // Step A: Enqueue the prioritized files in their exact array order
    foreach (var name in prioritizedFilenames)
    {
        if (allJsFiles.TryGetValue(name, out var file))
        {
            executionQueue.Add(file);
            processedFiles.Add(name);
        }
    }

    // Step B: Enqueue all remaining files, sorted alphabetically by filename
    var remainingSortedFiles = allJsFiles.Keys
        .Where(name => !processedFiles.Contains(name))
        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);

    foreach (var name in remainingSortedFiles)
    {
        executionQueue.Add(allJsFiles[name]);
    }

    // 4. Stream and concatenate the files sequentially
    using var outputStream = new FileStream(combinedOutputPath, FileMode.Create, FileAccess.Write);
    using var writer = new StreamWriter(outputStream);

    foreach (var file in executionQueue)
    {
        Console.WriteLine($"Appending: {file.Name}");

        writer.WriteLine();
        writer.WriteLine($"// File: {file.Name}");
        writer.WriteLine();
        writer.Flush(); // Flush out headers to sync text and byte streams

        using var inputStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read);
        inputStream.CopyTo(outputStream);
    }

    Console.WriteLine($"Successfully concatenated all files into: {combinedOutputPath}");
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred: {ex.Message}");
}
 */