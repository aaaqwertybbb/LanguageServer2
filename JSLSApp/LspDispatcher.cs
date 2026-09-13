using JSLSApp.LspTypes;
using System.Text.Json;
using Range = JSLSApp.LspTypes.Range;

namespace JSLSApp;

internal static class LspDispatcher
{
    internal static JavaScriptWorkspace _javaScriptWorkspace = JavaScriptWorkspace.Empty;
    /// <summary>
    /// TODO: awkward and...
    /// I don't think 'slice' is in LSP specification but I need to start like this cause it is only way I'll get something "initially working".
    /// </summary>
    internal static TextDocumentCompletionItem[]? _completionItemArray;
    internal static SyntaxNode? _completionItemArray_nodeBasedOn;

    public static object? GiveMessage(string content, Message request)
    {
        switch (request?.Method)
        {
            case "initialize":
                return DeserializeContent_Initialize(content, request);
            case "textDocument/didOpen":
                return DeserializeContent_DidOpen(content, request);
            case "textDocument/didClose":
                return DeserializeContent_DidClose(content, request);
            case "textDocument/didChange":
                return DeserializeContent_DidChange(content, request);
            case "textDocument/documentSymbol":
                return DeserializeContent_DocumentSymbol(content, request);
            case "textDocument/hover":
                return DeserializeContent_Hover(content, request);
            case "textDocument/definition":
                return DeserializeContent_Definition(content, request);
            case "textDocument/completion":
                return DeserializeContent_Completion(content, request);
            case "textDocument/completion_slice":
                return DeserializeContent_CompletionSlice(content, request);
            case "textDocument/CustomFullFileLexRequest":
                return DeserializeContent_CustomFullFileLexRequest(content, request);
            default:
                return request;
        }
    }

    private static Message DeserializeContent_CustomFullFileLexRequest(string content, Message request)
    {
        var customFullFileLexRequest = JsonSerializer.Deserialize<CustomFullFileLexRequest>(content);
        if (customFullFileLexRequest is null || customFullFileLexRequest.@params.textDocument.uri is null)
        {
            return request;
        }

        customFullFileLexRequest.@params.textDocument.uri = EnsureLocalPath(customFullFileLexRequest.@params.textDocument.uri);

        var found = _javaScriptWorkspace.OpenedSourceFileAbsolutePathToInMemoryContentMap.TryGetValue(customFullFileLexRequest.@params.textDocument.uri, out var javaScriptDocument);
        if (!found || javaScriptDocument is null)
        {
            return request;
        }

        var javascriptParser = new JavaScriptParser(javaScriptDocument, _javaScriptWorkspace);
        javaScriptDocument.CompilationUnit = javascriptParser.Parse();

        var textDocumentDocumentSymbolResponse = new CustomFullFileLexResponse(customFullFileLexRequest.id, javascriptParser.PsuedoFourFieldTrackedSyntaxList.ToArray());
        Console.Out.WriteLine(Program.MAIN_encodeMessageObject(textDocumentDocumentSymbolResponse));

        return request;
    }

    private static Message DeserializeContent_CompletionSlice(string content, Message request)
    {
        var completionRequestSlice = JsonSerializer.Deserialize<TextDocumentCompletionRequest_slice>(content);
        if (completionRequestSlice is null || completionRequestSlice.@params.textDocument.uri is null || _completionItemArray is null)
        {
            return request;
        }

        completionRequestSlice.@params.textDocument.uri = EnsureLocalPath(completionRequestSlice.@params.textDocument.uri);

        var found = _javaScriptWorkspace.OpenedSourceFileAbsolutePathToInMemoryContentMap.TryGetValue(completionRequestSlice.@params.textDocument.uri, out var javaScriptDocument);
        if (!found || javaScriptDocument is null)
        {
            return request;
        }

        var textDocumentCompletionResponseResult = new TextDocumentCompletionResponseResult()
        {
            isIncomplete = false,
            items = _completionItemArray.Skip(completionRequestSlice.@params.indexStart).Take(completionRequestSlice.@params.indexEnd - completionRequestSlice.@params.indexStart).ToArray(),
            itemsStart = completionRequestSlice.@params.indexStart,
            itemsEnd = completionRequestSlice.@params.indexEnd,
            totalLength = _completionItemArray.Length,
        };

        var textDocumentCompletionResponse = new TextDocumentCompletionResponse(completionRequestSlice.id, textDocumentCompletionResponseResult);
        Console.Out.WriteLine(Program.MAIN_encodeMessageObject(textDocumentCompletionResponse));

        return request;
    }

    private static Message DeserializeContent_Definition(string content, Message request)
    {
        var definitionRequest = JsonSerializer.Deserialize<TextDocumentDefinitionRequest>(content);
        if (definitionRequest is null || definitionRequest.@params.textDocument.uri is null)
        {
            return request;
        }

        definitionRequest.@params.textDocument.uri = EnsureLocalPath(definitionRequest.@params.textDocument.uri);

        var found = _javaScriptWorkspace.OpenedSourceFileAbsolutePathToInMemoryContentMap.TryGetValue(definitionRequest.@params.textDocument.uri, out var javaScriptDocument);
        if (!found || javaScriptDocument is null)
        {
            return request;
        }

        var documentSymbolArray = new DocumentSymbol[javaScriptDocument.CompilationUnit.FunctionDefinitionStartPositionList.Count];
        for (int i = 0; i < javaScriptDocument.CompilationUnit.FunctionDefinitionStartPositionList.Count; i++)
        {
            var functionDefinition = javaScriptDocument.CompilationUnit.FunctionDefinitionStartPositionList[i];
            documentSymbolArray[i] = new DocumentSymbol
            {
                //name
                kind = SymbolKind.Function,
                name = functionDefinition.Name,
                range = new Range
                {
                    start = functionDefinition.StartPosition,
                    end = functionDefinition.StartPosition
                }
            };
        }

        SyntaxNode? result_node = null;

        var totalChecks = 0;

        result_node = SyntaxHelper.RecursiveSearch(javaScriptDocument.CompilationUnit.BodyList, definitionRequest.@params.position.line, ref totalChecks);

        if (result_node is not null && result_node.SyntaxKind == SyntaxKind.VariableReferenceNode)
        {
            var variableReferenceNode = (VariableReferenceNode)result_node;
            var range = new Range
            {
                start = new Position
                {
                    line = variableReferenceNode.DefinitionLine,
                    character = 0
                },
                end = result_node.End,
            };

            //nodeString = $"{result_node.SyntaxKind}~{result_node.Id_name}";

            var textDocumentDefinitionResponse = new TextDocumentDefinitionResponse(definitionRequest.id, definitionRequest.@params.textDocument.uri, range);
            Console.Out.WriteLine(Program.MAIN_encodeMessageObject(textDocumentDefinitionResponse));
            return request;
        }
        else
        {
            //nodeString = "result_node~was_null";
            // TODO: What does LSP say for responding the failed result null or...?
            var textDocumentDefinitionResponse = new TextDocumentDefinitionResponse(definitionRequest.id, definitionRequest.@params.textDocument.uri, null);
            Console.Out.WriteLine(Program.MAIN_encodeMessageObject(textDocumentDefinitionResponse));
            return request;
        }
    }

    private static Message DeserializeContent_Completion(string content, Message request)
    {
        var completionRequest = JsonSerializer.Deserialize<TextDocumentCompletionRequest>(content);
        if (completionRequest is null || completionRequest.@params.textDocument.uri is null)
        {
            return request;
        }

        completionRequest.@params.textDocument.uri = EnsureLocalPath(completionRequest.@params.textDocument.uri);

        var found = _javaScriptWorkspace.OpenedSourceFileAbsolutePathToInMemoryContentMap.TryGetValue(completionRequest.@params.textDocument.uri, out var javaScriptDocument);
        if (!found || javaScriptDocument is null)
        {
            return request;
        }

        SyntaxNode? result_node = null;

        var totalChecks = 0;
        var nodeCount = 0; // the defined/declared nodes

        result_node = SyntaxHelper.RecursiveSearch_BodyOnly(javaScriptDocument.CompilationUnit.BodyList, javaScriptDocument.CompilationUnit.LexicalScope, completionRequest.@params.position.line, ref totalChecks, ref nodeCount);

        // TODO: This misses the count of the global scope if only the global scope is checked...
        // ...I'm gonna add it as a line immediately following this invocation as a hack for now. (2 of 2)
        if (nodeCount == 0)
        {
            nodeCount += javaScriptDocument.CompilationUnit.LexicalScope.Count;
        }

        if (nodeCount > 25)
        {
            nodeCount = 25;
        }

        /*
        - [x] fix the autocomplete final element when scroll not work vs arrowdown
        - [ ] Something like flat list of [(index,len,node), ...] then binary search
	        - [ ] store the amount of menu options that fit in the slice, and then just swap their content each time a slice is requested.
        - [ ] pick something?
        - [ ] The includes are gonna be just another node that brings the entirety of the single file ast of that file into the current one through an include node or something or like flattens the file ast into the global copy paste all the nodes
         */

        if (_completionItemArray is not null && _completionItemArray_nodeBasedOn == result_node)
        {
            //if (_completionItemArray.Length > 0)
            //{
            //    _completionItemArray[0] = new TextDocumentCompletionItem()
            //    {
            //        label = $"it was reused",
            //        kind = (int)CompletionItemKind.Text
            //    };
            //}

            // TODO: Consider not making a flat array but instead traversing the nodes themselves as a means of doing an autocomplete menu.
        }
        else
        {
            string nodeString;
            if (result_node is not null)
            {
                nodeString = $"{result_node.SyntaxKind}~{result_node.Id_name}";
            }
            else
            {
                nodeString = "result_node~was_null";
            }

            int childNodeLength;
            List<SyntaxNode> bodyList;
            if (result_node?.Body?.BodyList is not null)
            {
                childNodeLength = result_node.Body.BodyList.Count;
                bodyList = result_node.Body.BodyList;
                _completionItemArray_nodeBasedOn = result_node;
            }
            else
            {
                childNodeLength = javaScriptDocument.CompilationUnit.BodyList.Count;
                bodyList = javaScriptDocument.CompilationUnit.BodyList;
                _completionItemArray_nodeBasedOn = null;
            }

            var completionItemArray = new TextDocumentCompletionItem[nodeCount];

            if (bodyList is not null)
            {
                var target_bodyList = bodyList;
                Body? target_body = null;
                if (target_bodyList != javaScriptDocument.CompilationUnit.BodyList)
                {
                    target_body = result_node!.Body;
                }

                int total = 0;

                for (; total < nodeCount;)
                {
                    for (int i = 0; i < target_bodyList.Count; i++)
                    {
                        var node = target_bodyList[i];
                        
                        if (total < nodeCount)
                        {
                            if (node.Body is not null)
                            {
                                completionItemArray[total++] = new TextDocumentCompletionItem()
                                {
                                    label = $"{node.Body.Type} {node.Id_name}",
                                    kind = (int)CompletionItemKind.Text
                                };
                            }
                            else
                            {
                                // I need to figure out why this is an index out of range error then I can be done
                                // as I typed it I think I saw it?
                                completionItemArray[total++] = new TextDocumentCompletionItem()
                                {
                                    label = $"{node.Id_type} {node.Id_name}",
                                    kind = (int)CompletionItemKind.Text
                                };
                            }
                        }
                    }

                    if (total < nodeCount)
                    {
                        if (target_body?.Scope?.Parent?.Body?.BodyList is not null)
                        {
                            target_bodyList = target_body.Scope.Parent.Body.BodyList;
                            target_body = target_body.Scope.Parent.Body;
                        }
                        else
                        {
                            if (target_bodyList == javaScriptDocument.CompilationUnit.BodyList)
                            {
                                break;
                            }
                            else
                            {
                                target_bodyList = javaScriptDocument.CompilationUnit.BodyList;
                                target_body = null;
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            _completionItemArray = completionItemArray;
        }

        var textDocumentCompletionResponseResult = new TextDocumentCompletionResponseResult()
        {
            isIncomplete = false,
            items = Array.Empty<TextDocumentCompletionItem>(),
            itemsStart = 0,
            itemsEnd = 0,
            totalLength = _completionItemArray.Length,
        };

        var textDocumentCompletionResponse = new TextDocumentCompletionResponse(completionRequest.id, textDocumentCompletionResponseResult);
        Console.Out.WriteLine(Program.MAIN_encodeMessageObject(textDocumentCompletionResponse));

        return request;
    }

    private static Message DeserializeContent_Hover(string content, Message request)
    {
        var hoverRequest = JsonSerializer.Deserialize<TextDocumentHoverRequest>(content);
        if (hoverRequest is null || hoverRequest.@params.textDocument.uri is null)
        {
            return request;
        }

        hoverRequest.@params.textDocument.uri = EnsureLocalPath(hoverRequest.@params.textDocument.uri);

        var found = _javaScriptWorkspace.OpenedSourceFileAbsolutePathToInMemoryContentMap.TryGetValue(hoverRequest.@params.textDocument.uri, out var javaScriptDocument);
        if (!found || javaScriptDocument is null)
        {
            return request;
        }
        
        var documentSymbolArray = new DocumentSymbol[javaScriptDocument.CompilationUnit.FunctionDefinitionStartPositionList.Count];
        for (int i = 0; i < javaScriptDocument.CompilationUnit.FunctionDefinitionStartPositionList.Count; i++)
        {
            var functionDefinition = javaScriptDocument.CompilationUnit.FunctionDefinitionStartPositionList[i];
            documentSymbolArray[i] = new DocumentSymbol
            {
                //name
                kind = SymbolKind.Function,
                name = functionDefinition.Name,
                range = new Range
                {
                    start = functionDefinition.StartPosition,
                    end = functionDefinition.StartPosition
                }
            };
        }

        SyntaxNode? result_node = null;

        var totalChecks = 0;

        result_node = SyntaxHelper.RecursiveSearch(javaScriptDocument.CompilationUnit.BodyList, hoverRequest.@params.position.line, ref totalChecks);

        string nodeString;
        if (result_node is not null)
        {
            nodeString = $"{result_node.SyntaxKind}~{result_node.Id_name}";
        }
        else
        {
            nodeString = "result_node~was_null";
        }

        var textDocumentHoverResponse = new TextDocumentHoverResponse(hoverRequest.id, $"tooltip example for ({nodeString}) ({hoverRequest.@params.position.line}, {hoverRequest.@params.position.character}) {hoverRequest.@params.textDocument.uri}");
        Console.Out.WriteLine(Program.MAIN_encodeMessageObject(textDocumentHoverResponse));

        return request;
    }

    private static Message DeserializeContent_DocumentSymbol(string content, Message request)
    {
        var symbolRequest = JsonSerializer.Deserialize<TextDocumentDocumentSymbolRequest>(content);
        if (symbolRequest is null || symbolRequest.@params.textDocument.uri is null)
        {
            return request;
        }

        symbolRequest.@params.textDocument.uri = EnsureLocalPath(symbolRequest.@params.textDocument.uri);

        var found = _javaScriptWorkspace.OpenedSourceFileAbsolutePathToInMemoryContentMap.TryGetValue(symbolRequest.@params.textDocument.uri, out var javaScriptDocument);
        if (!found || javaScriptDocument is null)
        {
            return request;
        }

        var javascriptParser = new JavaScriptParser(javaScriptDocument, _javaScriptWorkspace);
        javaScriptDocument.CompilationUnit = javascriptParser.Parse();

        var documentSymbolArray = new DocumentSymbol[javaScriptDocument.CompilationUnit.FunctionDefinitionStartPositionList.Count];
        for (int i = 0; i < javaScriptDocument.CompilationUnit.FunctionDefinitionStartPositionList.Count; i++)
        {
            var functionDefinition = javaScriptDocument.CompilationUnit.FunctionDefinitionStartPositionList[i];
            documentSymbolArray[i] = new DocumentSymbol
            {
                //name
                kind = SymbolKind.Function,
                name = functionDefinition.Name,
                range = new Range
                {
                    start = functionDefinition.StartPosition,
                    end = functionDefinition.StartPosition
                }
            };
        }

        var textDocumentDocumentSymbolResponse = new TextDocumentDocumentSymbolResponse(symbolRequest.id, documentSymbolArray);
        Console.Out.WriteLine(Program.MAIN_encodeMessageObject(textDocumentDocumentSymbolResponse));

        return request;
    }

    private static Message DeserializeContent_DidChange(string content, Message request)
    {
        var didChangeTextDocumentNotification = JsonSerializer.Deserialize<DidChangeTextDocumentNotification>(content);
        if (didChangeTextDocumentNotification is null || didChangeTextDocumentNotification.@params.textDocument.uri is null)
        {
            return request;
        }

        didChangeTextDocumentNotification.@params.textDocument.uri = EnsureLocalPath(didChangeTextDocumentNotification.@params.textDocument.uri);

        if (didChangeTextDocumentNotification.@params.contentChanges is null)
        {
            File.AppendAllText(Program.myPath, $"\n====didChangeTextDocumentNotification.@params.contentChanges is null====\n");
            return request;
        }

        if (didChangeTextDocumentNotification.@params.contentChanges.Length == 0)
        {
            File.AppendAllText(Program.myPath, $"\n====didChangeTextDocumentNotification.@params.contentChanges.Length == 0====\n");
            return request;
        }
        
        foreach (var item in didChangeTextDocumentNotification.@params.contentChanges)
        {
            if (item.range is null)
                File.AppendAllText(Program.myPath, $"\n====didChangeTextDocumentNotification item range is null====\n");

            if (item.rangeLength is null)
                File.AppendAllText(Program.myPath, $"\n====didChangeTextDocumentNotification item rangeLength is null====\n");

            if (item.text is null)
                File.AppendAllText(Program.myPath, $"\n====didChangeTextDocumentNotification item text is null====\n");
        }

        if (_javaScriptWorkspace is null)
        {
            File.AppendAllText(Program.myPath, $"\n====_javaScriptWorkspace is null====\n");
            return request;
        }

        _javaScriptWorkspace.DidChangeTextDocumentNotification(Program.myPath, didChangeTextDocumentNotification.@params.textDocument.uri, didChangeTextDocumentNotification.@params.contentChanges);
        return request;
    }

    private static Message DeserializeContent_DidClose(string content, Message request)
    {
        var didCloseTextDocumentNotification = JsonSerializer.Deserialize<DidCloseTextDocumentNotification>(content);
        if (didCloseTextDocumentNotification is null || didCloseTextDocumentNotification.@params.textDocument.uri is null)
        {
            return request;
        }

        didCloseTextDocumentNotification.@params.textDocument.uri = EnsureLocalPath(didCloseTextDocumentNotification.@params.textDocument.uri);

        if (_javaScriptWorkspace is null)
        {
            File.AppendAllText(Program.myPath, $"\n====_javaScriptWorkspace is null====\n");
            return request;
        }

        _javaScriptWorkspace.DidCloseTextDocumentNotification(Program.myPath, didCloseTextDocumentNotification.@params.textDocument.uri);
        return request;
    }

    private static object DeserializeContent_DidOpen(string content, Message request)
    {
        var didOpenTextDocumentNotification = JsonSerializer.Deserialize<DidOpenTextDocumentNotification>(content);
        if (didOpenTextDocumentNotification is null || didOpenTextDocumentNotification.@params.textDocument.uri is null || didOpenTextDocumentNotification.@params.textDocument.text is null)
        {
            return request;
        }

        didOpenTextDocumentNotification.@params.textDocument.uri = EnsureLocalPath(didOpenTextDocumentNotification.@params.textDocument.uri);

        if (didOpenTextDocumentNotification.@params.textDocument.uri is null)
        {
            return didOpenTextDocumentNotification;
        }
            
        if (_javaScriptWorkspace is null)
        {
            File.AppendAllText(Program.myPath, $"\n====_javaScriptWorkspace is null====\n");
            return didOpenTextDocumentNotification;
        }
        
        _javaScriptWorkspace.DidOpenTextDocumentNotification(
            Program.myPath,
            didOpenTextDocumentNotification.@params.textDocument.uri,
            didOpenTextDocumentNotification.@params.textDocument.text);
        return didOpenTextDocumentNotification;
    }

    private static object DeserializeContent_Initialize(string content, Message request)
    {
        var initializeRequest = JsonSerializer.Deserialize<InitializeRequest>(content);
        if (initializeRequest is null)
        {
            return request;
        }

        // TODO: what is specified for when you receive an initialize request after you've already initialized.
        _javaScriptWorkspace = JavaScriptWorkspace.Empty;
        _completionItemArray = null;
        _completionItemArray_nodeBasedOn = null;

        if (!string.IsNullOrWhiteSpace(initializeRequest.@params.rootUri))
        {
            _javaScriptWorkspace = new JavaScriptWorkspace(initializeRequest.@params.rootUri);
        }
        else if (initializeRequest.@params.workspaceFolders is null)
        {
            File.AppendAllText(Program.myPath, $"\n====initializeRequest?.Params?.workspaceFolders:null====\n");
        }
        else
        {
            _javaScriptWorkspace = new JavaScriptWorkspace(initializeRequest.@params.workspaceFolders);
            File.AppendAllText(Program.myPath, $"\n====initializeRequest?.Params?.workspaceFolders...====\n");
            foreach (var workspaceFolder in initializeRequest.@params.workspaceFolders)
            {
                File.AppendAllText(Program.myPath, $"\n====workspaceFolder: name->{workspaceFolder.name} | uri->{workspaceFolder.uri}====\n");
            }
        }

        var initializeResponse = new InitializeResponse(new InitializeResponseResult());
        Console.Out.WriteLine(Program.MAIN_encodeMessageObject(initializeResponse));
        return initializeRequest;
    }

    /// <summary>
    /// TODO: this seems a bit GC expensive
    /// </summary>
    public static string EnsureLocalPath(string sourceFileAbsolutePath)
    {
        return new Uri(sourceFileAbsolutePath).LocalPath;
    }

    /*
     TODO: Deserializing a class where the class contains properties of which are reference type, but not marked as nullable;...
           ...If the deserializer finds these properties to be null, then it will just let that be so? i.e.: I have to validate them myself or...?

    Google AI:

    < ...
    <
    < # System.Text.Json (Default .NET)
    < 
    < By default, System.Text.Json ignores the non-nullable annotations and will set the property to null if the data is missing from the JSON.
    <
    < To force validation automatically during deserialization, use the following approaches:
    <
    < - The required Modifier (Recommended for .NET 7+):...
    < - JsonRequired Attribute:...
    < - Global Contract Resolver (.NET 8+):...
    <
    < ...

    > what does 'required' do for a nullable reference type? They have to explicitly have the json field ...?
    (my ... was text that forms the question a bit but taken literally is innacurate so I wanna avoid anyone reading the sentence in its entirety the ... was: "with value 'null'")

    < Yes...
    <
    < ...

    TODO tomorrow:
    - propagate up parent scopes and create one list of all identifiers that are possibly available then re-use it when you slice
    - import
        - make ast and connect the two ast(s)?
    - cache syntax highlighting for the sections you've syntax highlighted drawn

     */

    /*
     * If anyone wants to know what the next steps would be for something like this:
     * you could consider changing all the strings to ints
     * by finding a hash function that results in a unique number over the closed set of all keywords
     * 
     * Then any identifier collisions would need to be verified character by character
     * but they should be rare.
     * 
     * This removes a MASSIVE amount of strings.
     * The lexical scope isn't about indexing a string it should be some kind of non-allocated value
     * like the hash
     * 
     * ========
     * 
     * One of the things I did very wrong in the past was trying to flatten the AST into a single array.
     * I highly recommend NOT doing this after years of doing this.
     * 
     * The main reason I had to do that was having the "language server" and "client" being the same app
     * and thus my GC overhead was massive.
     * 
     * By splitting them out, the cost of the AST isn't nearly as large relative to the total object count
     * and by using one you greatly simplify the logic.
     * 
     * My initial thought was that adding a server-client architecture would slow the app down.
     * But it is the complete opposite (maybe you could find a way but I couldn't).
     * 
     * Because the cost of sending messages between them is non-zero yes, but it is nothing
     * compared to the cost of storing all this allocated memory in this C# program in the same program
     * as the UI and having the garbage collector explode.
     * 
     * I think I want to completely stay away from writing code on this project until I hit 199.9 lbs.
     * I am quick to get obsessed with things so taking some time away I think is a good thing.
     */
}
