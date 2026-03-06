codeunit 50100 MyCodeunit
{
    procedure MyProcedure(ObjectID: Integer)
    var
        AllObjWithCaption: Record AllObjWithCaption;
    begin
        [|AllObjWithCaption.Get(AllObjWithCaption."Object Type"::Codeunit, ObjectID)|];
    end;
}

table 50100 AllObjWithCaption
{
    fields
    {
        field(1; "Object Type"; Option)
        {
            OptionMembers = "TableData","Table",,"Report",,"Codeunit","XMLport","MenuSuite","Page","Query","System","FieldNumber",,,"PageExtension","TableExtension","Enum","EnumExtension","Profile","ProfileExtension","PermissionSet","PermissionSetExtension","ReportExtension";
        }
        field(3; "Object ID"; Integer)
        {
        }
    }
    keys
    {
        key(pk; "Object Type", "Object ID")
        {
        }
    }
}
