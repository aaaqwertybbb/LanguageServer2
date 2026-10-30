using JSLSApp.LspTypes;

namespace TestProject1;

public class LexerTests
{
    /// <summary>
    /// Everything in this file is extremely inaccurate I messed with the numbers and the text and the json length so it all kinda works for at least a single case.
    /// It'll hit the Parse function if it doesn't throw an exception.
    /// 
    /// got it `{"An item with the same key has already been added. Key: createElement"}`
    /// </summary>
    [Fact]
    public void ParseFile()
    {
        var program = new JSLSApp.Program();
        JSLSApp.Program.MAIN_decodeMessage(InitializeJson);
        var aaa = Format_didOpenTextDocumentNotification(FileText);
        JSLSApp.Program.MAIN_decodeMessage(aaa);
        JSLSApp.Program.MAIN_decodeMessage(customFullFileLexRequest);
        JSLSApp.Program.MAIN_decodeMessage(customEditorGetNodesAdjacentToCursorFromLspRequest);




        //JSLSApp.Program.MAIN_decodeMessage(DefinitionRequestOne); // ensure the goto definition of the original 'x'
        //JSLSApp.Program.MAIN_decodeMessage(DidChangeNotification); // paste "function Aaa() { return 'x'; }"
        //JSLSApp.Program.MAIN_decodeMessage(DefinitionRequestTwo); // ensure the original goto definition of the 'x' no longer works
        //JSLSApp.Program.MAIN_decodeMessage(DefinitionRequestThree); // try another goto definition but with the updated position for 'x'
        //JSLSApp.Program.MAIN_decodeMessage(DefinitionRequestFour); // try a final goto definition but for the 'x' that is inside of the "function Aaa() { return 'x'; }" text.
    }

    // TODO: Try to resolve unknown symbols after parsing the file rather than doing what you did last time which was to parse the entire scope and then re-visit the child scopes one by one...
    // ...whichever is the correct answer I don't know but you tried one so you should try the other one now.

    public const string InitializeJson = "Content-Length: 331\r\n\r\n{\"method\":\"initialize\",\"id\":0,\"params\":{\"processId\":18260,\"clientInfo\":{\"name\":\"TextEditor123\",\"version\":\"0.0.1\"},\"rootUri\":null,\"capabilities\":{\"workspace\":{}},\"workspaceFolders\":[{\"uri\":\"C:\\\\Users\\\\hunte\\\\Repos\\\\New folder (7)\\\\Editor2\",\"name\":\"2\"},{\"uri\":\"C:\\\\Users\\\\hunte\\\\Repos\\\\New folder (7)\\\\LanguageServer2\",\"name\":\"3\"}]}}\\n";
    // I'm gonna put "      " at the end of this string cuz the length is wrong and it won't read it
    public const string didOpenTextDocumentNotification = "Content-Length: 433\r\n\r\n{\"method\":\"textDocument/didOpen\",\"params\":{\"textDocument\":{\"uri\":\"file:///C:/Users/hunte/Repos/New folder (7)/Editor2/testFile.js\",\"languageId\":\"javascript\",\"version\":0,\"text\":\"aaa\\n        /**\\n         * @type {HTMLDivElement}\\n         * Consider the existence of such methods as 'state_cursor_setIndex' before mutating state directly\\n         */xxx/*\\n         */\\n        this.cursorElement = document.createElement('div');\"}}}\\n      ";
    public const string customFullFileLexRequest = "Content-Length: 157\r\n\r\n{\"id\":1,\"method\":\"textDocument/CustomFullFileLexRequest\",\"params\":{\"textDocument\":{\"uri\":\"file:///C:/Users/hunte/Repos/New folder (7)/Editor2/testFile.js\"}}}\\n";
    public const string customEditorGetNodesAdjacentToCursorFromLspRequest = "Content-Length: 219\r\n\r\n{\"id\":3,\"method\":\"textDocument/CustomEditorGetNodesAdjacentToCursorFromLspRequest\",\"params\":{\"textDocument\":{\"uri\":\"file:///C:/Users/hunte/Repos/New folder (7)/Editor2/testFile.js\"},\"position\":{\"line\":3,\"character\":0}}}\\n";

    public const string FileText = "function EDI_init() {\\n    const gutterPaddingLeft;\\n}\\n\\nfunction EDI_render_do() {\\n    const renderKind;\\n    renderKind;\\n}";


    public const string DefinitionRequestOne = "Content-Length: 179\r\n\r\n{\"id\":3,\"method\":\"textDocument/definition\",\"params\":{\"textDocument\":{\"uri\":\"file:///C:/Users/hunte/Repos/New folder (7)/Editor2/testFile.js\"},\"position\":{\"line\":2,\"character\":7}}}\n";
    public const string DefinitionRequestTwo = "Content-Length: 179\r\n\r\n{\"id\":3,\"method\":\"textDocument/definition\",\"params\":{\"textDocument\":{\"uri\":\"file:///C:/Users/hunte/Repos/New folder (7)/Editor2/testFile.js\"},\"position\":{\"line\":2,\"character\":7}}}\n";
    public const string DefinitionRequestThree = "Content-Length: 179\r\n\r\n{\"id\":3,\"method\":\"textDocument/definition\",\"params\":{\"textDocument\":{\"uri\":\"file:///C:/Users/hunte/Repos/New folder (7)/Editor2/testFile.js\"},\"position\":{\"line\":5,\"character\":7}}}\n";
    public const string DefinitionRequestFour = "Content-Length: 180\r\n\r\n{\"id\":3,\"method\":\"textDocument/definition\",\"params\":{\"textDocument\":{\"uri\":\"file:///C:/Users/hunte/Repos/New folder (7)/Editor2/testFile.js\"},\"position\":{\"line\":3,\"character\":11}}}\n";

    public const string DidChangeNotification = "Content-Length: 288\r\n\r\n{\"method\":\"textDocument/didChange\",\"params\":{\"textDocument\":{\"uri\":\"file:///C:/Users/hunte/Repos/New folder (7)/Editor2/testFile.js\",\"version\":1},\"contentChanges\":[{\"range\":{\"start\":{\"line\":1,\"character\":0},\"end\":{\"line\":1,\"character\":0}},\"text\":\"function Aaa() {\\\\n    return x;\\\\n}\"}]}}\\n";


    public const string CompletionRequest = "Content-Length: 179\r\n\r\n{\"id\":2,\"method\":\"textDocument/completion\",\"params\":{\"textDocument\":{\"uri\":\"file:///C:/Users/hunte/Repos/New folder (7)/Editor2/testFile.js\"},\"position\":{\"line\":0,\"character\":0}}}\\n";

    /*
    There are 4 steps:
    - [x] Get the code to even run (now that you've changed all the properties tons of build errors, and make sure nothing is bugging out)
    - [ ] Replicate the current goto definition logic with the changed properties (that are now relative positions).
        - [x] find the reference node
        - [ ] find the declaration node
            - [ ] Algorithm "first thing that comes to mind regardless of what it is"
                - [ ] Start at the definition node, and calculate the total "negative" distance required to reach the GlobalNode.
                - [ ] Absolute value of that is the position but I need the line and character too.
                    - [ ] Line and character would be the absolute value of
                        - [ ] Upon finding a new line you remember the "negative character index" that you have stored.
                        - [ ] When you're done your line number is the absolute value of the "negative line index".
            - [ ] Algorithm "second thing that comes to mind regardless of what it is"
                - [ ] Do something similar to the "first thing that comes to mind..." BUT:
                    - [ ] If the reference were to be valid you'd maybe find the declaration by just backtracking from the reference.
                - [ ] Problematic: If the declaration were in a different file you'd fail to find it.
                - [ ] Ambivalent:
                    - [ ] The distance from the declaration to the Global node compared against:
                        - [ ] The distance from the reference to the declaration.
                        - [ ] Is an irrelevant point it seems it could go in either one's favor
            - [ ] The first two algorithms are predicated upon the referenceNode having a C# field that references the declarationNode.
                - [ ] The above idea "feels" asinine (maybe it isn't). But I think it is a necessary step towards the correct answer.
                - [x] The declaration would need a reference to the parentNode (of which would be a scope provider).
                    - [ ] You could then find the declaration node among the parentNode's scope-bodyList
                    - [ ] and thus you'd have Algorithm "first thing..." available.
                        - [ ] This comment doesn't actually read completely, you need the knowledge that a scope has a 'SyntaxNode? ParentScopeProvider'
                              and that is how you'll continue all the way to the GlobalNode.
                              - [ ] No it's the scopes that are wrong they still use absolute positions
                              - [ ] Okay I know what to do
                                  - [ ] You go backwards from the definition node by just jumping to each parent scope until you are at the global node.
                                       - [ ] And you only need to determine the distances up until the scope provider that you visited at that level.
                                       - [ ] Go in reverse, track all the scopeProviders that would bring you back to where you were so you stop reading past that "sibling" at each depth.
                                       - [ ] If there's an earlier scope at the same depth you still have to descend into it so you can determine the positions of it all.
                                       - [ ] You might be able to track the open and close braces of the scopes by tracking how far the open brace is from the start of the scopeProvider node.
                                       - [ ] And then, the end is how far the close brace is away from the final child node of the scope.
                                       - [ ] But that messes you up a bit because you might start with a child count of 0 so at child count 0 it could be the distance from the open brace token
                                       - [ ] and at child count > 0 it is the distance from the final child node.
                                       - [ ] I been watching mad azeroth survivors lately.
                                       - [ ] I don't wanna touch it though myself cause it looks crazy fun -- like to the point of it being dangerous. "don't do azeroth survivors"
                                       - [ ] I'm crazy distracted rn I don't wanna do this I might just go to bed I've been watching the same "music video" on loop for more than an hour
                                       - [ ] 8:51
                                       - [ ] you need some kind of caching for the "traversal of the relative lengths" so you don't have to do the whole summing over and over you just do it when there's a cache invalidation
                                       - [ ] 8:16
                                       - [ ] get the insertion to shift
                                       - [ ] get the removals to shift
                                       - [ ] then start mapping out all the ways an edit can change the AST
                                           - [ ] that is to say: the two "shift" goals are just about shifting the existing nodes
                                               - [ ] The initial goal: the edits specifically will not modify the AST it just is about shifting the existing ones.
                                               - [ ] You gotta write out every EditKind and check that each one works.
                                               - [ ] 90% of them should solve one another
                                               - [ ] But indent more and indent less are cases you might miss so just check everything.
                                               - [ ] LspDispatcher.cs
                                                   - [ ] DeserializeContent_DidChange
                                                       - [ ] _javaScriptWorkspace.DidChangeTextDocumentNotification(...);
                                                       - [ ] JavaScriptWorkspace.cs
                                                           - [ ] DidChangeTextDocumentNotification
                                                               - [ ] doc.Chars.RemoveRange(startIndexPosition, endIndexPosition - startIndexPosition);
                                                               - [ ] doc.Chars.InsertRange(indexPosition, change.text);
                                                               - [ ] And to start you have 1 test that will successfully goto definition
                                                                   - [ ] Then it inserts text
                                                                   - [ ] It tries the original goto definition and this time it fails
                                                                   - [ ] It changes the goto definition to the location it should be at post text insertions
                                                                   - [ ] And now when trying this changed goto definition it works.
                                                                   - [ ] You could try making this for the removal of text too.
                                                                   - [ ] But I think at minimum the unit test is the fastest turn around for getting an initial case to work.
                                                                   - [ ] After that you'll probably be familiar with the code enough to where it doesn't matter how you do it just do it
                                                                   - [ ] ^ I'm thinking this to myself because I don't feel focused, I don't feel like I wanna write code right now.
                                                                   - [ ] ^ So important to the steps of solving the problem is to understand my own emotional state and what is required of me in order to will myself to complete the goal.
                                                                   - [ ] In terms of a "make daily progress that is at minimum fatiguing"
                                                                   - [ ] Whether some days you do an hour or some days you do 10 hours is irrelevant what matters is the feeling of fatigue and like you've struggled with something.
                                                                   - [ ] Some days the struggle is focus. This problem at this point I think shouldn't be too bad I just don't wanna do anything.
                                                                   - [ ] So I'm trying to figure out all my heuristics for whether I've sufficiently fatigued myself for the day.
                                                                   - [ ] I think a meaningful workload/progress is "shift the existing nodes in the easiest case where you aren't modifying any nodes or parsing the insertions or etc.. just simple shifting"
                                                                   - [ ] Ultimately the feeling of lacking focus on some random day is just your brain playing tricks on you.
                                                                   - [ ] After all we know this is common for the brain to do, as some days it has me thinking I am a good programmer
                                                                   - [ ] Some people comment about ADHD as not existing at all, that it solely is a matter of maturity
                                                                   - [ ] After all, if you don't feel well, you can from the perspective of maturity know that you "ought" to get something done regardless of your emotional state
                                                                   - [ ] BUT I disagree highly with the idea "ADHD as not existing at all".
                                                                   - [ ] If you consistently day after day feel like garbage
                                                                   - [ ] And consistently you "with maturity" force yourself to do what needs to get done
                                                                   - [ ] This constant drag of your emotional state will wear you down no matter how mature you are.
                                                                   - [ ] Some days I literally feel "euphoric"
                                                                   - [ ] Other days I feel like complete garbage (like today)
                                                                   - [ ] Some days I feel like complete garbage but after 4 hours of forcing myself to code "via maturity" I find that I either feel slightly better or enter that state of euphoria again.
                                                                   - [ ] It is this ability for me to have good days that I would say demonstrates I do NOT have ADHD
                                                                   - [ ] If someone were the alternative, I feel completely for them. That must be terrible.
                                                                   - [ ] There's also the "I don't have ADHD but I wanna slam dopaminergic substances 24/7 to increase my throughput"
                                                                   - [ ] These people tend to suck at coding. They just drown you in spaghetti code and delusions of grandeur.
                                                                   - [ ] Versus someone who has ADHD and were to be taking dopaminergic substances as prescribed, they're much more reasonable
                                                                   - [ ] I played a ton of Azeroth Survivors (in the browser) yesterday the game is awesome.
                - [ ] You need to map out the positions of every node kind cause if you miss even just 1 it completely falls apart. (and currently you're missing the majority of them).
            - [ ] Algorithm "???"
    - [ ] When you insert text you update the relative positions of the "node on the left and right of where you are inserting".
    - [ ] When you insert text you parse the text to create a new node and modify the AST with the node inserted at the correct position.
    */

    public string Format_didOpenTextDocumentNotification(string fileText)
    {
        return "Content-Length: " + (181 + fileText.Length) + "\r\n\r\n{\"method\":\"textDocument/didOpen\",\"params\":{\"textDocument\":{\"uri\":\"file:///C:/Users/hunte/Repos/New folder (7)/Editor2/testFile.js\",\"languageId\":\"javascript\",\"version\":0,\"text\":\"" + fileText + "\"}}}\\n      ";
    }
}
