using System;

namespace Specflow.Extensions.FluentTableAsserter.CollectionAsserters.Exceptions;

public class PropertyDefinitionAlreadyExistsException : Exception
{
    public PropertyDefinitionAlreadyExistsException(string propertyDefinitionString) : base(
        $"The same property definition exists: {propertyDefinitionString}")
    {
    }
}