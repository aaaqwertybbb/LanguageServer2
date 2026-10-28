using JSLSApp.LspTypes;
using JSLSApp.Syntax;
using System.Text.Json;
using System.Xml.Linq;
using Range = JSLSApp.LspTypes.Range;

namespace JSLSApp;

internal static class LspDispatcher
{
    internal static JavaScriptWorkspace _javaScriptWorkspace = JavaScriptWorkspace.Empty;
    /// <summary>
    /// TODO: awkward and...
    /// I don't think 'slice' is in LSP specification but I need to start like this cause it is only way I'll get something "initially working".
    /// </summary>
    internal static string _completionItemArray_sliceFiltered_previousFilter = string.Empty;
    internal static TextDocumentCompletionItem[]? _completionItemArray_sliceFiltered;
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
            case "textDocument/CustomEditorVerifyLspTextAgainstEditorTextRequest":
                return DeserializeContent_CustomEditorVerifyLspTextAgainstEditorTextRequest(content, request);
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

        var base64String = Convert.ToBase64String(javascriptParser.SyntaxHighlightByteList);

        var textDocumentDocumentSymbolResponse = new CustomFullFileLexResponse(customFullFileLexRequest.id, base64String);
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

        if (string.IsNullOrWhiteSpace(completionRequestSlice.@params.filter))
        {
            completionRequestSlice.@params.filter = string.Empty;
        }

        var filterChanged = false;

        if (_completionItemArray_sliceFiltered_previousFilter != completionRequestSlice.@params.filter)
        {
            if (completionRequestSlice.@params.filter.StartsWith(_completionItemArray_sliceFiltered_previousFilter) || completionRequestSlice.@params.filter.EndsWith(_completionItemArray_sliceFiltered_previousFilter))
            {
                filterChanged = true;
                _completionItemArray_sliceFiltered_previousFilter = completionRequestSlice.@params.filter;
                _completionItemArray_sliceFiltered = _completionItemArray_sliceFiltered.Where(x => x.label.Contains(_completionItemArray_sliceFiltered_previousFilter)).ToArray();
            }
            else
            {
                filterChanged = true;
                _completionItemArray_sliceFiltered_previousFilter = completionRequestSlice.@params.filter;
                _completionItemArray_sliceFiltered = _completionItemArray.Where(x => x.label.Contains(_completionItemArray_sliceFiltered_previousFilter)).ToArray();
            }
        }

        var lengthSlice = completionRequestSlice.@params.indexEnd - completionRequestSlice.@params.indexStart;
        if (_completionItemArray_sliceFiltered.Length - completionRequestSlice.@params.indexStart < lengthSlice)
        {
            if (filterChanged)
            {
                completionRequestSlice.@params.indexStart -= (lengthSlice - (_completionItemArray_sliceFiltered.Length - completionRequestSlice.@params.indexStart));
                if (completionRequestSlice.@params.indexStart < 0)
                {
                    completionRequestSlice.@params.indexStart = 0;
                }
            }
        }
        var sliceItems = _completionItemArray_sliceFiltered.Skip(completionRequestSlice.@params.indexStart).Take(lengthSlice).ToArray();

        var textDocumentCompletionResponseResult = new TextDocumentCompletionResponseResult()
        {
            isIncomplete = false,
            items = sliceItems,
            itemsStart = completionRequestSlice.@params.indexStart,
            itemsEnd = completionRequestSlice.@params.indexEnd,
            totalLength = _completionItemArray_sliceFiltered.Length,
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

        SyntaxNode? result_node = null;

        var totalChecks = 0;

        var currentIndexPosition = 0;
        var currentIndexLine = 0;
        var currentIndexCharacter = 0;

        result_node = SyntaxHelper.RecursiveSearch(javaScriptDocument.Chars, javaScriptDocument.CompilationUnit.GlobalNode, definitionRequest.@params.position.line, definitionRequest.@params.position.character, ref totalChecks, ref currentIndexPosition, ref currentIndexLine, ref currentIndexCharacter);

        // Contains both IsScopeProvider==true nodes and IsScopeProvider==false nodes
        // BECAUSE it contains the definition node itself as the first thing that gets pushed.
        // Other than that first pushed node everything is IsScopeProvider==true.
        Stack<SyntaxNode> inclusiveFinalNodeToConsiderAtEachDepth = new();
        Stack<(SyntaxNode ScopeProvider, int ChildIndexOfScopeProviderAmongItsSiblings)> returnStack = new();

        if (result_node is not null && result_node.SyntaxKind == SyntaxKind.VariableReferenceNode)
        {
            var referenceNode = (VariableReferenceNode)result_node;
            if (referenceNode.VariableDeclarationNode.SyntaxKind == SyntaxKind.VariableDeclarationNode)
            {
                var declarationNode = (VariableDeclarationNode)referenceNode.VariableDeclarationNode;
                if (declarationNode.ParentScopeProvider is not null)
                {
                    var indexOf = declarationNode.ParentScopeProvider.GetScope().BodyList.IndexOf(declarationNode);
                    if (indexOf != -1)
                    {
                        

                        var targetScopeProvider = declarationNode.ParentScopeProvider;

                        var isFirstLoop = true;

                        while (targetScopeProvider is not null)
                        {
                            if (isFirstLoop)
                            {
                                isFirstLoop = false;
                                inclusiveFinalNodeToConsiderAtEachDepth.Push(declarationNode);
                                inclusiveFinalNodeToConsiderAtEachDepth.Push(targetScopeProvider);
                            }
                            else
                            {
                                if (targetScopeProvider != javaScriptDocument.CompilationUnit.GlobalNode)
                                {
                                    inclusiveFinalNodeToConsiderAtEachDepth.Push(targetScopeProvider);
                                }
                            }
                            targetScopeProvider = targetScopeProvider.GetScope().ParentScopeProvider;
                        }

                        targetScopeProvider = javaScriptDocument.CompilationUnit.GlobalNode;
                        var finalNodeToConsider = inclusiveFinalNodeToConsiderAtEachDepth.Pop();

                        currentIndexPosition = 0;
                        currentIndexLine = 0;
                        currentIndexCharacter = 0;

                        var isNaturalHandOff = true;

                        var isSidePath = false;

                        var i = 0;
                        while (targetScopeProvider is not null)
                        {
                            ref var scope = ref targetScopeProvider.GetScope();
                            for (; i < scope.BodyList.Count; i++)
                            {
                                var childNode = scope.BodyList[i];
                                if (currentIndexPosition < javaScriptDocument.Chars.Count)
                                {
                                    var distance = childNode.DistanceFromPreviousNode;
                                    if (currentIndexPosition + distance >= javaScriptDocument.Chars.Count)
                                        distance = javaScriptDocument.Chars.Count - currentIndexPosition;

                                    for (var j = 0; j < distance; j++)
                                    {
                                        switch (javaScriptDocument.Chars[currentIndexPosition])
                                        {
                                            //case '\r': TODO: Include or not '\r'? When using my own editor I swap everything to '\n' but...
                                            case '\n':
                                                currentIndexLine++;
                                                currentIndexCharacter = 0;
                                                break;
                                            default:
                                                currentIndexCharacter++;
                                                break;
                                        }
                                        currentIndexPosition++;
                                    }
                                }
                                if (childNode.IsScopeProvider)
                                {
                                    if (childNode != finalNodeToConsider)
                                    {
                                        returnStack.Push((targetScopeProvider, i));
                                    }
                                    else
                                    {
                                        if (!isSidePath) // TODO: determine side path!!!!!
                                        {
                                            finalNodeToConsider = inclusiveFinalNodeToConsiderAtEachDepth.Pop();
                                        }
                                    }
                                    targetScopeProvider = childNode;
                                    isNaturalHandOff = true;
                                    break;
                                }
                                else
                                {
                                    if (childNode == finalNodeToConsider)
                                    {
                                        returnStack.Clear(); // TODO: Don't do this just anxiety I have no reason for this line to exist?
                                        targetScopeProvider = null;
                                        break;
                                    }
                                }
                            }
                            if (isNaturalHandOff)
                            {
                                isNaturalHandOff = false;
                            }
                            else
                            {
                                if (returnStack.Count > 0)
                                {
                                    var returnEntry = returnStack.Pop();
                                    targetScopeProvider = returnEntry.ScopeProvider;
                                    i = returnEntry.ChildIndexOfScopeProviderAmongItsSiblings + 1;
                                }
                                else
                                {
                                    targetScopeProvider = null;
                                }
                            }
                        }

                        var range = new Range
                        {
                            start = new Position
                            {
                                line = currentIndexLine,// TODO: relative changes break this; variableReferenceNode.DefinitionLine,
                                character = currentIndexCharacter//variableReferenceNode.DefinitionCharacter
                            },
                            end = new Position //end = result_node.End, // TODO: relative changes break this;
                            {
                                line = currentIndexLine,
                                character = currentIndexCharacter
                            },
                        };

                        //nodeString = $"{result_node.SyntaxKind}~{result_node.Id_name}";
                        Console.Out.WriteLine(Program.MAIN_encodeMessageObject(
                            new TextDocumentDefinitionResponse(definitionRequest.id, definitionRequest.@params.textDocument.uri, range)));
                        return request;
                    }
                }
            }
        }

        //nodeString = "result_node~was_null";
        // TODO: What does LSP say for responding the failed result null or...?
        var textDocumentDefinitionResponse = new TextDocumentDefinitionResponse(definitionRequest.id, definitionRequest.@params.textDocument.uri, null);
        Console.Out.WriteLine(Program.MAIN_encodeMessageObject(textDocumentDefinitionResponse));
        return request;
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
        var definitionNodeCount = 0;
        

        // Step A: Get the closest node to the cursor position that has a scope.
        //         - This is done by starting at the GlobalScope and then recursively checking each child node in the targetScope's "BodyList".
        // Step B: Get the closest node to the cursor position that has a scope.
        //         - This is done by creating a menu option for every definition that exists within the targetNode's "BodyList".
        //         - Each time you do this, you change the targetNode to be the targetNode.Parent
        //         - This gets you the entries of the closest scope to start as the 0th menu option and onward they're the first options you can pick.
        // Caching: If the result of "Step A" is the same node as the previous invocation, just re-use that list of menu options again.

        // Step A: Get the closest node to the cursor position that has a scope.
        result_node = SyntaxHelper.RecursiveSearch_BodyOnly(javaScriptDocument.CompilationUnit.GlobalNode, completionRequest.@params.position.line, ref totalChecks, ref definitionNodeCount);
        result_node ??= javaScriptDocument.CompilationUnit.GlobalNode;
        // TODO: ^ this is bad, node and scope is murky here it feels awkard todo fix it (cuz of the GlobalNode stuff)

        var nodeCountMaxAmountToTake = definitionNodeCount; // the defined/declared nodes

        if (nodeCountMaxAmountToTake > 250)
        {
            nodeCountMaxAmountToTake = 250;
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
            _completionItemArray_nodeBasedOn = result_node;

            // +1 so I can put a message at the 0th menu option
            var completionItemArray = new TextDocumentCompletionItem[nodeCountMaxAmountToTake + 1];
            completionItemArray[0] = new TextDocumentCompletionItem
            {
                label = $"('ctrl+r' to refresh) {nodeCountMaxAmountToTake} of {definitionNodeCount}",
                kind = (int)CompletionItemKind.Text
            };

            int totalTaken = 0;

            ref var targetScope = ref result_node.GetScope();

            for (; totalTaken < nodeCountMaxAmountToTake;)
            {
                for (int i = 0; i < targetScope.BodyList.Count; i++)
                {
                    var node = targetScope.BodyList[i];
                        
                    if (totalTaken < nodeCountMaxAmountToTake)
                    {
                        TextDocumentCompletionItem textDocumentCompletionItem;
                        if (node.SyntaxKind == SyntaxKind.ClassDeclarationNode)
                        {
                            textDocumentCompletionItem = new TextDocumentCompletionItem
                            {
                                label = $"(class) {node.GetIdentifierHash()}",
                                kind = (int)CompletionItemKind.Text
                            };
                        }
                        else if (node.SyntaxKind == SyntaxKind.FunctionDeclarationNode)
                        {
                            textDocumentCompletionItem = new TextDocumentCompletionItem
                            {
                                label = $"{node.GetIdentifierHash()}(?)...",
                                kind = (int)CompletionItemKind.Text
                            };
                        }
                        else if (node.SyntaxKind == SyntaxKind.VariableDeclarationNode)
                        {
                            textDocumentCompletionItem = new TextDocumentCompletionItem
                            {
                                label = $"(id) {node.GetIdentifierHash()}",
                                kind = (int)CompletionItemKind.Text
                            };
                        }
                        else
                        {
                            continue;
                        }
                        // +1 so I can put a message at the 0th menu option
                        completionItemArray[((totalTaken++) + 1)] = textDocumentCompletionItem;
                    }
                }

                if (totalTaken < nodeCountMaxAmountToTake && targetScope.ParentScopeProvider is not null)
                {
                    targetScope = ref targetScope.ParentScopeProvider.GetScope();
                }
                else
                {
                    break;
                }
            }

            _completionItemArray = completionItemArray;
            _completionItemArray_sliceFiltered = _completionItemArray;
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

        var currentIndexPosition = 0;
        var currentIndexLine = 0;
        var currentIndexCharacter = 0;

        result_node = SyntaxHelper.RecursiveSearch(javaScriptDocument.Chars, javaScriptDocument.CompilationUnit.GlobalNode, hoverRequest.@params.position.line, hoverRequest.@params.position.character, ref totalChecks, ref currentIndexPosition, ref currentIndexLine, ref currentIndexCharacter);

        string nodeString;
        if (result_node is not null && result_node.IsIdentifiable)
        {
            nodeString = $"{result_node.SyntaxKind}~{result_node.GetIdentifierHash()}";
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
            //Console.Out.WriteLine($"\n====didChangeTextDocumentNotification.@params.contentChanges is null====\n");
            return request;
        }

        if (didChangeTextDocumentNotification.@params.contentChanges.Length == 0)
        {
            //Console.Out.WriteLine($"\n====didChangeTextDocumentNotification.@params.contentChanges.Length == 0====\n");
            return request;
        }

        /*foreach (var item in didChangeTextDocumentNotification.@params.contentChanges)
        {
            if (item.range is null)
                Console.Out.WriteLine($"\n====didChangeTextDocumentNotification item range is null====\n");

            if (item.rangeLength is null)
                Console.Out.WriteLine($"\n====didChangeTextDocumentNotification item rangeLength is null====\n");

            if (item.text is null)
                Console.Out.WriteLine($"\n====didChangeTextDocumentNotification item text is null====\n");
        }*/

        if (_javaScriptWorkspace is null)
        {
            //Console.Out.WriteLine($"\n====_javaScriptWorkspace is null====\n");
            return request;
        }

        _javaScriptWorkspace.DidChangeTextDocumentNotification(didChangeTextDocumentNotification.@params.textDocument.uri, didChangeTextDocumentNotification.@params.contentChanges);
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
            //Console.Out.WriteLine($"\n====_javaScriptWorkspace is null====\n");
            return request;
        }

        _javaScriptWorkspace.DidCloseTextDocumentNotification(didCloseTextDocumentNotification.@params.textDocument.uri);
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
            //Console.Out.WriteLine($"\n====_javaScriptWorkspace is null====\n");
            return didOpenTextDocumentNotification;
        }
        
        _javaScriptWorkspace.DidOpenTextDocumentNotification(
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
        _completionItemArray_sliceFiltered = null;
        _completionItemArray_nodeBasedOn = null;

        if (!string.IsNullOrWhiteSpace(initializeRequest.@params.rootUri))
        {
            _javaScriptWorkspace = new JavaScriptWorkspace(initializeRequest.@params.rootUri);
        }
        else if (initializeRequest.@params.workspaceFolders is null)
        {
            //Console.Out.WriteLine($"\n====initializeRequest?.Params?.workspaceFolders:null====\n");
        }
        else
        {
            _javaScriptWorkspace = new JavaScriptWorkspace(initializeRequest.@params.workspaceFolders);
            //Console.Out.WriteLine($"\n====initializeRequest?.Params?.workspaceFolders...====\n");
            foreach (var workspaceFolder in initializeRequest.@params.workspaceFolders)
            {
                //Console.Out.WriteLine($"\n====workspaceFolder: name->{workspaceFolder.name} | uri->{workspaceFolder.uri}====\n");
            }
        }

        var initializeResponse = new InitializeResponse(new InitializeResponseResult());
        Console.Out.WriteLine(Program.MAIN_encodeMessageObject(initializeResponse));
        return initializeRequest;
    }

    private static Message DeserializeContent_CustomEditorVerifyLspTextAgainstEditorTextRequest(string content, Message request)
    {
        var customEditorVerifyLspTextAgainstEditorTextRequest = JsonSerializer.Deserialize<CustomEditorVerifyLspTextAgainstEditorTextRequest>(content);
        if (customEditorVerifyLspTextAgainstEditorTextRequest is null || customEditorVerifyLspTextAgainstEditorTextRequest.@params.textDocument.uri is null)
        {
            return request;
        }

        customEditorVerifyLspTextAgainstEditorTextRequest.@params.textDocument.uri = EnsureLocalPath(customEditorVerifyLspTextAgainstEditorTextRequest.@params.textDocument.uri);

        var found = _javaScriptWorkspace.OpenedSourceFileAbsolutePathToInMemoryContentMap.TryGetValue(customEditorVerifyLspTextAgainstEditorTextRequest.@params.textDocument.uri, out var javaScriptDocument);
        if (!found || javaScriptDocument is null)
        {
            return request;
        }

        var customEditorVerifyLspTextAgainstEditorTextResponse = new CustomEditorVerifyLspTextAgainstEditorTextResponse(customEditorVerifyLspTextAgainstEditorTextRequest.id, new string(javaScriptDocument.Chars.ToArray()));
        Console.Out.WriteLine(Program.MAIN_encodeMessageObject(customEditorVerifyLspTextAgainstEditorTextResponse));

        return request;
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
