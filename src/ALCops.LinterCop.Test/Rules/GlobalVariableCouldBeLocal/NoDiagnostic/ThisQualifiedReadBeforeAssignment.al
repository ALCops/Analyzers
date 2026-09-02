codeunit 50100 ThisQualifiedPriorRead
{
    var
        [|MyValue|]: Integer;

    local procedure ShowValue()
    begin
        Message('%1', this.MyValue);
        this.MyValue := 42;
    end;
}
