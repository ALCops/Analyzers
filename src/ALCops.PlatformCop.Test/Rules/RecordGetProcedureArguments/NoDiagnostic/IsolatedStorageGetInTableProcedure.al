table 50100 "My Setup"
{
    fields
    {
        field(1; "Primary Key"; Integer) { }
    }

    keys
    {
        key(PK; "Primary Key") { Clustered = true; }
    }

    procedure GetPassword(): Text
    var
        Password: Text;
    begin
        if IsolatedStorage.Contains('MyKey', DataScope::Module) then
            [|IsolatedStorage.Get('MyKey', DataScope::Module, Password)|];

        [|IsolatedStorage.Get('MyKey', Password)|];

        IsolatedStorage.Delete('MyKey', DataScope::Module);
        exit(Password);
    end;
}
