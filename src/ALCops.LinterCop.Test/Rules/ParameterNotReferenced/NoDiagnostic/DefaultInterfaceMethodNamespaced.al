namespace MyPublisher.MyExtension.MyAppDomain;

interface DefaultMethodContract
{
    procedure RequiredMethod(Input: Text): Text;

    procedure OptionalMethod([|Input: Text|]): Text
    begin
        exit('default value');
    end;
}
