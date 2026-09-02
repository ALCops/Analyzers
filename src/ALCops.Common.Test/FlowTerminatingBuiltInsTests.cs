using System.Reflection;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.Common.Test;

public class FlowTerminatingBuiltInsTests
{
    [TestCase("Error", "Dialog", true)]
    [TestCase("FieldError", "Table", true)]
    [TestCase("FieldError", "FieldRef", true)]
    [TestCase("Error", "Table", false)]
    [TestCase("FieldError", "Dialog", false)]
    [TestCase("FieldError", "Codeunit", false)]
    public void CleanBindRequiresExactBuiltInClass(
        string methodName,
        string containingClassName,
        bool expected)
    {
        var containingClass = SymbolProxy.Create<IClassTypeSymbol>(
            (nameof(ISymbol.Name), containingClassName));
        var method = SymbolProxy.Create<IMethodSymbol>(
            (nameof(ISymbol.Name), methodName),
            (nameof(ISymbol.ContainingSymbol), containingClass),
            (nameof(IMethodSymbol.MethodKind), EnumProvider.MethodKind.BuiltInMethod));
        var invocation = SymbolProxy.Create<IInvocationExpression>(
            (nameof(IInvocationExpression.TargetMethod), method),
            (nameof(IOperation.IsInvalid), false));

        Assert.That(
            FlowTerminatingBuiltIns.IsFlowTerminatingCall(invocation),
            Is.EqualTo(expected));
    }

    [Test]
    public void InvalidBindRequiresExactReceiverKind()
    {
        Assert.Multiple(() =>
        {
            Assert.That(IsInvalidBindTerminating("Error", EnumProvider.NavTypeKind.Dialog), Is.True);
            Assert.That(IsInvalidBindTerminating("FieldError", EnumProvider.NavTypeKind.Record), Is.True);
            Assert.That(IsInvalidBindTerminating("FieldError", EnumProvider.NavTypeKind.FieldRef), Is.True);
            Assert.That(IsInvalidBindTerminating("Error", EnumProvider.NavTypeKind.Record), Is.False);
            Assert.That(IsInvalidBindTerminating("FieldError", EnumProvider.NavTypeKind.Dialog), Is.False);
        });
    }

    private static bool IsInvalidBindTerminating(string methodName, NavTypeKind receiverKind)
    {
        var containingType = SymbolProxy.Create<ITypeSymbol>(
            (nameof(ITypeSymbol.NavTypeKind), receiverKind));
        var method = SymbolProxy.Create<IMethodSymbol>(
            (nameof(ISymbol.Name), methodName),
            (nameof(ISymbol.ContainingSymbol), containingType));
        var invocation = SymbolProxy.Create<IInvocationExpression>(
            (nameof(IInvocationExpression.TargetMethod), method),
            (nameof(IOperation.IsInvalid), true));

        return FlowTerminatingBuiltIns.IsFlowTerminatingCall(invocation);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1852:Seal internal types",
        Justification = "DispatchProxy generates a subtype at runtime.")]
    private class SymbolProxy : DispatchProxy
    {
        private Dictionary<string, object?> _propertyValues =
            new Dictionary<string, object?>();

        public static T Create<T>(params (string PropertyName, object? Value)[] propertyValues)
            where T : class
        {
            var proxy = DispatchProxy.Create<T, SymbolProxy>();
            ((SymbolProxy)(object)proxy)._propertyValues = propertyValues.ToDictionary(
                static property => property.PropertyName,
                static property => property.Value);

            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.IsSpecialName == true &&
                targetMethod.Name.StartsWith("get_", StringComparison.Ordinal) &&
                _propertyValues.TryGetValue(targetMethod.Name[4..], out var value))
            {
                return value;
            }

            return targetMethod?.ReturnType.IsValueType == true
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
        }
    }
}