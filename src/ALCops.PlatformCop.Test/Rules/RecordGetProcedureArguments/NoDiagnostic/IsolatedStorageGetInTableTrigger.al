table 50100 "My Setup"
{
    fields
    {
        field(1; "Primary Key"; Integer) { }
        field(2; "Line No."; Integer) { }
    }

    keys
    {
        key(PK; "Primary Key", "Line No.") { Clustered = true; }
    }

    trigger OnInsert()
    var
        Password: Text;
    begin
        if IsolatedStorage.Contains('MyKey', DataScope::Module) then
            [|IsolatedStorage.Get('MyKey', DataScope::Module, Password)|];

        [|IsolatedStorage.Get('MyKey', Password)|];

        IsolatedStorage.Delete('MyKey', DataScope::Module);
    end;
}
