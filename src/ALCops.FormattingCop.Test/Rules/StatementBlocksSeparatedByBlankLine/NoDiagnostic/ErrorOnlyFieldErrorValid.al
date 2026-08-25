codeunit 50126 MyFieldErrorOnlyValidCodeunit
{
    procedure FieldErrorAfterStatement(var FirstTable: Record "My Test Table")
    begin
        Message('Something failed');

        [|FirstTable|].FieldError("Dummy No. 1");
    end;
}

table 50127 "My Test Table"
{
    fields
    {
        field(1; "Dummy No. 1"; Text[100])
        {
        }
    }
}
