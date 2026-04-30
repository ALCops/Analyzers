codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        SimpleErr: Label 'An unexpected error occurred.';
    begin
        Error([|SimpleErr|]);
    end;
}
