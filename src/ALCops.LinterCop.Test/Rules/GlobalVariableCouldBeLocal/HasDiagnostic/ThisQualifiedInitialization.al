codeunit 50100 ThisQualifiedInitialization
{
    var
        [|MyValue|]: Integer;

    local procedure ShowValue()
    begin
        this.MyValue := 42;
        Message('%1', this.MyValue);
    end;
}
