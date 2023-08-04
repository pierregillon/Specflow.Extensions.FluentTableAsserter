using System.Collections.Immutable;
using FluentAssertions;
using Specflow.Extensions.FluentTableAsserter.Exceptions;
using TechTalk.SpecFlow;

namespace Specflow.Extensions.FluentTableAsserter.Tests;

public abstract class FluentObjectAssertions
{
    public class InstanciatingAssertion
    {
        private readonly Table _someTable = new("test");

        public static IEnumerable<object?[]> Collections()
        {
            yield return new object[] { new List<int>() };
            yield return new object[] { Array.Empty<int>() };
            yield return new object[] { new HashSet<int>() };
            yield return new object[] { new ImmutableArray<int>() };
        }

        [Theory]
        [MemberData(nameof(Collections))]
        public void Fails_when_source_is_a_collection(IEnumerable<int> collection)
        {
            Action action = () => collection.InstanceShouldBeEquivalentToTable(_someTable);

            action
                .Should()
                .Throw<InstanceToAssertCannotBeACollectionException>()
                .WithMessage(
                    $"You cannot call '{nameof(ObjectExtensions.InstanceShouldBeEquivalentToTable)}' with a collection. "
                    + $"Make sure it is a simple object or use '{nameof(EnumerableExtensions.ShouldBeEquivalentToTable)}' to assert your collection of items."
                );
        }
    }
}

public abstract class FluentCollectionAssertions
{
    public class InstanciatingAssertion
    {
        private static readonly Table SomeTable = new("test");

        [Fact]
        public void Fails_to_compile_when_no_property_provided()
        {
            const string code = @"

using System.Collections.Generic;
using TechTalk.SpecFlow;
using Specflow.Extensions.FluentTableAsserter;

namespace Test;

public class UserCode
{
    public static void Execute()
    {
        new List<Person>()
            .ShouldBeEquivalentToTable(new Table(""some header""))
            .AssertEquivalent();
    }

    public record Person;
}

";

            code
                .Should()
                .NotCompile()
                .WithErrors(
                    "'IFluentAsserterInitialization<UserCode.Person>' does not contain a definition for 'AssertEquivalent' "
                    + "and no accessible extension method 'AssertEquivalent' accepting a first argument of type "
                    + "'IFluentAsserterInitialization<UserCode.Person>' could be found (are you missing a "
                    + "using directive or an assembly reference?)"
                );
        }

        [Fact]
        public void Fails_with_null_list()
        {
            List<Person> persons = null!;

            var wrongAction = () => persons.ShouldBeEquivalentToTable(SomeTable);

            wrongAction
                .Should()
                .Throw<ArgumentException>()
                .WithMessage("Value cannot be null. (Parameter 'actualElements')");
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private record Person;
    }

    public class ColumnsCompatibilityCheck
    {
        private static readonly IEnumerable<Person> EmptyPersonList = Array.Empty<Person>();

        [Fact]
        public void Fails_when_table_has_columns_that_has_not_been_mapped_to_element_property()
        {
            var table = new Table("Test");

            var action = () => EmptyPersonList
                .ShouldBeEquivalentToTable(table)
                .WithProperty(x => x.FirstName)
                .Assert();

            action
                .Should()
                .Throw<MissingColumnDefinitionException>()
                .WithMessage("The column 'Test' has not been mapped to any property of class 'Person'.");
        }

        [Fact]
        public void Accepts_when_ignoring_not_wanted_columns()
        {
            var table = new Table("FirstName", "Test");

            var action = () => EmptyPersonList
                .ShouldBeEquivalentToTable(table)
                .WithProperty(x => x.FirstName)
                .IgnoringColumn("Test")
                .Assert();

            action
                .Should()
                .NotThrow();
        }

        [Fact]
        public void Cannot_declare_multiple_times_same_property()
        {
            var table = new Table("FirstName");

            var action = () => EmptyPersonList
                .ShouldBeEquivalentToTable(table)
                .WithProperty(x => x.FirstName)
                .WithProperty(x => x.FirstName)
                .Assert();

            action
                .Should()
                .Throw<PropertyDefinitionAlreadyExistsException>()
                .WithMessage("The same property definition exists: Person.FirstName -> [FirstName]");
        }

        [Theory]
        [InlineData("FirstName")]
        [InlineData("First name")]
        public void Cannot_declare_multiple_times_same_property_without_different_column_name(string headerVariation)
        {
            var table = new Table("FirstName");

            var action = () => EmptyPersonList
                .ShouldBeEquivalentToTable(table)
                .WithProperty(x => x.FirstName)
                .WithProperty(x => x.FirstName, options => options.ComparedToColumn(headerVariation))
                .Assert();

            action
                .Should()
                .Throw<Exception>();
        }

        [Fact]
        public void Can_declare_multiple_times_same_property_but_with_different_column_name()
        {
            var table = new Table("FirstName", "FirstName2");

            var action = () => EmptyPersonList
                .ShouldBeEquivalentToTable(table)
                .WithProperty(x => x.FirstName)
                .WithProperty(x => x.FirstName, options => options.ComparedToColumn("FirstName2"))
                .Assert();

            action
                .Should()
                .NotThrow();
        }

        [Theory]
        [InlineData("First Name")]
        [InlineData("First name")]
        [InlineData("firstname")]
        [InlineData("first name")]
        [InlineData("FIRST NAME")]
        public void Accepts_column_when_naming_is_equivalent_for_a_human(string header)
        {
            var table = new Table(header);

            var action = () => EmptyPersonList
                .ShouldBeEquivalentToTable(table)
                .WithProperty(x => x.FirstName)
                .Assert();

            action
                .Should()
                .NotThrow();
        }

        [Theory]
        [InlineData("My First Name")]
        [InlineData("my First name")]
        [InlineData("myfirstname")]
        [InlineData("my first name")]
        [InlineData("MY FIRST NAME")]
        public void Accepts_column_when_a_different_column_name_has_been_configured(string header)
        {
            var table = new Table(header);

            var action = () => EmptyPersonList
                .ShouldBeEquivalentToTable(table)
                .WithProperty(x => x.FirstName, options => options
                    .ComparedToColumn("MyFirstName"))
                .Assert();

            action
                .Should()
                .NotThrow();
        }

        [Fact]
        public void Accepts_multiple_property_mapping_to_the_same_column()
        {
            var table = new Table("Name");

            var action = () => EmptyPersonList
                .ShouldBeEquivalentToTable(table)
                .WithProperty(x => x.FirstName, options => options
                    .ComparedToColumn("Name"))
                .WithProperty(x => x.LastName, options => options
                    .ComparedToColumn("Name"))
                .Assert();

            action
                .Should()
                .NotThrow();
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private record Person(string FirstName, string LastName);
    }

    public class ComparingRowsAndValues
    {
        private readonly Table _expectedTable;
        private readonly List<Person> _actualPersons = new();
        private readonly Action _assertion;

        public ComparingRowsAndValues()
        {
            _expectedTable = new Table("FirstName", "LastName");
            _assertion = () => _actualPersons
                .ShouldBeEquivalentToTable(_expectedTable)
                .WithProperty(x => x.FirstName)
                .WithProperty(x => x.LastName)
                .Assert();
        }

        [Fact]
        public void Does_not_throw_when_values_are_equivalent_to_rows()
        {
            _expectedTable.AddRow("John", "Doe");
            _actualPersons.Add(new Person("John", "Doe"));
            _assertion
                .Should()
                .NotThrow();
        }

        [Fact]
        public void Accepts_property_declaration_in_different_order_than_columns()
        {
            _expectedTable.AddRow("John", "Doe");
            _actualPersons.Add(new Person("John", "Doe"));

            var action = () => _actualPersons
                .ShouldBeEquivalentToTable(_expectedTable)
                .WithProperty(x => x.LastName)
                .WithProperty(x => x.FirstName)
                .Assert();

            action
                .Should()
                .NotThrow();
        }

        [Fact]
        public void Throws_when_one_value_is_different()
        {
            _expectedTable.AddRow("John", "Doe");
            _actualPersons.Add(new Person("Jonathan", "Doe"));

            _assertion
                .Should()
                .Throw<ExpectedTableNotEquivalentToCollectionItemException>()
                .WithMessage(
                    "At index 0, 'FirstName' actual data is 'Jonathan' but should be 'John' from column 'FirstName'."
                );
        }

        [Fact]
        public void Accepts_empty_string_instead_of_null()
        {
            _expectedTable.AddRow(string.Empty, "Doe");
            _actualPersons.Add(new Person(null!, "Doe"));

            _assertion
                .Should()
                .NotThrow();
        }

        [Fact]
        public void Throws_when_row_count_greater_than_element_count()
        {
            _expectedTable.AddRow("Jonathan", "Doe");
            _expectedTable.AddRow("Test", "test");
            _actualPersons.Add(new Person("Jonathan", "Doe"));

            _assertion
                .Should()
                .Throw<TableRowCountIsDifferentThanElementCountException<Person>>()
                .WithMessage("Table row count (2) is different than 'Person' count (1)");
        }

        [Fact]
        public void Throws_when_row_count_smaller_than_element_count()
        {
            _expectedTable.AddRow("Jonathan", "Doe");
            _actualPersons.Add(new Person("Jonathan", "Doe"));
            _actualPersons.Add(new Person("Test", "Test"));

            _assertion
                .Should()
                .Throw<TableRowCountIsDifferentThanElementCountException<Person>>()
                .WithMessage("Table row count (1) is different than 'Person' count (2)");
        }

        [Fact]
        public void Accepts_multiple_property_mapping_to_the_same_column()
        {
            _expectedTable.AddRow("Jonathan", "Doe");
            _actualPersons.Add(new Person("Jonathan", "Jonathan"));

            var action = () => _actualPersons
                .ShouldBeEquivalentToTable(_expectedTable)
                .WithProperty(x => x.FirstName, options => options
                    .ComparedToColumn("FirstName"))
                .WithProperty(x => x.LastName, options => options
                    .ComparedToColumn("FirstName"))
                .IgnoringColumn("LastName")
                .Assert();

            action
                .Should()
                .NotThrow();
        }

        private record Person(string FirstName, string LastName);
    }

    public class ColumnValueConvertionToPropertyValue
    {
        private readonly Table _expectedTemperatureTable;
        private readonly List<Temperature> _actualTemperatures;

        public ColumnValueConvertionToPropertyValue()
        {
            _expectedTemperatureTable = new Table("Value", "Type");
            _actualTemperatures = new List<Temperature>();
        }

        [Fact]
        public void Throws_when_column_value_cannot_be_converted_to_property_type()
        {
            _expectedTemperatureTable.AddRow("Test", "Celsius");
            _actualTemperatures.Add(new Temperature(100, TemperatureType.Celsius));

            var action = () => _actualTemperatures
                .ShouldBeEquivalentToTable(_expectedTemperatureTable)
                .WithProperty(x => x.Value)
                .IgnoringColumn("Type")
                .Assert();

            action
                .Should()
                .Throw<CannotConvertColumnValueToPropertyTypeException>()
                .WithMessage("The value 'Test' cannot be converted to type 'Int32' of property 'Temperature.Value'");
        }

        [Fact]
        public void Accepts_when_column_value_cannot_be_converted_to_property_type_but_a_converter_is_defined()
        {
            _expectedTemperatureTable.AddRow("hundred", "Celsius");
            _actualTemperatures.Add(new Temperature(100, TemperatureType.Celsius));

            var action = () => _actualTemperatures
                .ShouldBeEquivalentToTable(_expectedTemperatureTable)
                .WithProperty(x => x.Value, options => options
                    .WithColumnValueConversion(columnValue => columnValue == "hundred" ? 100 : -1))
                .IgnoringColumn("Type")
                .Assert();

            action
                .Should()
                .NotThrow();
        }

        [Fact]
        public void Automatically_parse_value_to_enum()
        {
            _expectedTemperatureTable.AddRow("100", "kelvin");
            _actualTemperatures.Add(new Temperature(100, TemperatureType.Kelvin));

            var action = () => _actualTemperatures
                .ShouldBeEquivalentToTable(_expectedTemperatureTable)
                .WithProperty(x => x.Value)
                .WithProperty(x => x.Type)
                .Assert();

            action
                .Should()
                .NotThrow();
        }

        [Theory]
        [InlineData("some other value")]
        [InlineData("Some Other Value")]
        public void Automatically_parse_human_readable_value_to_enum(string humanReadable)
        {
            _expectedTemperatureTable.AddRow("100", humanReadable);
            _actualTemperatures.Add(new Temperature(100, TemperatureType.SomeOtherValue));

            var action = () => _actualTemperatures
                .ShouldBeEquivalentToTable(_expectedTemperatureTable)
                .WithProperty(x => x.Value)
                .WithProperty(x => x.Type)
                .Assert();

            action
                .Should()
                .NotThrow();
        }

        [Fact]
        public void Fails_when_enum_value_invalid()
        {
            _expectedTemperatureTable.AddRow("100", "test");
            _actualTemperatures.Add(new Temperature(100, TemperatureType.Kelvin));

            var action = () => _actualTemperatures
                .ShouldBeEquivalentToTable(_expectedTemperatureTable)
                .WithProperty(x => x.Value)
                .WithProperty(x => x.Type)
                .Assert();

            action
                .Should()
                .Throw<CannotParseEnumToEnumValuException<TemperatureType>>()
                .WithMessage("'test' cannot be parsed to any enum value of type TemperatureType.");
        }

        private record Temperature(int Value, TemperatureType Type);

        private enum TemperatureType
        {
            Celsius,
            Kelvin,
            SomeOtherValue
        }
    }

    public class ArrayConvertion
    {
        [Fact]
        public void Enumerable_property_type_is_comparable_with_table_column_value()
        {
            var table = new Table("Names");
            table.AddRow("john, sam, eric");

            var elements = new List<Details>
            {
                new(new[] { "john", "sam", "eric" })
            };

            elements
                .ShouldBeEquivalentToTable(table)
                .WithProperty(x => x.Names, o => o
                    .WithColumnValueConversion(columnValue => columnValue.Split(',', StringSplitOptions.TrimEntries))
                )
                .Assert();
        }

        [Fact]
        public void Order_is_preserved_comparing_enumerable()
        {
            var table = new Table("Names");
            table.AddRow("john, sam, eric");

            var elements = new List<Details>
            {
                new(new[] { "sam", "john", "eric" })
            };

            var action = () => elements
                .ShouldBeEquivalentToTable(table)
                .WithProperty(x => x.Names, o => o
                    .WithColumnValueConversion(columnValue => columnValue.Split(',', StringSplitOptions.TrimEntries))
                )
                .Assert();

            action
                .Should()
                .Throw<ExpectedTableNotEquivalentToCollectionItemException>()
                .WithMessage(
                    "At index 0, 'Names' actual data is 'sam, john, eric' but should be 'john, sam, eric' from column 'Names'."
                );
        }

        [Fact]
        public void Different_length_fails()
        {
            var table = new Table("Names");
            table.AddRow("john, sam");

            var elements = new List<Details>
            {
                new(new[] { "john", "sam", "eric" })
            };

            var action = () => elements
                .ShouldBeEquivalentToTable(table)
                .WithProperty(x => x.Names, o => o
                    .WithColumnValueConversion(columnValue => columnValue.Split(',', StringSplitOptions.TrimEntries))
                )
                .Assert();

            action
                .Should()
                .Throw<ExpectedTableNotEquivalentToCollectionItemException>()
                .WithMessage(
                    "At index 0, 'Names' actual data is 'john, sam, eric' but should be 'john, sam' from column 'Names'."
                );
        }

        private record Details(IEnumerable<string> Names);
    }
}