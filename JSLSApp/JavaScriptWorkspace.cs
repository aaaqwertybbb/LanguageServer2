using JSLSApp.LspTypes;
using JSLSApp.Syntax;

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
        //_workspaceFolders = new();
        //_workspaceFolders.Add(new WorkspaceFolder
        //{
        //    name = rootAbsolutePath,
        //    uri = rootAbsolutePath
        //});
        //
        //Recursive_FileDiscovery(rootAbsolutePath);
    }

    public JavaScriptWorkspace(List<WorkspaceFolder> workspaceFolders)
    {
        //_isWorkspace = true;
        //_workspaceFolders = workspaceFolders;
        //foreach (var workspaceFolder in _workspaceFolders)
        //{
        //    Recursive_FileDiscovery(workspaceFolder.uri);
        //}
    }

    public void DidOpenTextDocumentNotification(string sourceFileAbsolutePath, string text)
    {
        var localPath = new Uri(sourceFileAbsolutePath).LocalPath;
        OpenedSourceFileAbsolutePathToInMemoryContentMap.Add(sourceFileAbsolutePath, new JavaScriptDocument(localPath, text.ToList()));
    }
    
    public void DidCloseTextDocumentNotification(string sourceFileAbsolutePath)
    {
        var localPath = new Uri(sourceFileAbsolutePath).LocalPath;
        var wasRemoved = OpenedSourceFileAbsolutePathToInMemoryContentMap.Remove(localPath);
    }

    public void DidChangeTextDocumentNotification(string sourceFileAbsolutePath, TextDocumentContentChangeEvent[] contentChanges)
    {
        var found = OpenedSourceFileAbsolutePathToInMemoryContentMap.TryGetValue(sourceFileAbsolutePath, out var doc);
        if (!found)
        {
            //Console.Out.WriteLine($"\n====ERROR DidChangeTextDocumentNotification did not find file====\n");
            return;
        }

        if (contentChanges.Length == 0)
        {
            //Console.Out.WriteLine($"\n====DidChangeTextDocumentNotification; {nameof(contentChanges)} length was 0====\n");
            return;
        }

        // TODO: is 'contentChanges' guaranteed to be sorted?
        foreach (var change in contentChanges)
        {
            if (change.range is null)
            {
                //Console.Out.WriteLine($"\n====DidChangeTextDocumentNotification; TODO: support 'if (change.range is null)'====\n");
                continue;
            }

            if (change.range.start.line != change.range.end.line || change.range.start.character != change.range.end.character)
            {
                if (change.text is not null && change.text.Length > 0)
                {
                    // TODO: You're allowed to provide text to insert here as well, so this needs to be supported.
                    //Console.Out.WriteLine($"\n====change.text is not null====\n");
                    continue;
                }

                var startIndexPosition = FindPositionFromLineAndCharacter(doc.Chars, change.range.start.line, change.range.start.character);
                var endIndexPosition = FindPositionFromLineAndCharacter(doc.Chars, change.range.end.line, change.range.end.character);

                var count = endIndexPosition - startIndexPosition;

                //(SyntaxNode before, SyntaxNode? among, SyntaxNode after) = FindNodesAdjacentToTheEdit(doc, change.range.start, change.range.end);
                //if (among is null)
                //{
                //    after.DistanceFromPreviousNode -= count;
                //}

                doc.Chars.RemoveRange(startIndexPosition, count);
            }
            else
            {
                if (change.text is null)
                {
                    //Console.Out.WriteLine($"\n====DidChangeTextDocumentNotification; change.text is null====\n");
                    continue;
                }

                var indexPosition = FindPositionFromLineAndCharacter(doc.Chars, change.range.start.line, change.range.start.character);
                if (indexPosition == -1)
                {
                    //Console.Out.WriteLine($"\n====DidChangeTextDocumentNotification; if (indexPosition == -1)====\n");
                    continue;
                }

                //(SyntaxNode before, SyntaxNode? among, SyntaxNode after) = FindNodesAdjacentToTheEdit(doc, change.range.start, change.range.start);
                //if (among is null)
                //{
                //    after.DistanceFromPreviousNode += change.text.Length;
                //}
                
                doc.Chars.InsertRange(indexPosition, change.text);
            }
        }
    }

    /// <summary>
    /// find the current node (which is gonna be null for this simplest scenario of an edit that "doesn't change anything other than text positions")
    /// then the node that came before
    /// the node that came after
    /// 
    /// 'int endLine, int endCharacter' can be ignored for the first version because it is assumed that no modifications to the AST are taking place
    /// and thus from "start to end" you only have whitespace essentially is the first idea.
    /// 
    /// before,among,after are all in reference to the START of a given node.
    /// 
    /// this isn't correct/done I only stopped to cook 1 lb of 98% fat free ground chicken with sriracha I'm gonna eat it now
    /// </summary>
    public (SyntaxNodeWithPosition before, SyntaxNodeWithPosition among, SyntaxNodeWithPosition after) FindNodesAdjacentToTheEdit(JavaScriptDocument javaScriptDocument, Position start, Position end)
    {
        SyntaxNode? result_node = null;

        var currentIndexPosition = 0;
        var currentIndexLine = 0;
        var currentIndexCharacter = 0;

        // Contains both IsScopeProvider==true nodes and IsScopeProvider==false nodes
        // BECAUSE it contains the definition node itself as the first thing that gets pushed.
        // Other than that first pushed node everything is IsScopeProvider==true.
        Stack<SyntaxNode> inclusiveFinalNodeToConsiderAtEachDepth = new();
        Stack<(SyntaxNode ScopeProvider, int ChildIndexOfScopeProviderAmongItsSiblings)> returnStack = new();

        SyntaxNodeWithPosition before = default;
        SyntaxNodeWithPosition among = default;
        SyntaxNodeWithPosition after = default;

        before.Node = javaScriptDocument.CompilationUnit.GlobalNode;
        before.Position = new Position(currentIndexLine, currentIndexCharacter);
        before.PositionIndex = currentIndexPosition;
        SyntaxHelper.RecursiveSearch_FindNodesAdjacentToTheEdit(javaScriptDocument.Chars, start.line, start.character, ref currentIndexPosition, ref currentIndexLine, ref currentIndexCharacter, ref before, ref among, ref after, inclusiveFinalNodeToConsiderAtEachDepth, returnStack);

        return (before, among, after);
    }

    /*public void Recursive_FileDiscovery(string targetDir)
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
    }*/

    /// <summary>
    /// Returns the positionIndex if found, otherwise -1.
    /// </summary>
    public int FindPositionFromLineAndCharacter(List<char> chars, int indexLine, int indexCharacter)
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
