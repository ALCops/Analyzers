codeunit 50100 MyCodeunit
{
    procedure [|GetValue|](Selector: Enum TestEnum) Result: Integer
    begin
        case Selector of
            Selector::First:
                Result := 1;
            Selector::Second:
                Result := 2;
        end;
    end;
}

enum 50100 TestEnum
{
    Extensible = true;

    value(0; First)
    {
    }
    value(1; Second)
    {
    }
}

enumextension 50101 TestEnumExtension extends TestEnum
{
    value(2; Third)
    {
    }
}