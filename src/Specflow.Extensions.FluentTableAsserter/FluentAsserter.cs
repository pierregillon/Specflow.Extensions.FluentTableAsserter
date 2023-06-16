using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using TechTalk.SpecFlow;

namespace Specflow.Extensions.FluentTableAsserter;

public class FluentAsserter<T> : IFluentAsserter<T>
{
    private readonly Table _table;
    private readonly IEnumerable<T> _actualValues;
    private readonly List<IPropertyDefinition<T>> _propertyDefinitions = new();
    private readonly List<string> _ignoredColumns = new();

    public FluentAsserter(Table table, IEnumerable<T> actualValues)
    {
        _table = table;
        _actualValues = actualValues;
    }

    public IFluentAsserter<T> WithProperty<TProperty>(
        Expression<Func<T, TProperty>> propertyExpression,
        Func<PropertyConfiguration, PropertyConfiguration>? configure = null
    )
    {
        var configuration = configure is not null
            ? configure(PropertyConfiguration.Default)
            : PropertyConfiguration.Default;

        var propertyDefinition =
            new PropertyDefinition<T, TProperty>(typeof(TProperty), propertyExpression, configuration);

        _propertyDefinitions.Add(propertyDefinition);

        return this;
    }

    public IFluentAsserter<T> IgnoringColumn(string columnName)
    {
        _ignoredColumns.Add(columnName);
        return this;
    }

    public void AssertEquivalent()
    {
        var notMappedHeaders = _table.Header
            .Where(header => !_ignoredColumns.Contains(header))
            .Where(header => !_propertyDefinitions.Any(p => p.IsMappedTo(header)))
            .ToArray();

        if (notMappedHeaders.Any())
        {
            throw new MissingColumnDefinitionException(typeof(T), notMappedHeaders.First());
        }

        for (var rowIndex = 0; rowIndex < _table.Rows.Count; rowIndex++)
        {
            var row = _table.Rows[rowIndex];
            var data = _actualValues.ElementAt(rowIndex);
            for (var headerIndex = 0; headerIndex < _table.Rows.Count; headerIndex++)
            {
                var propertyDefinition = _propertyDefinitions[headerIndex];
                var expectedValue = row[headerIndex];

                var result = propertyDefinition.AssertEquivalent(expectedValue, data);

                if (!result.IsSuccess)
                {
                    throw new ExpectedTableNotEquivalentToDataException(
                        rowIndex,
                        result.MemberName,
                        result.ActualValue,
                        _table.Header.ElementAt(headerIndex),
                        expectedValue
                    );
                }
            }
        }
    }
}