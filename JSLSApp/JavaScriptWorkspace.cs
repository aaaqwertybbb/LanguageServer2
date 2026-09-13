using JSLSApp.LspTypes;

namespace JSLSApp;

public class JavaScriptWorkspace
{
    public static JavaScriptWorkspace Empty = new JavaScriptWorkspace();

    /// <summary>
    /// To determine which constructor was originally used check '<see cref="_isWorkspace"/>'
    /// If <see cref="JavaScriptWorkspace(List{WorkspaceFolder}?)"/> is used, '<see cref="_isWorkspace"/>' is true, otherwise false.
    /// </summary>
    private readonly bool _isWorkspace;

    /// <summary>
    /// Both the <see cref="JavaScriptWorkspace(string)"/> and <see cref="JavaScriptWorkspace(List{WorkspaceFolder}?)"/>
    /// will populate this list of <see cref="WorkspaceFolder"/>.
    /// 
    /// <br/><br/>
    /// 
    /// To determine which constructor was originally used check '<see cref="_isWorkspace"/>'
    /// If <see cref="JavaScriptWorkspace(List{WorkspaceFolder}?)"/> is used, '<see cref="_isWorkspace"/>' is true, otherwise false.
    /// 
    /// <br/><br/>
    /// 
    /// The result of the constructor '<see cref="JavaScriptWorkspace(string)"/>' is just to make a single <see cref="WorkspaceFolder"/>.
    /// </summary>
    private readonly List<WorkspaceFolder> _workspaceFolders;

    public List<string> SourceFileAbsolutePathList { get; } = new();
    public Dictionary<string, JavaScriptDocument> OpenedSourceFileAbsolutePathToInMemoryContentMap { get; set; } = new();

    private JavaScriptWorkspace()
    {
        _workspaceFolders = new();
    }

    public JavaScriptWorkspace(string rootAbsolutePath)
    {
        _workspaceFolders = new();
        _workspaceFolders.Add(new WorkspaceFolder
        {
            name = rootAbsolutePath,
            uri = rootAbsolutePath
        });

        Recursive_FileDiscovery(rootAbsolutePath);
    }

    public JavaScriptWorkspace(List<WorkspaceFolder> workspaceFolders)
    {
        _isWorkspace = true;
        _workspaceFolders = workspaceFolders;
        foreach (var workspaceFolder in _workspaceFolders)
        {
            Recursive_FileDiscovery(workspaceFolder.uri);
        }
    }

    public void DidOpenTextDocumentNotification(string myPath, string sourceFileAbsolutePath, string text)
    {
        var localPath = new Uri(sourceFileAbsolutePath).LocalPath;
        OpenedSourceFileAbsolutePathToInMemoryContentMap.Add(sourceFileAbsolutePath, new JavaScriptDocument(localPath, text.ToList()));
    }
    
    public void DidCloseTextDocumentNotification(string myPath, string sourceFileAbsolutePath)
    {
        var localPath = new Uri(sourceFileAbsolutePath).LocalPath;
        var wasRemoved = OpenedSourceFileAbsolutePathToInMemoryContentMap.Remove(localPath);
    }

    public void DidChangeTextDocumentNotification(string myPath, string sourceFileAbsolutePath, TextDocumentContentChangeEvent[] contentChanges)
    {
        var found = OpenedSourceFileAbsolutePathToInMemoryContentMap.TryGetValue(sourceFileAbsolutePath, out var doc);
        if (!found)
        {
            File.AppendAllText(myPath, $"\n====ERROR DidChangeTextDocumentNotification did not find file====\n");
            return;
        }

        if (contentChanges.Length == 0)
        {
            File.AppendAllText(myPath, $"\n====DidChangeTextDocumentNotification; {nameof(contentChanges)} length was 0====\n");
            return;
        }

        foreach (var change in contentChanges)
        {
            if (change.range is null)
            {
                File.AppendAllText(myPath, $"\n====DidChangeTextDocumentNotification; TODO: support 'if (change.range is null)'====\n");
                continue;
            }

            if (change.range.start.line != change.range.end.line || change.range.start.character != change.range.end.character)
            {
                if (change.text is not null)
                {
                    // TODO: You're allowed to provide text to insert here as well, so this needs to be supported.
                    File.AppendAllText(myPath, $"\n====change.text is not null====\n");
                    continue;
                }

                var startIndexPosition = FindPositionFromLineAndCharacter(myPath, doc.Chars, change.range.start.line, change.range.start.character);
                var endIndexPosition = FindPositionFromLineAndCharacter(myPath, doc.Chars, change.range.end.line, change.range.end.character);
                doc.Chars.RemoveRange(startIndexPosition, endIndexPosition - startIndexPosition);
            }
            else
            {
                if (change.text is null)
                {
                    File.AppendAllText(myPath, $"\n====DidChangeTextDocumentNotification; change.text is null====\n");
                    continue;
                }

                var indexPosition = FindPositionFromLineAndCharacter(myPath, doc.Chars, change.range.start.line, change.range.start.character);
                if (indexPosition == -1)
                {
                    File.AppendAllText(myPath, $"\n====DidChangeTextDocumentNotification; if (indexPosition == -1)====\n");
                    continue;
                }
                
                doc.Chars.InsertRange(indexPosition, change.text);
            }
        }
    }

    public void Recursive_FileDiscovery(string targetDir)
    {
        foreach (var childFile in Directory.EnumerateFiles(targetDir))
        {
            if (Path.GetExtension(childFile) == ".js" || Path.GetExtension(childFile) == ".cjs")
            {
                SourceFileAbsolutePathList.Add(childFile);
            }
        }

        foreach (var childDir in Directory.EnumerateDirectories(targetDir))
        {
            if (Path.GetFileName(childDir) == "node_modules")
            {
                //
            }
            else if (Path.GetFileName(childDir) == ".git")
            {
                //
            }
            else if (Path.GetFileName(childDir) == ".vscode")
            {
                //
            }
            else if (Path.GetFileName(childDir) == "out")
            {
                //
            }
            else if (Path.GetFileName(childDir) == "bin")
            {
                //
            }
            else if (Path.GetFileName(childDir) == "obj")
            {
                //
            }
            else
            {
                Recursive_FileDiscovery(childDir);
            }
        }
    }

    /// <summary>
    /// Returns the positionIndex if found, otherwise -1.
    /// </summary>
    public int FindPositionFromLineAndCharacter(string myPath, List<char> chars, int indexLine, int indexCharacter)
    {
        // current line index
        var line = 0;
        // current character index amongst a line
        var character = 0;
        if (line == indexLine && character == indexCharacter)
        {
            // TODO: chars.Count == 0; write it in a way that isn't scuffed?
            // TODO: this is actually saying a bug exists when the "position" turns out to be count
            return 0;
        }
        for (var i = 0; i < chars.Count; i++)
        {
            if (line == indexLine && character == indexCharacter)
            {
                return i;
            }
            else if (line > indexLine)
            {
                return -1;
            }

            switch (chars[i])
            {
                case '\r':
                    line++;
                    character = 0;
                    if (i <= chars.Count - 2)
                    {
                        if (chars[i + 1] == '\n')
                        {
                            i++;
                        }
                    }
                    break;
                case '\n':
                    line++;
                    character = 0;
                    break;
                default:
                    character++;
                    break;
            }
        }
        if (line == indexLine && character == indexCharacter)
        {
            // TODO: chars.Count == 0; write it in a way that isn't scuffed?
            // TODO: this is actually saying a bug exists when the "position" turns out to be count
            return chars.Count;
        }
        return -1;
    }
}
