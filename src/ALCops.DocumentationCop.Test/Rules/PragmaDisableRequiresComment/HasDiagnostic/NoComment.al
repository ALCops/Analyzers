codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    begin
        [|#pragma warning disable AA0001|]
        Message('Hello');
        #pragma warning restore AA0001
    end;
}
