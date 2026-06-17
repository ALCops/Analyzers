codeunit 50100 MyCodeunit
{
    Access = Internal;

    /// <summary>
    /// Sets the call body content from a stream.
    /// </summary>
    /// <param name="ContentStream">The instream containing the call body.</param>
    [NonDebuggable]
    procedure [|MyProcedure|](ContentStream: InStream)
    begin
    end;
}
