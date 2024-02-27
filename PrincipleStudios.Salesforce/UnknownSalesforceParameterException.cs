namespace PrincipleStudios.Salesforce;

public class UnknownSalesforceParameterException : Exception
{
    public UnknownSalesforceParameterException() { }
    public UnknownSalesforceParameterException(string message) : base(message) { }
    public UnknownSalesforceParameterException(string message, Exception inner) : base(message, inner) { }
}
