codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        Counter: Integer;
    begin
        Counter := 1; // Belongs to the assignment
        [|#pragma warning disable AA0001|]
        Message('Hello');
        #pragma warning restore AA0001
    end;
}
