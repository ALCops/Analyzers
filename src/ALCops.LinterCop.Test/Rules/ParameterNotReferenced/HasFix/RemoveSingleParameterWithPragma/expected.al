codeunit 50100 RemoveSinglePragmaParameter
{
    procedure RemoveParameter(
        MyInteger: Integer;
        MyDate: Date)
    begin
        MyInteger := 1;
        MyDate := Today();
    end;
}