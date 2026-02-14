namespace OrderFlow.Infrastructure.Options;

public sealed class MongoOptions
{
    public const string SectionName = "Mongo";
    public string ConnectionString { get; init; } = default!;
    public string Database { get; init; } = "orderflow";
}
