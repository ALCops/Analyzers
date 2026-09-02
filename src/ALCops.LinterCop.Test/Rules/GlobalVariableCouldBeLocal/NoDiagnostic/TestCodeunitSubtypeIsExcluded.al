codeunit 50100 TestCodeunitSubtypeIsExcluded
{
    Subtype = Test;

    var
        [|MyValue|]: Integer;

    [Test]
    procedure ShowValue()
    begin
        MyValue := 42;
        Message('%1', MyValue);
    end;
}
