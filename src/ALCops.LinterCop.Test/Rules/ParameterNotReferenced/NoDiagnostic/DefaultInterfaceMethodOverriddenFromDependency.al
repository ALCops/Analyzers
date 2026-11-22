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
