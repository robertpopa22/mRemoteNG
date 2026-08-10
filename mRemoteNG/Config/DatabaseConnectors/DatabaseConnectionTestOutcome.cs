namespace mRemoteNG.Config.DatabaseConnectors
{
    /// <summary>
    /// The classified outcome of a connection test plus the provider's own error text, so the
    /// options dialog can say what actually failed instead of "an unknown error occurred". (#165)
    /// </summary>
    /// <param name="Result">The classified result.</param>
    /// <param name="ErrorDetail">Provider error text, or null when the connection succeeded.</param>
    public record DatabaseConnectionTestOutcome(ConnectionTestResult Result, string? ErrorDetail);
}
