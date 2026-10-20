codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    begin
        // The comment documents the first pragma only
        #pragma warning disable AA0001
        [|#pragma warning disable AA0002|]
        Message('Hello');
        #pragma warning restore AA0002
        #pragma warning restore AA0001
    end;
}
