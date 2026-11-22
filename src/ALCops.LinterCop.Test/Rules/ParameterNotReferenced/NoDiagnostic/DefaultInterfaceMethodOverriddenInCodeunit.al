codeunit 50100 OverridingImplementation implements DefaultMethodContract
{
    procedure RequiredMethod(Input: Text): Text
    begin
        exit(Input);
    end;

    procedure OptionalMethod([|Input: Text|]): Text
    begin
        exit('override value');
    end;
}

interface DefaultMethodContract
{
    procedure RequiredMethod(Input: Text): Text;

    procedure OptionalMethod(Input: Text): Text
    begin
        exit('default value');
    end;
}
