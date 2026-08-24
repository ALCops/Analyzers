codeunit 50100 RemoveSinglePragmaParameter
{
    procedure RemoveParameter(
        MyInteger: Integer;
        #pragma warning disable AA0036
        [|MyText: Text|];
        #pragma warning restore AA0036
        MyDate: Date)
    begin
        MyInteger := 1;
        MyDate := Today();
    end;
}