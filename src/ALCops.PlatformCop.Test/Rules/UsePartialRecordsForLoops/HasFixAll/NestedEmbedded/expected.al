codeunit 50100 MyCodeunit
{
    procedure MyProcedure(Kind: Integer; Found: Boolean) Result: Decimal
    var
        MyTable: Record MyTable;
        OtherTable: Record MyTable;
        ThirdTable: Record MyTable;
    begin
        case Kind of
            1:
                begin
                    MyTable.SetLoadFields("No.");
                    if MyTable.FindSet() then
                        repeat
                            if Found then begin
                                OtherTable.SetLoadFields(Amount);
                                if OtherTable.FindSet() then
                                    repeat
                                        Result += OtherTable.Amount;
                                    until OtherTable.Next() = 0;
                            end;
                        until MyTable.Next() = 0;
                end;
            2:
                if Found then
                    Result := 0
                else begin
                    ThirdTable.SetLoadFields(Amount);
                    if ThirdTable.FindSet() then
                        repeat
                            Result += ThirdTable.Amount;
                        until ThirdTable.Next() = 0;
                end;
        end;
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Amount; Decimal) { }
    }

    keys
    {
        key(PK; "No.") { }
    }
}
