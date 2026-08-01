using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class DbMigrationv12 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SenderIdentities_ClientId_EmailType",
                table: "SenderIdentities");

            migrationBuilder.AddColumn<string>(
                name: "Tenant",
                table: "SenderIdentities",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MessageId",
                table: "Emails",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Tenant",
                table: "Emails",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            // Backfill MessageId for any pre-existing rows with their (already unique) primary key, so
            // the unique index below can be created regardless of how many rows already exist. New rows
            // always set MessageId to the envelope id explicitly.
            migrationBuilder.Sql(
                "UPDATE \"Emails\" SET \"MessageId\" = \"Id\"::text WHERE \"MessageId\" = '';");

            migrationBuilder.CreateIndex(
                name: "IX_SenderIdentities_ClientId_EmailType_Tenant",
                table: "SenderIdentities",
                columns: new[] { "ClientId", "EmailType", "Tenant" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Emails_MessageId",
                table: "Emails",
                column: "MessageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SenderIdentities_ClientId_EmailType_Tenant",
                table: "SenderIdentities");

            migrationBuilder.DropIndex(
                name: "IX_Emails_MessageId",
                table: "Emails");

            migrationBuilder.DropColumn(
                name: "Tenant",
                table: "SenderIdentities");

            migrationBuilder.DropColumn(
                name: "MessageId",
                table: "Emails");

            migrationBuilder.DropColumn(
                name: "Tenant",
                table: "Emails");

            migrationBuilder.CreateIndex(
                name: "IX_SenderIdentities_ClientId_EmailType",
                table: "SenderIdentities",
                columns: new[] { "ClientId", "EmailType" },
                unique: true);
        }
    }
}
