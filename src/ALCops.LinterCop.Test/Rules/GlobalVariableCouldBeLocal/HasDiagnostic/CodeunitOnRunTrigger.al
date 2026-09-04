codeunit 50100 CodeunitOnRunTrigger
{
    trigger OnRun()
    begin
        MyValue := 10;
        Message('%1', MyValue);
    end;

    var
        [|MyValue|]: Integer;
}
