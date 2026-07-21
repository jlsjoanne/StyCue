using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stycue.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchDocumentFullTextIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
          """
          CREATE FULLTEXT CATALOG [StycueSearchCatalog];
          """,
          suppressTransaction: true);

            migrationBuilder.Sql(
                """
          CREATE FULLTEXT INDEX ON [dbo].[SearchDocuments]
          (
              [SearchText] LANGUAGE 1028
          )
          KEY INDEX [PK_SearchDocuments]
          ON [StycueSearchCatalog]
          WITH CHANGE_TRACKING AUTO;
          """,
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
          """
          DROP FULLTEXT INDEX ON [dbo].[SearchDocuments];
          """,
          suppressTransaction: true);

            migrationBuilder.Sql(
                """
          DROP FULLTEXT CATALOG [StycueSearchCatalog];
          """,
                suppressTransaction: true);
        }
    }
}
