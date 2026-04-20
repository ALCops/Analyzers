table 50100 MyTable
{
    fields
    {
        field(1; Name; Text[100]) { }
    }

    [IntegrationEvent(false, false)]
    local procedure OnBeforeMyProcedure(var MyTableParam: Record MyTable; var IsHandled: Boolean)
    begin
    end;

    procedure MyProcedure()
    var
        IsHandled: Boolean;
    begin
        OnBeforeMyProcedure([|Rec|], IsHandled);
    end;
}
