codeunit 50100 MyCodeunit
{
    procedure MyProcedure(MyValue: Integer)
    var
        MyTable: Record MyTable;
    begin
        MyTable.SetFilter(MyField, StrSubstNo([|'*%1*'|], MyValue));
        MyTable.SetFilter(MyField, StrSubstNo([|'@*%1*'|], MyValue));
        MyTable.SetFilter(MyField, StrSubstNo([|'?%1*'|], MyValue));
    end;
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }