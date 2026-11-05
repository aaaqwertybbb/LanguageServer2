using JSLSApp.LspTypes;
using JSLSApp.Syntax;
using System.Buffers;
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
    internal static Stack<(SyntaxNode ScopeProvider, int ChildIndexOfScopeProviderAmongItsSiblings, bool IsAtAnAfter)> _returnStack = new();

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

    public static (int PositionIndex, int Length) DeserializeContent_CustomRemove_ParseMessageBuffer(ReadOnlyMemory<byte> viewOverRentedBuffer)
    {
        var span = viewOverRentedBuffer.Span;

        var index = 0;
        var positionIndex = 0;
        var length = 0;

        while (index < span.Length && span[index] != Program.MessageKind_field_separator)
        {
            var b = span[index++];
            if (b >= '0' && b <= '9')
            {
                positionIndex = (positionIndex * 10) + (b - '0');
            }
            else
            {
                throw new NotImplementedException(); // TODO: bad input
            }
        }

        if (index >= span.Length)
        {
            throw new NotImplementedException();
        }

        if (span[index] == Program.MessageKind_field_separator)
        {
            index++;
        }
        else
        {
            throw new NotImplementedException();
        }

        while (index < span.Length)
        {
            var b = span[index++];
            if (b >= '0' && b <= '9')
            {
                length = (length * 10) + (b - '0');
            }
            else
            {
                throw new NotImplementedException(); // TODO: bad input
            }
        }

        return (positionIndex, length);
    }

    public static void DeserializeContent_CustomRemove(int positionIndex, int length)
    {
        if (_javaScriptWorkspace.MostRecentlyOpenedFilePath is null)
            return;

        var found = _javaScriptWorkspace.OpenedSourceFileAbsolutePathToInMemoryContentMap.TryGetValue(_javaScriptWorkspace.MostRecentlyOpenedFilePath, out var javaScriptDocument);
        if (!found || javaScriptDocument is null)
        {
            return;
        }

        // 1. Edge Case: If nothing is being removed, get out immediately
        if (length <= 0)
        {
            return;
        }

        // 2. Validate removal parameters against document state bounds
        if (positionIndex < 0 || positionIndex + length > javaScriptDocument.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(positionIndex), "The removal range exceeds current document bounds.");
        }

        // 3. Shift the trailing data to the left to overwrite the removed region
        int trailingSegmentIndex = positionIndex + length;
        int trailingSegmentLength = javaScriptDocument.Count - trailingSegmentIndex;

        if (trailingSegmentLength > 0)
        {
            // Span-based CopyTo safely manages shifting memory regions to the left (like memmove)
            Span<byte> docSpan = javaScriptDocument.Chars.AsSpan();
            ReadOnlySpan<byte> trailingSegment = docSpan.Slice(trailingSegmentIndex, trailingSegmentLength);
            Span<byte> destinationSegment = docSpan.Slice(positionIndex);

            trailingSegment.CopyTo(destinationSegment);
        }

        // 4. Update the active tracked count
        javaScriptDocument.Count -= length;

        // Optional Clean up: If you want to erase leftover byte remnants at the end of the tracked data range:
        // Array.Clear(javaScriptDocument.Chars, javaScriptDocument.Count, length);
    }


    public static (int PositionIndex, ReadOnlyMemory<byte> Bytes) DeserializeContent_CustomInsertLtr_ParseMessageBuffer(ReadOnlyMemory<byte> viewOverRentedBuffer)
    {
        var span = viewOverRentedBuffer.Span;

        var index = 0;
        var positionIndex = 0;

        while (index < span.Length && span[index] != Program.MessageKind_field_separator)
        {
            var b = span[index++];
            if (b >= '0' && b <= '9')
            {
                positionIndex = (positionIndex * 10) + (b - '0');
            }
            else
            {
                throw new NotImplementedException(); // TODO: bad input
            }
        }

        if (index >= span.Length)
        {
            throw new NotImplementedException();
        }

        if (span[index] == Program.MessageKind_field_separator)
        {
            index++;
        }
        else
        {
            throw new NotImplementedException();
        }

        return (positionIndex, viewOverRentedBuffer.Slice(index));
    }
    
    public static void DeserializeContent_CustomInsertLtr(int positionIndex, ReadOnlyMemory<byte> viewOverRentedBuffer)
    {
        if (_javaScriptWorkspace.MostRecentlyOpenedFilePath is null)
            return;

        var found = _javaScriptWorkspace.OpenedSourceFileAbsolutePathToInMemoryContentMap.TryGetValue(_javaScriptWorkspace.MostRecentlyOpenedFilePath, out var javaScriptDocument);
        if (!found || javaScriptDocument is null)
        {
            return;
        }

        // 1. Get a Span over the incoming segment
        ReadOnlySpan<byte> incomingSpan = viewOverRentedBuffer.Span;
        int length = incomingSpan.Length;

        // 2. Validate target insertion index bounds against current Document state
        // javaScriptDocument.Count matches INTS[fEDI_textByteList_count]
        if (positionIndex < 0 || positionIndex > javaScriptDocument.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(positionIndex), "Insertion index out of document bounds.");
        }

        // 3. Ensure internal capacity (matches EDI_textByteList_ensureCapacityForInsertion)
        int requiredCapacity = javaScriptDocument.Count + length;
        if (requiredCapacity > javaScriptDocument.Capacity)
        {
            // Calculate new capacity (e.g., doubling or adding 20% breathing room)
            // TODO: use the same growth algorithm as the editor i.e.: something like '// 1. Scale geometrically by 1.125x (current + current / 8)' with 1024 minimums or something.
            int newCapacity = Math.Max(256, Math.Max(javaScriptDocument.Capacity * 2, requiredCapacity));

            // Allocate a new internal buffer
            byte[] newBuffer = new byte[newCapacity];

            // Copy the pre-split segment over to the new array
            if (positionIndex > 0)
            {
                Array.Copy(javaScriptDocument.Chars, 0, newBuffer, 0, positionIndex);
            }

            // Copy the trailing segment over to the new array, offset by the incoming text length
            if (positionIndex < javaScriptDocument.Count)
            {
                Array.Copy(javaScriptDocument.Chars, positionIndex, newBuffer, positionIndex + length, javaScriptDocument.Count - positionIndex);
            }

            // Swap the reference and update metadata
            javaScriptDocument.Chars = newBuffer;
            javaScriptDocument.Capacity = newCapacity;
        }
        else
        {
            // 4. Shift trailing bytes to the right to make room (matches EDI_textByteList_copyTo)
            if (positionIndex != javaScriptDocument.Count)
            {
                // Span-based copy handles overlapping regions beautifully without buffer corruption
                Span<byte> docSpan = javaScriptDocument.Chars.AsSpan();
                ReadOnlySpan<byte> trailingSegment = docSpan.Slice(positionIndex, javaScriptDocument.Count - positionIndex);
                Span<byte> destinationSegment = docSpan.Slice(positionIndex + length);

                trailingSegment.CopyTo(destinationSegment);
            }
        }

        // 5. Blit the incoming bytes directly into the gap (matches .set(segmentToInsert, index))
        incomingSpan.CopyTo(javaScriptDocument.Chars.AsSpan(positionIndex, length));

        // 6. Finalize tracking properties
        javaScriptDocument.Count += length;
    }

    public static void DeserializeContent_CustomSaveFile()
    {
        if (_javaScriptWorkspace.MostRecentlyOpenedFilePath is null)
            return;

        var found = _javaScriptWorkspace.OpenedSourceFileAbsolutePathToInMemoryContentMap.TryGetValue(_javaScriptWorkspace.MostRecentlyOpenedFilePath, out var javaScriptDocument);
        if (!found || javaScriptDocument is null)
        {
            return;
        }

        Encoding saveEncoding = javaScriptDocument.HadBomMark ? Program.Utf8WithBom : Program.Utf8NoBom;
        string filePath = _javaScriptWorkspace.MostRecentlyOpenedFilePath;

        using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            if (javaScriptDocument.HadBomMark)
            {
                ReadOnlySpan<byte> preamble = saveEncoding.GetPreamble();
                fs.Write(preamble);
            }

            // Access the internal normalized byte span
            ReadOnlySpan<byte> normalizedSpan = new ReadOnlySpan<byte>(javaScriptDocument.Chars, 0, javaScriptDocument.Count);
            byte lineEndingKind = javaScriptDocument.LineEndingKind; // Ensure you expose/store this from open

            // OPTION A: If the file was natively LF (2), write directly (Zero-allocation path)
            if (lineEndingKind == 2)
            {
                fs.Write(normalizedSpan);
            }
            else
            {
                // OPTION B: Denormalize back to CRLF (3) or CR (1) using a small stack/pooled buffer
                byte CR = 13;
                byte LF = 10;

                // Use a chunking buffer to keep allocations at zero
                byte[] writeBuffer = System.Buffers.ArrayPool<byte>.Shared.Rent(4096);
                int writeIdx = 0;

                try
                {
                    for (int i = 0; i < normalizedSpan.Length; i++)
                    {
                        // Flush buffer if it's nearing capacity
                        if (writeIdx >= writeBuffer.Length - 2)
                        {
                            fs.Write(writeBuffer.AsSpan(0, writeIdx));
                            writeIdx = 0;
                        }

                        byte b = normalizedSpan[i];

                        if (b == LF)
                        {
                            if (lineEndingKind == 3) // Restore CRLF
                            {
                                writeBuffer[writeIdx++] = CR;
                                writeBuffer[writeIdx++] = LF;
                            }
                            else if (lineEndingKind == 1) // Restore CR
                            {
                                writeBuffer[writeIdx++] = CR;
                            }
                            else
                            {
                                writeBuffer[writeIdx++] = LF;
                            }
                        }
                        else
                        {
                            writeBuffer[writeIdx++] = b;
                        }
                    }

                    // Write remaining bytes
                    if (writeIdx > 0)
                    {
                        fs.Write(writeBuffer.AsSpan(0, writeIdx));
                    }
                }
                finally
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(writeBuffer);
                }
            }
        }
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
                                        args.ReturnStack.Push((targetScopeProvider, i, false));
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
        byte lineEndingKind = 2; // 1 = CR, 2 = LF, 3 = CRLF
        const byte CR = 13; // '\r'
        const byte LF = 10; // '\n'
        const int paddingLength = 1024;

        // 1. Get the file size to calculate exactly what we need
        var fileInfo = new FileInfo(filePath);
        int fileSize = (int)fileInfo.Length;

        // 2. High Performance: Allocate ONE uninitialized block of heap memory 
        // This behaves exactly like Node's Buffer.allocUnsafe (O(1) allocation time)
        int totalCapacity = fileSize + paddingLength;
        byte[] fileBytes = GC.AllocateUninitializedArray<byte>(totalCapacity);

        // 3. Open a stream and read directly into the start of our array
        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            stream.ReadExactly(fileBytes, 0, fileSize);
        }

        ReadOnlySpan<byte> fileSpan = fileBytes.AsSpan(0, fileSize);
        int dataStartIndex = 0;

        // 4. Detect and strip the BOM
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

        // Look at text data boundaries
        ReadOnlySpan<byte> textSpan = fileSpan.Slice(dataStartIndex);

        // 5. Judge line ending style based on first encountered character
        int firstLineBreakIndex = textSpan.IndexOfAny(CR, LF);
        if (firstLineBreakIndex != -1)
        {
            if (textSpan[firstLineBreakIndex] == CR &&
                firstLineBreakIndex + 1 < textSpan.Length &&
                textSpan[firstLineBreakIndex + 1] == LF)
            {
                lineEndingKind = 3;
            }
            else if (textSpan[firstLineBreakIndex] == LF)
            {
                lineEndingKind = 2;
            }
            else if (textSpan[firstLineBreakIndex] == CR)
            {
                lineEndingKind = 1;
            }
        }
        else
        {
            lineEndingKind = 2;
        }

        // 6. In-place, zero-allocation normalization pass
        // Reading from 'i' and overwriting straight into 'targetIndex'
        int targetIndex = 0;
        for (int i = dataStartIndex; i < fileSize; i++)
        {
            byte b = fileBytes[i];

            if (b == CR)
            {
                if (i + 1 < fileSize && fileBytes[i + 1] == LF)
                {
                    // CRLF: skip writing CR, let the next loop handle the LF
                    continue;
                }
                else
                {
                    // Isolated CR: Normalize to LF
                    fileBytes[targetIndex++] = LF;
                    continue;
                }
            }

            fileBytes[targetIndex++] = b;
        }

        int cleanTextLength = targetIndex;
        int targetCapacity = cleanTextLength + paddingLength;

        int remainingLength = fileBytes.Length - cleanTextLength;
        // 7. Security: Zero-out only the 1024 padding bytes to clear out "ghost data"
        // This utilizes a highly optimized native CPU memset command under the hood
        fileBytes.AsSpan(cleanTextLength, remainingLength).Clear();

        _javaScriptWorkspace.DidOpenTextDocumentNotification(
            filePath,
            fileBytes,
            targetCapacity,
            cleanTextLength,
            hadBomMark,
            lineEndingKind
        );

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
