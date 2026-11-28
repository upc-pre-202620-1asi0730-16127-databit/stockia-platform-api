using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Migrations
{
    /// <inheritdoc />
    public partial class StripeBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "cancel_at_period_end",
                table: "subscriptions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_card_on_file",
                table: "subscriptions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "stripe_customer_id",
                table: "subscriptions",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "stripe_subscription_id",
                table: "subscriptions",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "payment_transactions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    restaurant_id = table.Column<int>(type: "int", nullable: false),
                    plan_id = table.Column<int>(type: "int", nullable: true),
                    invoice_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_transactions", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "processed_payment_events",
                columns: table => new
                {
                    event_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    type = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_processed_payment_events", x => x.event_id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_stripe_customer_id",
                table: "subscriptions",
                column: "stripe_customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_stripe_subscription_id",
                table: "subscriptions",
                column: "stripe_subscription_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_transactions_invoice_id",
                table: "payment_transactions",
                column: "invoice_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_transactions_restaurant_id",
                table: "payment_transactions",
                column: "restaurant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_transactions");

            migrationBuilder.DropTable(
                name: "processed_payment_events");

            migrationBuilder.DropIndex(
                name: "ix_subscriptions_stripe_customer_id",
                table: "subscriptions");

            migrationBuilder.DropIndex(
                name: "ix_subscriptions_stripe_subscription_id",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "cancel_at_period_end",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "has_card_on_file",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "stripe_customer_id",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "stripe_subscription_id",
                table: "subscriptions");
        }
    }
}
