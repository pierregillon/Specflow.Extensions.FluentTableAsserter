using Specflow.Extensions.FluentTableAsserter.CollectionAsserters;

namespace Specflow.Extensions.FluentTableAsserter.Properties;

public interface IPropertyDefinition<in T>
{
    public string PropertyName { get; }
    AssertionResult AssertEquivalent(string expectedValue, T element);
    bool IsMappedTo(string columnName);
}