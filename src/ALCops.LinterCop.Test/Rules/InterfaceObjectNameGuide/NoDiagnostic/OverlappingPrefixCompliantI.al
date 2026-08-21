interface [|"ICustomerFactory"|]
{
    // Coincidental overlap: the mandatory prefix "ICU" is a case-insensitive prefix of
    // "ICustomerFactory", but the fast path (name starts with 'I', no whitespace after)
    // returns compliant BEFORE any affix lookup. The name is therefore never stripped to
    // "stomerFactory" (whose first letter is not 'I'), so no diagnostic is raised.
    procedure DoSomething();
}
