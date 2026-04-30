codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        UnexpectedErr: Label 'Unexpected error for %1.', Comment = '%1=Record ID';
    begin
        Error([|UnexpectedErr|]);
    end;
}
