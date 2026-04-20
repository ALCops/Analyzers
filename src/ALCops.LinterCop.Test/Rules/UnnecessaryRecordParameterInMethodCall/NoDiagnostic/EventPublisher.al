table 50100 MyTable
{
    fields
    {
        field(1; Name; Text[100]) { }
    }

    [IntegrationEvent(false, false)]
    local procedure OnBeforeDoSth(var MyTableParam: Record MyTable; var IsHandled: Boolean)
    begin
    end;

    procedure DoSth()
    var
        IsHandled: Boolean;
    begin
        OnBeforeDoSth([|Rec|], IsHandled);
    end;
}
