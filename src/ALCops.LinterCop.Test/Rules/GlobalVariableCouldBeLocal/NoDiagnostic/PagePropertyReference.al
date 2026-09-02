page 50100 PagePropertyReference
{
    PageType = Card;

    layout
    {
        area(Content)
        {
            field(ValueControl; MyValue)
            {
                ApplicationArea = All;
            }
        }
    }

    var
        [|MyValue|]: Integer;

    trigger OnOpenPage()
    begin
        MyValue := 10;
        Message('%1', MyValue);
    end;
}
