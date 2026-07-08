namespace Stycue.Api.Options
{
    public class ApiDocOptions
    {
        public const string SectionName = "ApiDocs";

        public bool Enabled { get; init; } = true;
        public string Title { get; init; } = "API";
        public string Version { get; init; } = "v1";
        public string Route { get; init; } = "/api-docs";
    }
}
