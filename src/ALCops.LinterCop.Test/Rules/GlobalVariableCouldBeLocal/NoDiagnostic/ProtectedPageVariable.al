page 50100 ProtectedPageVariable
{
    PageType = Card;

    protected var
        [|MyValue|]: Integer;

    trigger OnOpenPage()
    begin
        MyValue := 10;
        Message('%1', MyValue);
    end;
}
