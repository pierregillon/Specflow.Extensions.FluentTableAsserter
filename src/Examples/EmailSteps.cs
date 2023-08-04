using Specflow.Extensions.FluentTableAsserter;
using TechTalk.SpecFlow;

namespace Examples;

[Binding]
public class EmailSteps
{
    private readonly ErrorDriver _errorDriver;
    private Email? _receivedEmail;

    public EmailSteps(ErrorDriver errorDriver) => _errorDriver = errorDriver;

    [When(@"I receive an email with")]
    public void WhenIReceiveAnEmailWith(Table table)
    {
        var singleRow = table.Rows.Single();

        _receivedEmail = new Email(
            singleRow["FromEmail"],
            singleRow["ToEmail"],
            singleRow["Subject"],
            singleRow["PlainText"],
            int.Parse(singleRow["AttachmentCount"])
        );
    }

    [When(@"asserting the email properties with")]
    [Then(@"the received email is")]
    public void WhenAssertingTheEmailPropertiesWith(Table table) =>
        _errorDriver.TryExecute(() => AssertTableValid(table));

    private void AssertTableValid(Table table) => _receivedEmail
        .InstanceShouldBeEquivalentToTable(table)
        .WithProperty(x => x.FromEmail)
        .WithProperty(x => x.FromEmail, x => x.ComparedToColumn("From"))
        .WithProperty(x => x.ToEmail)
        .WithProperty(x => x.ToEmail, x => x.ComparedToColumn("To"))
        .WithProperty(x => x.Subject)
        .WithProperty(x => x.PlainText)
        .WithProperty(x => x.PlainText, x => x.ComparedToColumn("Text"))
        .WithProperty(x => x.AttachmentCount)
        .Assert();
}

internal record Email(string FromEmail, string ToEmail, string Subject, string PlainText, int AttachmentCount);