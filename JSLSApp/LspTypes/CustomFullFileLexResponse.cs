namespace JSLSApp.LspTypes;

public class CustomFullFileLexResponse
{
    public CustomFullFileLexResponse(int id, int[] pseudoThreeFieldEntryList)
    {
        this.id = id;
        this.result = pseudoThreeFieldEntryList;
    }

    public int id { get; }

    /// <summary>
    /// int[] pseudoThreeFieldEntryList
    /// 
    /// trackedSyntaxKind_offset = 0;
    /// start_offset = 1;
    /// length_offset = 2;
    /// 
    /// if (result.Count % 3 != 0) {
    ///     throw new Exception("A mismatch of fields");
    /// }
    /// 
    /// struct TrackedSyntax
    /// {
    ///     trackedSyntaxKind,
    ///     start,
    ///     length
    /// }
    /// 
    /// List of TrackedSyntax trackedSyntaxList = new List of Tracked Syntax().
    /// 
    /// for (int i = 0; i < result.Count; i++) {
    ///     var trackedSyntaxKind = result[(i * 3) + 0];
    ///     var start = result[(i * 3) + 1];
    ///     var length = result[(i * 3) + 2];
    ///     
    ///     trackedSyntaxList.Add(new(trackedSyntaxKind, start, length));
    /// }
    /// 
    /// The way I store tabs in the editor client is going to be a pain.
    /// I'm going to account for that odd storage to start
    /// so I can get the proof of concept.
    /// 
    /// Likely I need to change either the client to not be weird.
    /// Or I can provide the line or something so the tab issue is isolated to the line itself then I can
    /// in the editor client convert the line based data to the position based data that accounts for my odd tab logic.
    /// </summary>
    public int[] result { get; set; }
}
