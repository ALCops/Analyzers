codeunit 50100 OverridingImplementation implements DefaultMethodContract
{
    procedure RequiredMethod(Input: Text): Text
    begin
        exit(Input);
    end;

    procedure OwnProcedure([|Input: Text|]): Text
    begin
        exit('own value');
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
