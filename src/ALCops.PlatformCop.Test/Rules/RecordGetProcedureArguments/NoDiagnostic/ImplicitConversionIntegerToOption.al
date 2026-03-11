codeunit 50100 MyCodeunit
{
    procedure GetObjectCaption(ObjectType: Integer; ObjectID: Integer)
    var
        AllObjWithCaption: Record AllObjWithCaption;
    begin
        [|AllObjWithCaption.Get(ObjectType, ObjectID)|];
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
