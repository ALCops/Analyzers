enum 50100 MyEnum implements MyInterface
{
    value(0; [|Default|])
    {
        Caption = 'Default';
        Implementation = MyInterface = MyCodeunit;
    }
}

interface MyInterface { }
codeunit 50100 MyCodeunit implements MyInterface { }