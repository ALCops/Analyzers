codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    begin
        // Legacy API requires this call
        #pragma warning disable AA0001
        Message('Hello');
        [|#pragma warning restore AA0001|]
    end;
}
