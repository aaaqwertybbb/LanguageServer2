using JSLSApp.LspTypes;
using JSLSApp.Syntax;
using System.Runtime.InteropServices;
using System.Text;
using Range = JSLSApp.LspTypes.Range;

namespace JSLSApp;

/// <summary>
/// Single threaded and a lot of pooling for the expensive messages.
/// (common pools are shared across messages since the single threaded implementation)
/// 
/// TODO: probably need to get completion to work again
/// TODO: hover
/// TODO: remove the constant code duplication
/// TODO: probably change any function that recursively yadayada instead of 1 arg you have 2 args 1 that is a common state for traversing and the second is specific to the function. Then all the traversing can be written once probably maybe.
/// 
/// </summary>
internal static class Lsp
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

    // =========================================================================================
    // !!!! #### DeserializeContent_CustomEditorGetNodesAdjacentToCursorFromLspRequest #### !!!!
    // =========================================================================================
    internal static CustomEditorGetNodesAdjacentToCursorFromLspRequestResponse _customEditorGetNodesAdjacentToCursorFromLspRequestResponse = new();
    internal static CustomEditorGetNodesAdjacentToCursorFromLspRequestResponseResult _customEditorGetNodesAdjacentToCursorFromLspRequestResponseResult = new();
    internal static CustomEditorGetNodesAdjacentToCursorFromLspRequestResponseResultNode _before = new();
    internal static CustomEditorGetNodesAdjacentToCursorFromLspRequestResponseResultNode _among = new();
    internal static CustomEditorGetNodesAdjacentToCursorFromLspRequestResponseResultNode _after = new();


    /// <summary>
    /// Contains both IsScopeProvider==true nodes and IsScopeProvider==false nodes BECAUSE it contains the definition node itself as the first thing that gets pushed. Other than that first pushed node everything is IsScopeProvider==true.
    /// </summary>
    internal static Stack<SyntaxNode> _inclusiveFinalNodeToConsiderAtEachDepth = new();
    internal static Stack<(SyntaxNode ScopeProvider, int ChildIndexOfScopeProviderAmongItsSiblings)> _returnStack = new();

    public static string DeserializeContent_CustomFullFileLexRequest(string filePath)
    {
        var found = _javaScriptWorkspace.OpenedSourceFileAbsolutePathToInMemoryContentMap.TryGetValue(filePath, out var javaScriptDocument);
        if (!found || javaScriptDocument is null)
        {
            return null;
        }

        var javascriptParser = new JavaScriptParser(javaScriptDocument, _javaScriptWorkspace);
        javaScriptDocument.CompilationUnit = javascriptParser.Parse();

        var base64String = Convert.ToBase64String(javascriptParser.SyntaxHighlightByteList);

        return base64String;
    }

    /// <summary>
    /// this isn't gonna work cuz the line endings are changing anyways maybe that's the issue I'm seeing I thought it was the BOM changing
    /// </summary>
    public static void DeserializeContent_CustomSaveFile()
    {
        /*if (_javaScriptWorkspace.MostRecentlyOpenedFilePath is null)
            return;

        var found = _javaScriptWorkspace.OpenedSourceFileAbsolutePathToInMemoryContentMap.TryGetValue(_javaScriptWorkspace.MostRecentlyOpenedFilePath, out var javaScriptDocument);
        if (!found || javaScriptDocument is null)
        {
            return;
        }

        // Choose the exact match for what the user originally opened
        Encoding saveEncoding = javaScriptDocument.HadBomMark ? Program.Utf8WithBom : Program.Utf8NoBom;

        string filePath = _javaScriptWorkspace.MostRecentlyOpenedFilePath;

        // 1. Open the file stream manually
        using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            // 2. Explicitly write the BOM if the encoding dictates it
            if (javaScriptDocument.HadBomMark)
            {
                ReadOnlySpan<byte> preamble = saveEncoding.GetPreamble();
                fs.Write(preamble);
            }

            // 3. Write your zero-allocation character span using a StreamWriter
            // Crucial: Pass the encoding so it knows how to transform the chars, 
            // but the stream writer will not duplicate the BOM because we are already mid-stream.
            using (var writer = new StreamWriter(fs, saveEncoding, bufferSize: 4096, leaveOpen: true))
            {
                writer.Write(new ReadOnlySpan<byte>(javaScriptDocument.Chars, 0, javaScriptDocument.Count));
            }
        }



        //File.WriteAllText(_javaScriptWorkspace.MostRecentlyOpenedFilePath, CollectionsMarshal.AsSpan(javaScriptDocument.Chars), saveEncoding);

        //File.WriteAllText(_javaScriptWorkspace.MostRecentlyOpenedFilePath, CollectionsMarshal.AsSpan(javaScriptDocument.Chars));*/
    }

    public static CustomEditorGetNodesAdjacentToCursorFromLspRequestResponseResult? DeserializeContent_CustomEditorGetNodesAdjacentToCursorFromLspRequest(int positionIndex)
    {
        if (_javaScriptWorkspace.MostRecentlyOpenedFilePath is null)
            return null;

        var found = _javaScriptWorkspace.OpenedSourceFileAbsolutePathToInMemoryContentMap.TryGetValue(_javaScriptWorkspace.MostRecentlyOpenedFilePath, out var javaScriptDocument);
        if (!found || javaScriptDocument is null)
        {
            return null;
        }

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
        
        SyntaxNodeWithPosition bbbefore = default;
        SyntaxNodeWithPosition aaamong = default;
        SyntaxNodeWithPosition aaafter = default;
        var args = new RecursiveSearch_FindNodesAdjacentToTheEdit_Args(javaScriptDocument, positionIndex, _inclusiveFinalNodeToConsiderAtEachDepth, _returnStack, ref bbbefore, ref aaamong, ref aaafter);

        SyntaxHelper.RecursiveSearch_FindNodesAdjacentToTheEdit(ref args);

        args.InclusiveFinalNodeToConsiderAtEachDepth.Clear();
        args.ReturnStack.Clear();

        _before.SyntaxKindString = SyntaxHelper.GetSyntaxKindString(args.Before.Node?.SyntaxKind ?? SyntaxKind.None);
        _before.Identifier = SyntaxHelper.GetIdentifierText(args.Before, args.Chars);
        _before.PositionIndex = args.Before.PositionIndex;

        _among.SyntaxKindString = SyntaxHelper.GetSyntaxKindString(args.Among.Node?.SyntaxKind ?? SyntaxKind.None);
        _among.Identifier = SyntaxHelper.GetIdentifierText(args.Among, args.Chars);
        _among.PositionIndex = args.Among.PositionIndex;
        
        _after.SyntaxKindString = SyntaxHelper.GetSyntaxKindString(args.After.Node?.SyntaxKind ?? SyntaxKind.None);
        _after.Identifier = SyntaxHelper.GetIdentifierText(args.After, args.Chars);
        _after.PositionIndex = args.After.PositionIndex;

        //_customEditorGetNodesAdjacentToCursorFromLspRequestResponse.id = customEditorGetNodesAdjacentToCursorFromLspRequest.id;
        _customEditorGetNodesAdjacentToCursorFromLspRequestResponseResult.Before = _before;
        _customEditorGetNodesAdjacentToCursorFromLspRequestResponseResult.Among = _among;
        _customEditorGetNodesAdjacentToCursorFromLspRequestResponseResult.After = _after;
        _customEditorGetNodesAdjacentToCursorFromLspRequestResponse.result = _customEditorGetNodesAdjacentToCursorFromLspRequestResponseResult;

        return _customEditorGetNodesAdjacentToCursorFromLspRequestResponse.result;
    }

    public static int DeserializeContent_Definition(int positionIndex)
    {
        if (_javaScriptWorkspace.MostRecentlyOpenedFilePath is null)
            return -1;

        var found = _javaScriptWorkspace.OpenedSourceFileAbsolutePathToInMemoryContentMap.TryGetValue(_javaScriptWorkspace.MostRecentlyOpenedFilePath, out var javaScriptDocument);
        if (!found || javaScriptDocument is null)
        {
            return -1;
        }

        var args = new RecursiveSearch_Args(javaScriptDocument, positionIndex, _inclusiveFinalNodeToConsiderAtEachDepth, _returnStack);

        SyntaxHelper.RecursiveSearch(ref args);
        SyntaxNode? result_node = args.TargetNode;

        args.InclusiveFinalNodeToConsiderAtEachDepth.Clear();
        args.ReturnStack.Clear();

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
                                args.InclusiveFinalNodeToConsiderAtEachDepth.Push(declarationNode);
                                if (targetScopeProvider != javaScriptDocument.CompilationUnit.GlobalNode)
                                {
                                    args.InclusiveFinalNodeToConsiderAtEachDepth.Push(targetScopeProvider);
                                }
                            }
                            else
                            {
                                if (targetScopeProvider != javaScriptDocument.CompilationUnit.GlobalNode)
                                {
                                    args.InclusiveFinalNodeToConsiderAtEachDepth.Push(targetScopeProvider);
                                }
                            }
                            targetScopeProvider = targetScopeProvider.GetScope().ParentScopeProvider;
                        }

                        targetScopeProvider = javaScriptDocument.CompilationUnit.GlobalNode;
                        var finalNodeToConsider = args.InclusiveFinalNodeToConsiderAtEachDepth.Pop();

                        args.CurrentIndexPosition = 0;

                        // TODO: This should probably start as false (it was required to start as false in a similar but not exactly the same scenario)
                        var isNaturalHandOff = true;

                        var isSidePath = false;

                        // nvm I forgot to republish the app so I was super confused when the test passed but I tried it in the editor and it failed
                        // and just I'm battling despair so seeing it fail hit deep
                        // ^ it doesn't work in 99% of cases I'm just saying it DOES work in the unit test and that specific unit test works in the editor.

                        var i = 0;
                        while (targetScopeProvider is not null)
                        {
                            ref var scope = ref targetScopeProvider.GetScope();
                            for (; i < scope.BodyList.Count; i++)
                            {
                                var childNode = scope.BodyList[i];
                                if (args.CurrentIndexPosition < javaScriptDocument.Count)
                                {
                                    var distance = childNode.DistanceFromPreviousNode;
                                    if (args.CurrentIndexPosition + distance >= javaScriptDocument.Count)
                                        distance = javaScriptDocument.Count - args.CurrentIndexPosition;

                                    for (var j = 0; j < distance; j++)
                                    {
                                        args.CurrentIndexPosition++;
                                    }
                                }
                                if (childNode.IsScopeProvider)
                                {
                                    if (childNode != finalNodeToConsider)
                                    {
                                        args.ReturnStack.Push((targetScopeProvider, i));
                                    }
                                    else
                                    {
                                        if (!isSidePath) // TODO: determine side path!!!!!
                                        {
                                            finalNodeToConsider = args.InclusiveFinalNodeToConsiderAtEachDepth.Pop();
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
                                        args.ReturnStack.Clear(); // TODO: Don't do this just anxiety I have no reason for this line to exist?
                                        targetScopeProvider = null;
                                        break;
                                    }
                                }
                            }
                            if (isNaturalHandOff)
                            {
                                isNaturalHandOff = false;
                                i = 0;
                            }
                            else
                            {
                                if (args.ReturnStack.Count > 0)
                                {
                                    var returnEntry = args.ReturnStack.Pop();
                                    targetScopeProvider = returnEntry.ScopeProvider;
                                    i = returnEntry.ChildIndexOfScopeProviderAmongItsSiblings + 1;
                                }
                                else
                                {
                                    targetScopeProvider = null;
                                }
                            }
                        }

                        args.InclusiveFinalNodeToConsiderAtEachDepth.Clear();
                        args.ReturnStack.Clear();

                        return args.CurrentIndexPosition;
                    }
                }
            }
        }

        return -1;
    }

    public static Message DeserializeContent_CompletionSlice(TextDocumentCompletionRequest_slice? completionRequestSlice)
    {
        if (completionRequestSlice is null || completionRequestSlice.@params.textDocument.uri is null || _completionItemArray is null)
            return null;

        var found = _javaScriptWorkspace.OpenedSourceFileAbsolutePathToInMemoryContentMap.TryGetValue(completionRequestSlice.@params.textDocument.uri, out var javaScriptDocument);
        if (!found || javaScriptDocument is null)
        {
            return null;
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

        return null;
    }

    public static Message DeserializeContent_Completion(TextDocumentCompletionRequest? completionRequest)
    {
        if (completionRequest is null || completionRequest.@params.textDocument.uri is null)
            return null;

        var found = _javaScriptWorkspace.OpenedSourceFileAbsolutePathToInMemoryContentMap.TryGetValue(completionRequest.@params.textDocument.uri, out var javaScriptDocument);
        if (!found || javaScriptDocument is null)
        {
            return null;
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

        return null;
    }

    public static Message DeserializeContent_Hover(TextDocumentHoverRequest? hoverRequest)
    {
        return null;
        /*if (hoverRequest is null || hoverRequest.@params.textDocument.uri is null)
            return null;

        hoverRequest.@params.textDocument.uri = EnsureLocalPath(hoverRequest.@params.textDocument.uri);

        var found = _javaScriptWorkspace.OpenedSourceFileAbsolutePathToInMemoryContentMap.TryGetValue(hoverRequest.@params.textDocument.uri, out var javaScriptDocument);
        if (!found || javaScriptDocument is null)
        {
            return null;
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

        // Contains both IsScopeProvider==true nodes and IsScopeProvider==false nodes
        // BECAUSE it contains the definition node itself as the first thing that gets pushed.
        // Other than that first pushed node everything is IsScopeProvider==true.
        Stack<SyntaxNode> inclusiveFinalNodeToConsiderAtEachDepth = new();
        Stack<(SyntaxNode ScopeProvider, int ChildIndexOfScopeProviderAmongItsSiblings)> returnStack = new();

        result_node = SyntaxHelper.RecursiveSearch(javaScriptDocument.Chars, javaScriptDocument.CompilationUnit.GlobalNode, hoverRequest.@params.position.line, hoverRequest.@params.position.character, ref totalChecks, ref currentIndexPosition, ref currentIndexLine, ref currentIndexCharacter, inclusiveFinalNodeToConsiderAtEachDepth, returnStack);

        inclusiveFinalNodeToConsiderAtEachDepth.Clear();
        returnStack.Clear();

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

        return null;*/
    }

    public static Message DeserializeContent_DocumentSymbol(TextDocumentDocumentSymbolRequest? symbolRequest)
    {
        if (symbolRequest is null || symbolRequest.@params.textDocument.uri is null)
            return null;

        var found = _javaScriptWorkspace.OpenedSourceFileAbsolutePathToInMemoryContentMap.TryGetValue(symbolRequest.@params.textDocument.uri, out var javaScriptDocument);
        if (!found || javaScriptDocument is null)
        {
            return null;
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

        return null;
    }

    public static Message DeserializeContent_DidChange(DidChangeTextDocumentNotification? didChangeTextDocumentNotification)
    {
        if (didChangeTextDocumentNotification is null || didChangeTextDocumentNotification.@params.textDocument.uri is null)
            return null;

        if (didChangeTextDocumentNotification.@params.contentChanges is null)
        {
            //Console.Out.WriteLine($"\n====didChangeTextDocumentNotification.@params.contentChanges is null====\n");
            return null;
        }

        if (didChangeTextDocumentNotification.@params.contentChanges.Length == 0)
        {
            //Console.Out.WriteLine($"\n====didChangeTextDocumentNotification.@params.contentChanges.Length == 0====\n");
            return null;
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
            return null;
        }

        _javaScriptWorkspace.DidChangeTextDocumentNotification(didChangeTextDocumentNotification.@params.textDocument.uri, didChangeTextDocumentNotification.@params.contentChanges);
        return null;
    }

    public static Message DeserializeContent_DidClose(DidCloseTextDocumentNotification? didCloseTextDocumentNotification)
    {
        if (didCloseTextDocumentNotification is null || didCloseTextDocumentNotification.@params.textDocument.uri is null)
            return null;

        if (_javaScriptWorkspace is null)
        {
            //Console.Out.WriteLine($"\n====_javaScriptWorkspace is null====\n");
            return null;
        }

        _javaScriptWorkspace.DidCloseTextDocumentNotification(didCloseTextDocumentNotification.@params.textDocument.uri);
        return null;
    }

    public static object DeserializeContent_DidOpen(string filePath)
    {
        _javaScriptWorkspace.MostRecentlyOpenedFilePath = filePath;
        var found = _javaScriptWorkspace.OpenedSourceFileAbsolutePathToInMemoryContentMap.TryGetValue(filePath, out var doc);
        if (found)
        {
            return null;
        }

        bool hadBomMark;
        int length;
        int capacity;
        byte[] buffer;
        // 0 => None
        // 1 => CR
        // 2 => LF
        // 3 => CRLF
        byte lineEndingKind = 2;

        // Constants for newline parsing
        byte CR = 13; // '\r'
        byte LF = 10; // '\n'

        byte[] fileBytes = File.ReadAllBytes(filePath);
        ReadOnlySpan<byte> fileSpan = fileBytes;

        int dataStartIndex = 0;

        // 1. Detect and strip the BOM
        if (fileSpan.Length >= 3 &&
            fileSpan[0] == 0xEF &&
            fileSpan[1] == 0xBB &&
            fileSpan[2] == 0xBF)
        {
            hadBomMark = true;
            dataStartIndex = 3;
        }
        else
        {
            hadBomMark = false;
        }

        // Slice past the BOM to look at pure text data
        ReadOnlySpan<byte> textSpan = fileSpan.Slice(dataStartIndex);

        // 2. Make our judgment based on the first encountered line ending
        // IndexOfAny will return the absolute first index of either byte found
        int firstLineBreakIndex = textSpan.IndexOfAny(CR, LF);

        if (firstLineBreakIndex != -1)
        {
            // If the first break character we hit is a CR, and it's immediately followed by an LF, it's CRLF.
            if (textSpan[firstLineBreakIndex] == CR &&
                firstLineBreakIndex + 1 < textSpan.Length &&
                textSpan[firstLineBreakIndex + 1] == LF)
            {
                lineEndingKind = 3;
            }
            else if (textSpan[firstLineBreakIndex] == LF)
            {
                // It was either a standalone LF (\n) or an ancient standalone CR (\r).
                // In either case, we treat the default output target as standard Unix LF.
                lineEndingKind = 2;
            }
            else if (textSpan[firstLineBreakIndex] == CR)
            {
                // It was either a standalone LF (\n) or an ancient standalone CR (\r).
                // In either case, we treat the default output target as standard Unix LF.
                lineEndingKind = 1;
            }
        }
        else
        {
            // Single-line file with no line endings at all. Default to false (LF).
            lineEndingKind = 2;
        }

        // 3. Allocate a chunk of memory with extra headroom (e.g., 20% extra padding)
        capacity = Math.Max(256, (int)(textSpan.Length * 1.2));
        buffer = new byte[capacity];

        // 3. High-performance, zero-allocation normalization pass
        int targetIndex = 0;
        for (int i = 0; i < textSpan.Length; i++)
        {
            byte b = textSpan[i];

            if (b == CR)
            {
                // Look ahead: is this a Windows style CRLF pair?
                if (i + 1 < textSpan.Length && textSpan[i + 1] == LF)
                {
                    // Yes! Let the next iteration of the loop naturally handle and write the LF.
                    continue;
                }
                else
                {
                    // No, this is an isolated CR. Normalize it into an LF.
                    buffer[targetIndex++] = LF;
                    continue;
                }
            }

            buffer[targetIndex++] = b;
        }

        // 4. Update the actual active length of your string
        length = targetIndex;

        _javaScriptWorkspace.DidOpenTextDocumentNotification(filePath, buffer, capacity, length, hadBomMark, lineEndingKind);
        return null;
    }

    public static object DeserializeContent_Initialize(InitializeRequest? initializeRequest)
    {
        if (initializeRequest is null)
            return null;

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

    public static Message DeserializeContent_CustomEditorVerifyLspTextAgainstEditorTextRequest(CustomEditorVerifyLspTextAgainstEditorTextRequest? customEditorVerifyLspTextAgainstEditorTextRequest)
    {
        if (customEditorVerifyLspTextAgainstEditorTextRequest is null || customEditorVerifyLspTextAgainstEditorTextRequest.@params.textDocument.uri is null)
            return null;

        var found = _javaScriptWorkspace.OpenedSourceFileAbsolutePathToInMemoryContentMap.TryGetValue(customEditorVerifyLspTextAgainstEditorTextRequest.@params.textDocument.uri, out var javaScriptDocument);
        if (!found || javaScriptDocument is null)
        {
            return null;
        }

        var customEditorVerifyLspTextAgainstEditorTextResponse = new CustomEditorVerifyLspTextAgainstEditorTextResponse(customEditorVerifyLspTextAgainstEditorTextRequest.id, Encoding.UTF8.GetString(javaScriptDocument.Chars, 0, javaScriptDocument.Count));
        Console.Out.WriteLine(Program.MAIN_encodeMessageObject(customEditorVerifyLspTextAgainstEditorTextResponse));

        return null;
    }
}
