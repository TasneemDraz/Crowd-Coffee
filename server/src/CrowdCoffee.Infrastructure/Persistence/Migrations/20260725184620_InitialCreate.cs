using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CrowdCoffee.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.UniqueConstraint("AK_Categories_Slug", x => x.Slug);
                });

            migrationBuilder.CreateTable(
                name: "ModifierGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SelectionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MaxSelections = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModifierGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DeliveryFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StaffUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    QuantityOnHand = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    QuantityReserved = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    ReorderPoint = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategorySlug = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategorySlug",
                        column: x => x.CategorySlug,
                        principalTable: "Categories",
                        principalColumn: "Slug",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModifierOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifierGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PriceDelta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModifierOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModifierOptions_ModifierGroups_ModifierGroupId",
                        column: x => x.ModifierGroupId,
                        principalTable: "ModifierGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductModifierGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategorySlug = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifierGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductModifierGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductModifierGroups_Categories_CategorySlug",
                        column: x => x.CategorySlug,
                        principalTable: "Categories",
                        principalColumn: "Slug",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductModifierGroups_ModifierGroups_ModifierGroupId",
                        column: x => x.ModifierGroupId,
                        principalTable: "ModifierGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderStatusHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ToStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderStatusHistories_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    MovementType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovements_StockItems_StockItemId",
                        column: x => x.StockItemId,
                        principalTable: "StockItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Recipes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifierOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StockItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityPerUnit = table.Column<decimal>(type: "decimal(18,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipes", x => x.Id);
                    table.CheckConstraint("CK_Recipes_ExactlyOneOwner", "([ProductId] IS NOT NULL AND [ModifierOptionId] IS NULL) OR ([ProductId] IS NULL AND [ModifierOptionId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Recipes_ModifierOptions_ModifierOptionId",
                        column: x => x.ModifierOptionId,
                        principalTable: "ModifierOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Recipes_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Recipes_StockItems_StockItemId",
                        column: x => x.StockItemId,
                        principalTable: "StockItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderItemModifiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifierOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifierName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PriceDelta = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItemModifiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItemModifiers_ModifierOptions_ModifierOptionId",
                        column: x => x.ModifierOptionId,
                        principalTable: "ModifierOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItemModifiers_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Description", "DisplayOrder", "IsActive", "Label", "Slug" },
                values: new object[,]
                {
                    { new Guid("008232a0-7fce-2d2d-1a24-5426c0ddadb4"), "Ceremonial-grade matcha, hot or iced.", 7, true, "Matcha", "matcha" },
                    { new Guid("1a5d5e93-d500-9abd-502d-094410cb3801"), "Thick, blended, and topped up.", 8, true, "Shakes", "shakes" },
                    { new Guid("29e7bcb0-6fa2-609c-3de4-dbdc47344d0c"), "CROWD originals — the ones only we make.", 4, true, "Signatures", "sigdrinks" },
                    { new Guid("40ea3427-30a2-3fc1-1637-a539e90536f5"), "House specialties from the pastry case.", 16, true, "Signature Desserts", "sigdesserts" },
                    { new Guid("40ea4e52-a6c5-e8cb-699c-ecfde7b9b5b5"), "Fresh and hearty, for the health-conscious.", 18, true, "Salads", "salads" },
                    { new Guid("481576ab-9fe3-1377-49bd-8f00d86f4612"), "Manual brew methods, made to order.", 3, true, "Speciality", "speciality" },
                    { new Guid("504fab73-93a4-e511-479e-10774dd79975"), "Fresh fruit, blended smooth.", 9, true, "Smoothies", "smoothies" },
                    { new Guid("57156ec1-3fde-25c7-a601-fb51ac6d5f32"), "For the non-coffee crowd.", 5, true, "Non Coffee", "noncoffee" },
                    { new Guid("5a6f999b-09e0-594a-03ab-6d360dd5308f"), "Blended, creamy, and cold.", 6, true, "Frappé Blended", "frappe" },
                    { new Guid("5af50df0-0228-3583-dc7b-ac663976e710"), null, 17, true, "Cheesecakes", "cheesecakes" },
                    { new Guid("5c6f5a10-9bca-883a-c81e-2c0218d7666c"), "Baguette, sourdough or tortilla — your call.", 11, true, "Sandwiches", "sandwiches" },
                    { new Guid("5e12c902-6733-bc4c-8108-035c2e54498f"), "Bold, no milk, no fuss.", 0, true, "Black Coffee", "black" },
                    { new Guid("753ed861-6572-406b-7766-ab18926f468e"), "Fresh croissants, baked daily.", 13, true, "Bakery", "bakery" },
                    { new Guid("8319477d-954c-a16c-c45f-6d06a15b4536"), "Chilled and ready for the corniche heat.", 2, true, "Iced Coffee", "iced" },
                    { new Guid("89119093-3b1d-57db-8396-4d353f1453ac"), "Morning plates, all day long.", 12, true, "Breakfast", "breakfast" },
                    { new Guid("8dd057fa-2af6-c879-163c-9e423c633e53"), "Filled and finished, straight from the oven.", 14, true, "Sweet Bakeries", "sweetbakeries" },
                    { new Guid("9974a1f7-b858-cdee-33fb-8ea99d88c1aa"), "Fruit and soda over ice, alcohol-free.", 10, true, "Mojito", "mojito" },
                    { new Guid("bab7949e-94f8-ada7-acc6-062f33c4eade"), "Espresso-based, made the classic way.", 1, true, "Classic Coffee", "classic" },
                    { new Guid("cd4b4d51-08d2-1338-b3de-e5abdedcaeb6"), "Cookies and cakes, baked in-house.", 15, true, "Pastries", "pastries" }
                });

            migrationBuilder.InsertData(
                table: "ModifierGroups",
                columns: new[] { "Id", "DisplayOrder", "IsRequired", "MaxSelections", "Name", "SelectionType", "Slug" },
                values: new object[,]
                {
                    { new Guid("63e02118-3979-0a5d-0c3e-a4fb8ab1a42c"), 0, true, 1, "Milk Type", "ExactlyOne", "milk" },
                    { new Guid("d5e8a7b5-131c-2cab-996e-808bae8e91d8"), 2, false, 2, "Syrups", "UpToN", "syrups" },
                    { new Guid("e265d8c3-7254-fe6b-5f85-0e910486d555"), 1, false, 1, "Espresso Shot", "UpToN", "shot" }
                });

            migrationBuilder.InsertData(
                table: "ModifierOptions",
                columns: new[] { "Id", "DisplayOrder", "IsActive", "ModifierGroupId", "Name", "PriceDelta", "Slug" },
                values: new object[,]
                {
                    { new Guid("03134b31-b193-04cf-864b-85ba223f28d8"), 3, true, new Guid("63e02118-3979-0a5d-0c3e-a4fb8ab1a42c"), "Oat Milk", 40m, "oat" },
                    { new Guid("1c17a227-4320-94f0-085c-f7fbb63e04ee"), 5, true, new Guid("d5e8a7b5-131c-2cab-996e-808bae8e91d8"), "Honey Syrup", 30m, "honey" },
                    { new Guid("1d51b3c2-92c5-7184-0e70-bdcd9f976fc1"), 7, true, new Guid("d5e8a7b5-131c-2cab-996e-808bae8e91d8"), "Caramel Syrup", 30m, "caramel" },
                    { new Guid("25da2ca0-03ed-79d0-c382-c46a53dc7266"), 8, true, new Guid("d5e8a7b5-131c-2cab-996e-808bae8e91d8"), "Salted Caramel Syrup", 30m, "saltedcaramel" },
                    { new Guid("4b930329-1780-9985-cc5d-715724d5eda3"), 5, true, new Guid("63e02118-3979-0a5d-0c3e-a4fb8ab1a42c"), "Coconut Milk", 40m, "coconut" },
                    { new Guid("5d11f6ad-675c-767a-4c63-2e23493d1211"), 1, true, new Guid("d5e8a7b5-131c-2cab-996e-808bae8e91d8"), "Sugar Free Caramel Syrup", 40m, "sf-caramel" },
                    { new Guid("5ea80c3b-f8f7-ad18-98f0-facdc3fddaec"), 0, true, new Guid("63e02118-3979-0a5d-0c3e-a4fb8ab1a42c"), "No Milk", 0m, "none" },
                    { new Guid("7819f6fc-4bc8-0e02-4314-ed0bc6d2db4b"), 2, true, new Guid("d5e8a7b5-131c-2cab-996e-808bae8e91d8"), "Sugar Free Vanilla Syrup", 40m, "sf-vanilla" },
                    { new Guid("9389480b-e8f0-88cc-13de-f72baaedba92"), 11, true, new Guid("d5e8a7b5-131c-2cab-996e-808bae8e91d8"), "Toffee Nut Syrup", 30m, "toffeenut" },
                    { new Guid("a93c104d-b60b-57d8-3acc-ff7bb3081678"), 1, true, new Guid("63e02118-3979-0a5d-0c3e-a4fb8ab1a42c"), "Full Cream Milk", 0m, "full" },
                    { new Guid("b3f3ff24-d8a1-daaf-2cb0-7b1dcb2fa8c9"), 4, true, new Guid("d5e8a7b5-131c-2cab-996e-808bae8e91d8"), "Vanilla Syrup", 30m, "vanilla" },
                    { new Guid("c2c96c89-7b65-761c-4d75-4696218bb24d"), 3, true, new Guid("d5e8a7b5-131c-2cab-996e-808bae8e91d8"), "Sugar Free Hazelnut Syrup", 40m, "sf-hazelnut" },
                    { new Guid("ce2c6527-5bf4-a854-3bd4-ae831f8d2ff6"), 10, true, new Guid("d5e8a7b5-131c-2cab-996e-808bae8e91d8"), "Strawberry Syrup", 30m, "strawberry" },
                    { new Guid("dd0195fb-9e8f-b435-712c-7aa82b9a26a3"), 0, true, new Guid("d5e8a7b5-131c-2cab-996e-808bae8e91d8"), "Sugar Free Salted Caramel Syrup", 40m, "sf-saltedcaramel" },
                    { new Guid("dd4f0baa-6f11-b50b-2395-c13e4e5f59fe"), 4, true, new Guid("63e02118-3979-0a5d-0c3e-a4fb8ab1a42c"), "Almond Milk", 40m, "almond" },
                    { new Guid("e94c5406-2c09-e23d-0926-090ec330c4e1"), 9, true, new Guid("d5e8a7b5-131c-2cab-996e-808bae8e91d8"), "White Chocolate Syrup", 30m, "whitechoc" },
                    { new Guid("ec68c938-7c9d-c48a-89b2-41c5b5c6786d"), 2, true, new Guid("63e02118-3979-0a5d-0c3e-a4fb8ab1a42c"), "Skimmed Milk", 0m, "skimmed" },
                    { new Guid("ed7f0966-6c95-3628-aa16-a4521dfa5a99"), 0, true, new Guid("e265d8c3-7254-fe6b-5f85-0e910486d555"), "Espresso Shot", 40m, "espresso" },
                    { new Guid("f6e09400-e2e7-5284-06e2-dc4592045ce8"), 6, true, new Guid("d5e8a7b5-131c-2cab-996e-808bae8e91d8"), "Cinnamon Syrup", 30m, "cinnamon" }
                });

            migrationBuilder.InsertData(
                table: "ProductModifierGroups",
                columns: new[] { "Id", "CategorySlug", "ModifierGroupId" },
                values: new object[,]
                {
                    { new Guid("00f3b17e-0e8a-b5ca-f574-252b51c6ff44"), "mojito", new Guid("e265d8c3-7254-fe6b-5f85-0e910486d555") },
                    { new Guid("01fb2d32-9bce-6b90-7a5a-6a66e0ddcb48"), "speciality", new Guid("d5e8a7b5-131c-2cab-996e-808bae8e91d8") },
                    { new Guid("02298f0e-4888-2ffa-48f3-7c3c19d5781f"), "black", new Guid("63e02118-3979-0a5d-0c3e-a4fb8ab1a42c") },
                    { new Guid("1287f858-eaba-5ebf-5dc4-a32a71a48545"), "mojito", new Guid("63e02118-3979-0a5d-0c3e-a4fb8ab1a42c") },
                    { new Guid("212a2892-9f0e-ade9-dc12-10bab2968ffc"), "iced", new Guid("63e02118-3979-0a5d-0c3e-a4fb8ab1a42c") },
                    { new Guid("350f9a1f-8ff3-e622-e490-dfe3441ea3a2"), "shakes", new Guid("d5e8a7b5-131c-2cab-996e-808bae8e91d8") },
                    { new Guid("433ca10b-2039-7bd3-cd73-b4171eb16f2f"), "black", new Guid("d5e8a7b5-131c-2cab-996e-808bae8e91d8") },
                    { new Guid("445c1846-3df3-a17c-e7f3-c5b830e8b6b7"), "shakes", new Guid("63e02118-3979-0a5d-0c3e-a4fb8ab1a42c") },
                    { new Guid("4f600f2b-35c5-91c7-ee71-58ac30db96c8"), "noncoffee", new Guid("63e02118-3979-0a5d-0c3e-a4fb8ab1a42c") },
                    { new Guid("5570ca6c-6a27-b5a0-9060-4ccaf73b62be"), "iced", new Guid("e265d8c3-7254-fe6b-5f85-0e910486d555") },
                    { new Guid("5ad5d737-2e88-f276-b1ce-f857c9997d27"), "smoothies", new Guid("d5e8a7b5-131c-2cab-996e-808bae8e91d8") },
                    { new Guid("81a38508-b678-69b8-6c98-64a749d4e511"), "sigdrinks", new Guid("d5e8a7b5-131c-2cab-996e-808bae8e91d8") },
                    { new Guid("81e69b37-cddf-4535-cdc2-d0662e59b1b3"), "matcha", new Guid("e265d8c3-7254-fe6b-5f85-0e910486d555") },
                    { new Guid("84a84fa5-80a4-f006-5c03-711d6bb0c7d2"), "frappe", new Guid("d5e8a7b5-131c-2cab-996e-808bae8e91d8") },
                    { new Guid("8599494f-8251-f117-a44c-e34758312116"), "matcha", new Guid("63e02118-3979-0a5d-0c3e-a4fb8ab1a42c") },
                    { new Guid("8c979b1e-e67d-e0f1-356f-5d2f70faa529"), "frappe", new Guid("63e02118-3979-0a5d-0c3e-a4fb8ab1a42c") },
                    { new Guid("92c7f97b-b73c-3e09-94dc-b32bf9994f6e"), "noncoffee", new Guid("e265d8c3-7254-fe6b-5f85-0e910486d555") },
                    { new Guid("9f1a5f39-8360-81ea-c774-87cc4d2e972e"), "sigdrinks", new Guid("e265d8c3-7254-fe6b-5f85-0e910486d555") },
                    { new Guid("a6853a3b-07de-e827-435d-eb78f026f15a"), "speciality", new Guid("63e02118-3979-0a5d-0c3e-a4fb8ab1a42c") },
                    { new Guid("a7ff4931-e038-2f73-c319-2b9f3d4c531b"), "smoothies", new Guid("63e02118-3979-0a5d-0c3e-a4fb8ab1a42c") },
                    { new Guid("ad551a9e-4e95-c78b-565d-a39097e8958a"), "classic", new Guid("e265d8c3-7254-fe6b-5f85-0e910486d555") },
                    { new Guid("b850bbe7-8132-9610-5f59-a82217c4a8c0"), "classic", new Guid("d5e8a7b5-131c-2cab-996e-808bae8e91d8") },
                    { new Guid("b895d025-e0b6-3511-2dc7-0a210c3b99a8"), "frappe", new Guid("e265d8c3-7254-fe6b-5f85-0e910486d555") },
                    { new Guid("b9e6bcc7-7938-670c-dc43-798bd2f10269"), "mojito", new Guid("d5e8a7b5-131c-2cab-996e-808bae8e91d8") },
                    { new Guid("c1dd6865-8a4d-2a52-b507-cfdd34a2e5e2"), "noncoffee", new Guid("d5e8a7b5-131c-2cab-996e-808bae8e91d8") },
                    { new Guid("c8640a06-2084-4863-f086-4b4f28974833"), "shakes", new Guid("e265d8c3-7254-fe6b-5f85-0e910486d555") },
                    { new Guid("cfae01c1-89af-4c63-791e-855bc9b8f111"), "black", new Guid("e265d8c3-7254-fe6b-5f85-0e910486d555") },
                    { new Guid("d0ced5cb-d217-3777-d6cd-b46b3b0814b7"), "sigdrinks", new Guid("63e02118-3979-0a5d-0c3e-a4fb8ab1a42c") },
                    { new Guid("d4875678-6d85-40a1-2193-dd0ca7d604ce"), "speciality", new Guid("e265d8c3-7254-fe6b-5f85-0e910486d555") },
                    { new Guid("dea154f9-bbe8-fa1e-06ec-05c105dd430c"), "classic", new Guid("63e02118-3979-0a5d-0c3e-a4fb8ab1a42c") },
                    { new Guid("df9a7103-561c-12ab-c537-0b22ce8ae6b7"), "matcha", new Guid("d5e8a7b5-131c-2cab-996e-808bae8e91d8") },
                    { new Guid("f2fa2aa4-ec69-f6b1-6ab7-6e2fe6538887"), "iced", new Guid("d5e8a7b5-131c-2cab-996e-808bae8e91d8") },
                    { new Guid("f5e56e60-3bc4-9361-0e81-1ae8a41b5bcf"), "smoothies", new Guid("e265d8c3-7254-fe6b-5f85-0e910486d555") }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "CategorySlug", "Description", "DisplayOrder", "ImageUrl", "IsActive", "IsAvailable", "Name", "Price", "Slug" },
                values: new object[,]
                {
                    { new Guid("00f1b1ff-9df0-ef7a-e88e-70ca96d1e53d"), "pastries", "Cookies and cakes, baked in-house.", 5, null, true, true, "Double Chocolate Cake", 150m, "pa-doublechoccake" },
                    { new Guid("01a7ba5e-d729-7cf8-68fd-3960aaf5e63c"), "smoothies", "Fresh fruit, blended smooth.", 3, null, true, true, "Pinacolada", 135m, "sm-pinacolada" },
                    { new Guid("022b689a-2270-c376-f3b4-c0d38ce40dbe"), "classic", "Espresso-based, made the classic way.", 7, null, true, true, "White Mocha", 110m, "c-whitemocha" },
                    { new Guid("02c49fee-3263-2b47-a7de-dc72fa18d246"), "frappe", "Blended, creamy, and cold.", 6, null, true, true, "Pistachio", 155m, "f-pistachio" },
                    { new Guid("04d26418-d359-1857-6aa6-4d0f99a5d149"), "cheesecakes", "Pistachio cream and crushed nuts.", 1, null, true, true, "Pistachio Cheesecake", 175m, "ch-pistachio" },
                    { new Guid("04fb17ab-b9ee-c967-2865-e14d3c1dfb65"), "frappe", "Blended, creamy, and cold.", 11, null, true, true, "Spanish Latte", 145m, "f-spanish" },
                    { new Guid("05d805c8-77b0-ea98-ea4f-9769685353c5"), "shakes", "Thick, blended, and topped up.", 2, null, true, true, "Pistachio Shake", 155m, "sh-pistachio" },
                    { new Guid("05e1f23b-6dc5-799d-9eec-1f358d45841a"), "shakes", "Thick, blended, and topped up.", 1, null, true, true, "Double Chocolate Shake", 145m, "sh-doublechoc" },
                    { new Guid("06b6de7f-1186-7330-3201-d15698766898"), "breakfast", "Morning plates, all day long.", 3, null, true, true, "Croissant", 65m, "bf-croissant" },
                    { new Guid("07bc14c1-7c9b-1dfb-97aa-48cca9559f37"), "cheesecakes", "Baked cheesecake, blueberry compote.", 2, null, true, true, "Blueberry Cheesecake", 165m, "ch-blueberry" },
                    { new Guid("08810061-ab7b-0728-97ef-7cc85b3b635b"), "classic", "Espresso-based, made the classic way.", 6, null, true, true, "Mocha", 110m, "c-mocha" },
                    { new Guid("08fec8fd-d1fe-3b5c-b478-8edcbf4d3be8"), "sweetbakeries", "Filled and finished, straight from the oven.", 7, null, true, true, "Mixed Berries Tart", 120m, "sb-mixedberriestart" },
                    { new Guid("09aae977-dcb9-bd34-9e46-3e1dbc7ffa0f"), "classic", "Espresso-based, made the classic way.", 8, null, true, true, "Caramel Macchiato", 115m, "c-caramelmac" },
                    { new Guid("0a1a874e-fac4-e562-260f-5f57c35eb984"), "iced", "Chilled and ready for the corniche heat.", 3, null, true, true, "Iced Spanish Latte", 120m, "i-spanish" },
                    { new Guid("0abe1c0f-6db6-7af2-2c3b-b2e9e2f4811d"), "sweetbakeries", "Filled and finished, straight from the oven.", 4, null, true, true, "Signature Supreme", 120m, "sb-sigsupreme" },
                    { new Guid("0ba2d026-27b5-980a-5de4-e156d63ffc1a"), "sweetbakeries", "Filled and finished, straight from the oven.", 6, null, true, true, "Pain Suisse", 75m, "sb-painsuisse" },
                    { new Guid("0fa78c6c-f55c-4f39-ada6-0bed8ea61cfc"), "bakery", "Signature. Fresh croissants, baked daily.", 3, null, true, true, "Focaccia", 90m, "bk-focaccia" },
                    { new Guid("10dc0fc2-463c-7630-f57c-8d95ea5713de"), "cheesecakes", "Baked cheesecake, raspberry coulis.", 3, null, true, true, "Raspberry Cheesecake", 165m, "ch-raspberry" },
                    { new Guid("1305252b-5018-77e5-e3ec-6d2f1258ecff"), "black", "Bold, no milk, no fuss.", 0, null, true, true, "Espresso", 70m, "b-espresso" },
                    { new Guid("130de195-4aef-3eba-aff3-b792cf934553"), "bakery", "Fresh croissants, baked daily.", 2, null, true, true, "Labneh Croissant", 75m, "bk-labnehcroissant" },
                    { new Guid("138bc1d8-fd43-c781-66b9-ea726d5e0f46"), "sandwiches", "Sourdough / Tortilla.", 5, null, true, true, "Turkey", 150m, "sw-turkey" },
                    { new Guid("154804c8-e849-ef3a-c591-bdd7bbea593e"), "iced", "Chilled and ready for the corniche heat.", 15, null, true, true, "Iced Hazelnut Candy", 125m, "i-hazelnutcandy" },
                    { new Guid("199a2eec-f603-3951-5ee0-6ad5b1563891"), "sigdrinks", "CROWD originals — the ones only we make.", 3, null, true, true, "Cinnamon Spanish Latte", 125m, "sd-cinnamonspanish" },
                    { new Guid("1b7b8258-8427-5e95-8855-86041387feaa"), "sweetbakeries", "Filled and finished, straight from the oven.", 2, null, true, true, "Double Chocolate Supreme", 110m, "sb-doublechocsupreme" },
                    { new Guid("1b8d9629-688f-54e8-488c-fa340e21bb4c"), "classic", "Espresso-based, made the classic way.", 9, null, true, true, "French Coffee", 100m, "c-frenchcoffee" },
                    { new Guid("1c0a0e39-ca57-edaa-b3ba-148a0e36540c"), "sigdesserts", "House specialties from the pastry case.", 5, null, true, true, "Matcha Cookies", 60m, "sg-matchacookies" },
                    { new Guid("1cdf4fae-5fc0-0d3e-41eb-868da6f29c79"), "sweetbakeries", "Filled and finished, straight from the oven.", 1, null, true, true, "Pistachio Flat Croissant", 100m, "sb-pistachioflat" },
                    { new Guid("1e7e24f4-285d-427e-f76f-bdc737336e54"), "sigdrinks", "CROWD originals — the ones only we make.", 9, null, true, true, "Iced Orange Caramel Macchiato", 130m, "sd-icedorangecaramelmac" },
                    { new Guid("1eb661aa-002f-ccd9-d42d-b3ff9a97d82f"), "frappe", "Blended, creamy, and cold.", 5, null, true, true, "Vanilla", 130m, "f-vanilla" },
                    { new Guid("24dded1d-6307-abea-f5c8-33e842f30644"), "frappe", "Blended, creamy, and cold.", 3, null, true, true, "Caramel", 140m, "f-caramel" },
                    { new Guid("262620ea-731e-7d8d-a6cd-09f14e7a91d4"), "frappe", "Blended, creamy, and cold.", 12, null, true, true, "Hazelnut Candy", 150m, "f-hazelnutcandy" },
                    { new Guid("278b8875-3373-256b-1f70-2e557ca8f52c"), "sigdesserts", "House specialties from the pastry case.", 1, null, true, true, "Chocolate Tiramisu", 145m, "sg-chocolatetiramisu" },
                    { new Guid("294024f2-9cdc-03e6-39a5-53a194ac0ebd"), "black", "Bold, no milk, no fuss.", 2, null, true, true, "Turkish Coffee", 75m, "b-turkish" },
                    { new Guid("29aab68d-9cdb-f4e9-48bc-16b581ef0e33"), "sweetbakeries", "Filled and finished, straight from the oven.", 5, null, true, true, "Pain Au Chocolate", 75m, "sb-painauchocolate" },
                    { new Guid("29ebd52f-70c3-af68-50da-33c0351527cd"), "iced", "Chilled and ready for the corniche heat.", 11, null, true, true, "Iced Salted Brownies", 130m, "i-saltedbrownies" },
                    { new Guid("2ac93995-e9dc-86d4-bd36-1f0093251573"), "sandwiches", "Baguette / Sourdough / Tortilla.", 3, null, true, true, "Mixed Cheese", 130m, "sw-mixedcheese" },
                    { new Guid("32cf2cae-2f4b-f5ec-9e66-df250370bf1c"), "speciality", "Manual brew methods, made to order.", 0, null, true, true, "V60 / Origami", 130m, "s-v60" },
                    { new Guid("33718a23-bf0e-5a85-76ab-f70036c70ff1"), "cheesecakes", "Lotus biscuit crust and caramel drizzle.", 5, null, true, true, "Biscoff Lotus Cheesecake", 175m, "ch-biscoff" },
                    { new Guid("337377b8-0263-cfc1-1829-345cb7e80e44"), "mojito", "Fruit and soda over ice, alcohol-free.", 3, null, true, true, "Pinacolada", 145m, "mj-pinacolada" },
                    { new Guid("352aa13f-27f3-4381-94c9-f0e9a2d88d51"), "sigdesserts", "House specialties from the pastry case.", 6, null, true, true, "San Sebastian", 160m, "sg-sansebastian" },
                    { new Guid("37b7a3c2-99de-a58d-b112-8e94693630f6"), "sigdrinks", "CROWD originals — the ones only we make.", 5, null, true, true, "Strawberry Hot Choco", 125m, "sd-strawberryhotchoco" },
                    { new Guid("3c075dce-4098-a0f2-c41f-c88f05e7c463"), "sigdrinks", "CROWD originals — the ones only we make.", 10, null, true, true, "Peach Coconut White Mocha", 135m, "sd-peachcoconutmocha" },
                    { new Guid("3c7a0fb2-60d1-349a-66f1-7e24e4b4d7d1"), "matcha", "Ceremonial-grade matcha, hot or iced.", 3, null, true, true, "Iced Matcha", 125m, "m-iced" },
                    { new Guid("3dd67cc9-e8ef-87d2-672e-3ad725602329"), "pastries", "Cookies and cakes, baked in-house.", 1, null, true, true, "Pistachio Cookie Pie", 95m, "pa-pistachiocookiepie" },
                    { new Guid("418d9eb9-4c9a-2d8e-fa80-06d77e26a59e"), "noncoffee", "For the non-coffee crowd.", 4, null, true, true, "Classic Chocolate", 90m, "n-classicchoc" },
                    { new Guid("42052d6c-85d5-02bf-8d02-f53883f55227"), "sigdrinks", "CROWD originals — the ones only we make.", 2, null, true, true, "Pineapple White Mocha", 130m, "sd-pineapplewhitemocha" },
                    { new Guid("424add57-9d68-a3af-8766-e2c4497c0ce9"), "frappe", "Blended, creamy, and cold.", 0, null, true, true, "Classic", 130m, "f-classic" },
                    { new Guid("42629c06-a5c4-86e4-d580-9df49d114e9e"), "pastries", "Cookies and cakes, baked in-house.", 3, null, true, true, "Double Chocolate Cookies", 55m, "pa-doublechoccookies" },
                    { new Guid("4297ac8a-e428-ab5c-b412-8a1f2d272b0a"), "frappe", "Blended, creamy, and cold.", 1, null, true, true, "Mocha", 140m, "f-mocha" },
                    { new Guid("450fd013-deef-9b2a-cd8c-39ec93b5fff6"), "matcha", "Ceremonial-grade matcha, hot or iced.", 1, null, true, true, "Hot White Matcha", 125m, "m-hotwhite" },
                    { new Guid("466ae2f4-349e-a4bf-236a-9f0b3ba1429c"), "shakes", "Thick, blended, and topped up.", 0, null, true, true, "Birthday Cake", 145m, "sh-birthdaycake" },
                    { new Guid("46d8cbf7-c858-b22a-a4e8-988c7d20a185"), "iced", "Chilled and ready for the corniche heat.", 0, null, true, true, "Iced Latte", 110m, "i-latte" },
                    { new Guid("4a57241d-03b7-cc22-e76f-3f97c5bc9c70"), "sandwiches", "Signature. Baguette / Sourdough / Tortilla.", 2, null, true, true, "Sourdough Halloumi", 140m, "sw-sourdoughhalloumi" },
                    { new Guid("4bac157b-c2d7-1b8d-e030-167f04b4ce3e"), "bakery", "Fresh croissants, baked daily.", 1, null, true, true, "Thyme Croissant", 70m, "bk-thymecroissant" },
                    { new Guid("4bfd1595-5c56-4561-ec29-59e197f05c3d"), "mojito", "Fruit and soda over ice, alcohol-free.", 4, null, true, true, "Mango Passion", 145m, "mj-mangopassion" },
                    { new Guid("4e055d30-715e-547c-8fa7-33cb8d3206a4"), "iced", "Chilled and ready for the corniche heat.", 7, null, true, true, "Iced White Mocha", 120m, "i-whitemocha" },
                    { new Guid("4f7f3b34-4c12-5208-2c1b-dd91d02a37d1"), "sigdesserts", "House specialties from the pastry case.", 0, null, true, true, "Classic Tiramisu", 140m, "sg-classictiramisu" },
                    { new Guid("4f8cc1f0-f7ae-910e-79ec-447b134c1905"), "cheesecakes", "Baked cheesecake, fresh strawberry topping.", 4, null, true, true, "Strawberry Cheesecake", 165m, "ch-strawberry" },
                    { new Guid("50816f26-15d0-f9e0-16c2-842d774109ba"), "breakfast", "Signature. Morning plates, all day long.", 5, null, true, true, "Crowd's Omelette Platter", 160m, "bf-omelette" },
                    { new Guid("526fac04-3759-c40a-90a7-1fbc335f0e4f"), "matcha", "Ceremonial-grade matcha, hot or iced.", 5, null, true, true, "Mango Matcha", 135m, "m-mango" },
                    { new Guid("53c6f524-b059-896e-8774-606328e254d4"), "sigdrinks", "CROWD originals — the ones only we make.", 12, null, true, true, "Coconut Matcha", 135m, "sd-coconutmatcha" },
                    { new Guid("555ed859-8fff-815a-5845-72b460dcf520"), "shakes", "Thick, blended, and topped up.", 7, null, true, true, "Biscoff Shake", 155m, "sh-biscoff" },
                    { new Guid("55c3f0b0-db87-ce52-a4ce-cac5e8447c45"), "noncoffee", "For the non-coffee crowd.", 8, null, true, true, "Decaf", 80m, "n-decaf" },
                    { new Guid("599e76f7-2625-ce16-743d-5c950d9eb30d"), "frappe", "Blended, creamy, and cold.", 13, null, true, true, "Cinnamon", 135m, "f-cinnamon" },
                    { new Guid("5a9a3f46-1893-60d6-87a8-4031e0e5e1e9"), "classic", "Espresso-based, made the classic way.", 5, null, true, true, "Latte", 100m, "c-latte" },
                    { new Guid("5c7a134f-02a8-a936-593f-51892330a924"), "noncoffee", "For the non-coffee crowd.", 1, null, true, true, "Herbs", 60m, "n-herbs" },
                    { new Guid("5db004e9-0833-44cd-7c77-094d8070c797"), "noncoffee", "For the non-coffee crowd.", 0, null, true, true, "Tea / Green Tea", 60m, "n-tea" },
                    { new Guid("5e987062-311a-7f0a-09ed-5c4702cc4218"), "iced", "Chilled and ready for the corniche heat.", 4, null, true, true, "Iced Salted Caramel", 125m, "i-saltedcaramel" },
                    { new Guid("619c4364-8a0b-0f63-7d3c-68434251215b"), "frappe", "Blended, creamy, and cold.", 8, null, true, true, "Coconut", 140m, "f-coconut" },
                    { new Guid("61cb95e7-f9aa-643d-9be0-fea1f36a8f03"), "speciality", "Manual brew methods, made to order.", 1, null, true, true, "Aeropress", 130m, "s-aeropress" },
                    { new Guid("624d92a3-0371-07cd-f9ae-2b445d314483"), "classic", "Espresso-based, made the classic way.", 1, null, true, true, "Macchiato", 90m, "c-macchiato" },
                    { new Guid("63f4ee80-50e0-a35a-3ddd-d099870fc1c8"), "matcha", "Ceremonial-grade matcha, hot or iced.", 8, null, true, true, "Spanish Matcha", 130m, "m-spanish" },
                    { new Guid("6434d36e-7f0a-93c1-4000-003284d632e0"), "iced", "Chilled and ready for the corniche heat.", 12, null, true, true, "Iced Coconut", 120m, "i-coconut" },
                    { new Guid("666968aa-dc3d-9000-1726-4a9d3b167eb6"), "sigdrinks", "CROWD originals — the ones only we make.", 0, null, true, true, "Hazelnut Candy Spanish Latte", 130m, "sd-hazelnutcandyspanish" },
                    { new Guid("66ca4802-6065-d2a5-dc8b-2c8e77859d93"), "smoothies", "Fresh fruit, blended smooth.", 4, null, true, true, "Mango Passion", 135m, "sm-mangopassion" },
                    { new Guid("66e56570-5282-5cab-47a9-7fe7d4e03aea"), "noncoffee", "For the non-coffee crowd.", 5, null, true, true, "Hot Dark Chocolate", 100m, "n-hotdarkchoc" },
                    { new Guid("66f16756-25a7-a482-aec8-4e41b883f0f7"), "bakery", "Fresh croissants, baked daily.", 0, null, true, true, "Plain Croissant", 60m, "bk-plaincroissant" },
                    { new Guid("691f142a-01fa-4e4c-6f8e-60a03bf52f72"), "pastries", "Cookies and cakes, baked in-house.", 0, null, true, true, "Double Chocolate Cookie Pie", 90m, "pa-doublechoccookiepie" },
                    { new Guid("6c10e7bb-5882-4216-1176-093838fb21a0"), "iced", "Chilled and ready for the corniche heat.", 14, null, true, true, "Iced Orange Caramel", 125m, "i-orangecaramel" },
                    { new Guid("6c6b36cf-7f79-0d6e-e9f8-7a786e7ede9a"), "classic", "Espresso-based, made the classic way.", 2, null, true, true, "Cortado", 95m, "c-cortado" },
                    { new Guid("79af3fff-04be-7b14-459e-5e64f89a2c40"), "mojito", "Fruit and soda over ice, alcohol-free.", 5, null, true, true, "Mix Orange", 135m, "mj-mixorange" },
                    { new Guid("7c5d9f60-65e6-12d8-7f41-41a6d60e94e0"), "frappe", "Blended, creamy, and cold.", 4, null, true, true, "Salted Caramel", 145m, "f-saltedcaramel" },
                    { new Guid("7dcd2ffd-8719-7f03-4f39-bb216bfa9a7c"), "sandwiches", "Signature. Baguette / Tortilla.", 6, null, true, true, "Avocado Shrimp", 195m, "sw-avocadoshrimp" },
                    { new Guid("7ea97bc6-57a6-8c5e-4a58-c82a98e0d287"), "iced", "Chilled and ready for the corniche heat.", 2, null, true, true, "Iced Cappuccino", 115m, "i-cappuccino" },
                    { new Guid("7f7b5561-40e3-df57-0620-e94252c558ca"), "noncoffee", "For the non-coffee crowd.", 2, null, true, true, "Flavored Tea", 65m, "n-flavoredtea" },
                    { new Guid("8463e7f9-9466-93fb-c8bb-93f37bb24c73"), "black", "Bold, no milk, no fuss.", 3, null, true, true, "Freddo Espresso", 90m, "b-freddo" },
                    { new Guid("8576c153-6cb3-f630-59e6-d7f25af4340a"), "iced", "Chilled and ready for the corniche heat.", 5, null, true, true, "Iced Caramel Macchiato", 125m, "i-caramelmac" },
                    { new Guid("86259620-7526-90b8-ecec-6154015f6d9c"), "noncoffee", "For the non-coffee crowd.", 3, null, true, true, "Karak", 65m, "n-karak" },
                    { new Guid("8a59f630-3a30-3389-580f-0113f53fe107"), "classic", "Espresso-based, made the classic way.", 3, null, true, true, "Flat White", 105m, "c-flatwhite" },
                    { new Guid("8c94f6d4-03d2-ff4d-8aee-cf2650a8ec5f"), "classic", "Espresso-based, made the classic way.", 11, null, true, true, "Spanish Latte", 110m, "c-spanish" },
                    { new Guid("8d896a6f-c7da-6d07-7fa6-7bb8d96583e2"), "mojito", "Fruit and soda over ice, alcohol-free.", 1, null, true, true, "Red Beach", 140m, "mj-redbeach" },
                    { new Guid("8e210250-5600-91c3-9897-743a36b29cca"), "classic", "Espresso-based, made the classic way.", 4, null, true, true, "Cappuccino", 100m, "c-cappuccino" },
                    { new Guid("8fccd31d-7853-dc2e-14aa-3b5f21d71491"), "breakfast", "Morning plates, all day long.", 7, null, true, true, "Club Sandwich", 145m, "bf-clubsandwich" },
                    { new Guid("9043baa0-9ded-5c58-e46e-8911d1b06f8f"), "iced", "Chilled and ready for the corniche heat.", 13, null, true, true, "Iced Orange Choco", 125m, "i-orangechoco" },
                    { new Guid("91bd7658-2d2d-959e-54bf-f1be44c04558"), "cheesecakes", "Baked cheesecake, tangy lemon curd.", 7, null, true, true, "Lemon Cheesecake", 160m, "ch-lemon" },
                    { new Guid("930dc847-65b0-c531-238c-a6132fdeef10"), "sandwiches", "Signature. Baguette / Sourdough / Tortilla.", 4, null, true, true, "Salmon Avocado", 190m, "sw-salmonavocado" },
                    { new Guid("932f00f9-0c78-a6ee-3c96-d05f89d4cf46"), "mojito", "Fruit and soda over ice, alcohol-free.", 0, null, true, true, "Green Ocean", 140m, "mj-greenocean" },
                    { new Guid("93e32623-b5b5-cad9-0d1e-c2b532a33bfa"), "smoothies", "Fresh fruit, blended smooth.", 0, null, true, true, "Green Ocean", 130m, "sm-greenocean" },
                    { new Guid("94360a43-1b51-bd4a-9704-6c02ae4287ad"), "iced", "Chilled and ready for the corniche heat.", 10, null, true, true, "Iced Pistachio", 130m, "i-pistachio" },
                    { new Guid("9455dcf8-c4dc-b9e9-ccb4-00c56fd4a409"), "frappe", "Blended, creamy, and cold.", 9, null, true, true, "Orange Choco", 145m, "f-orangechoco" },
                    { new Guid("964467f3-0aab-4711-71e4-4c5b55d228da"), "speciality", "Manual brew methods, made to order.", 2, null, true, true, "Chemix", 140m, "s-chemix" },
                    { new Guid("99999bea-21bb-a65a-8051-96670fc84a9f"), "smoothies", "Fresh fruit, blended smooth.", 6, null, true, true, "Strawberry", 130m, "sm-strawberry" },
                    { new Guid("9aae9a02-61a0-60d8-f717-5edf34a7770e"), "cheesecakes", "Swirled chocolate and vanilla cheesecake.", 8, null, true, true, "Marble Cheesecake", 165m, "ch-marble" },
                    { new Guid("9cbc8042-22f0-7a51-c356-ea540af81c06"), "sweetbakeries", "Filled and finished, straight from the oven.", 0, null, true, true, "Double Chocolate Flat Croissant", 95m, "sb-doublechocflat" },
                    { new Guid("9e23d93d-a5f8-2120-388b-1e2f07890d99"), "sandwiches", "Toast / Baguette / Tortilla.", 1, null, true, true, "Pesto Chicken", 150m, "sw-pestochicken" },
                    { new Guid("9ea09aae-cf7b-dc05-3405-4498c7ea21f1"), "noncoffee", "For the non-coffee crowd.", 6, null, true, true, "Hot White Chocolate", 100m, "n-hotwhitechoc" },
                    { new Guid("a1f1a69a-2eb3-ace5-e305-72226f276a38"), "sigdrinks", "CROWD originals — the ones only we make.", 11, null, true, true, "Gingersnap Mocha", 130m, "sd-gingersnapmocha" },
                    { new Guid("a2bac40d-e128-ef3e-7292-9d42dc08b05e"), "shakes", "Thick, blended, and topped up.", 6, null, true, true, "Salted Brownies", 150m, "sh-saltedbrownies" },
                    { new Guid("a3453e76-b17d-65a1-9620-f72e931a2e77"), "sigdrinks", "CROWD originals — the ones only we make.", 6, null, true, true, "Cinnamon Hot Choco", 120m, "sd-cinnamonhotchoco" },
                    { new Guid("a45914b8-d639-c91a-d3df-78eddaabd729"), "smoothies", "Fresh fruit, blended smooth.", 5, null, true, true, "Mix Orange", 125m, "sm-mixorange" },
                    { new Guid("a46137b0-1296-fe42-9faa-54ed7dad563b"), "matcha", "Ceremonial-grade matcha, hot or iced.", 6, null, true, true, "Mixed Berries Matcha", 135m, "m-mixedberries" },
                    { new Guid("a4a441e8-51f1-8238-ad2d-d7e5e578f904"), "breakfast", "Morning plates, all day long.", 1, null, true, true, "Roastbeef Croissant", 120m, "bf-roastbeefcroissant" },
                    { new Guid("a6b5823a-825c-aca5-a032-303a24e78c1b"), "breakfast", "Morning plates, all day long.", 0, null, true, true, "Turkey Croissant", 110m, "bf-turkeycroissant" },
                    { new Guid("a92014be-83ec-00d4-eb1e-9ef2367e1686"), "pastries", "Cookies and cakes, baked in-house.", 2, null, true, true, "Hazelnut Creme Cookie Pie", 95m, "pa-hazelnutcremecookiepie" },
                    { new Guid("ab60267e-14a2-196a-7602-a6511dc1192b"), "matcha", "Ceremonial-grade matcha, hot or iced.", 0, null, true, true, "Hot Matcha", 120m, "m-hot" },
                    { new Guid("af7522c9-ccc5-2b49-3f30-c59d0dc76e82"), "iced", "Chilled and ready for the corniche heat.", 1, null, true, true, "Iced Americano", 95m, "i-americano" },
                    { new Guid("afda94b7-1829-3433-95ea-54090972c629"), "iced", "Chilled and ready for the corniche heat.", 9, null, true, true, "Iced Vanilla", 110m, "i-vanilla" },
                    { new Guid("affc6753-abcf-0c4c-aedc-a870334be97b"), "speciality", "Manual brew methods, made to order.", 3, null, true, true, "French Press", 130m, "s-frenchpress" },
                    { new Guid("b0c49e1b-7a21-beac-453c-3664d8058dc8"), "sandwiches", "Signature. Baguette / Sourdough / Tortilla.", 0, null, true, true, "Roastbeef", 165m, "sw-roastbeef" },
                    { new Guid("b2a94c78-2412-608c-ab2b-4825e9c1c235"), "sigdrinks", "CROWD originals — the ones only we make.", 13, null, true, true, "Vanilla Cinnamon White Mocha", 130m, "sd-vanillacinnamonmocha" },
                    { new Guid("b3056555-6460-e7da-4917-60f7fb2056b6"), "sigdrinks", "CROWD originals — the ones only we make.", 1, null, true, true, "White Raspberry", 130m, "sd-whiteraspberry" },
                    { new Guid("b33a2b62-7104-24dd-46a9-35cb7350d9a3"), "sigdesserts", "House specialties from the pastry case.", 3, null, true, true, "Lemon Tart", 130m, "sg-lemontart" },
                    { new Guid("b3da0d0b-c684-92a8-7faa-c0a65d83cb7e"), "cheesecakes", "Burnt Basque-style cheesecake.", 0, null, true, true, "San Sebastian", 160m, "ch-sanseb" },
                    { new Guid("b454161a-d614-5ddd-be0f-65ba387e495e"), "breakfast", "Morning plates, all day long.", 4, null, true, true, "Mini Pizza", 90m, "bf-minipizza" },
                    { new Guid("b57ae714-baf8-1062-493e-2f9d6b4a2cf7"), "salads", "Fresh and hearty, for the health-conscious.", 2, null, true, true, "Western Salad", 150m, "sl-western" },
                    { new Guid("b70629b9-1026-a4b9-7b9e-2223716dc68a"), "classic", "Espresso-based, made the classic way.", 0, null, true, true, "Espresso Bonbon", 95m, "c-bonbon" },
                    { new Guid("b77f3b3e-368c-f43f-deb4-0eebe21545e2"), "cheesecakes", "Baked cheesecake, cherry topping.", 10, null, true, true, "Cherry Cheesecake", 165m, "ch-cherry" },
                    { new Guid("c1152741-cbe3-a3bc-1567-63cb3ab50b79"), "sigdesserts", "House specialties from the pastry case.", 4, null, true, true, "E'clair", 90m, "sg-eclair" },
                    { new Guid("c305d0cc-d5fc-f892-5c51-d5fe608c141c"), "iced", "Chilled and ready for the corniche heat.", 6, null, true, true, "Iced Mocha", 120m, "i-mocha" },
                    { new Guid("c37487b4-e94d-7b12-ff5c-10db56905ff4"), "iced", "Chilled and ready for the corniche heat.", 8, null, true, true, "Iced Caramel", 115m, "i-caramel" },
                    { new Guid("c6dda0a6-5cd5-b62d-95f9-b59557ca5579"), "salads", "Fresh and hearty, for the health-conscious.", 0, null, true, true, "Chicken Ceaser Salad", 160m, "sl-chickencaesar" },
                    { new Guid("c794702b-bc98-f0af-98d6-e8d9d93f0c9c"), "shakes", "Thick, blended, and topped up.", 5, null, true, true, "Crowd Shake", 155m, "sh-crowdshake" },
                    { new Guid("c85afe10-4b9f-37eb-88ec-002993660f9e"), "frappe", "Blended, creamy, and cold.", 2, null, true, true, "White Mocha", 140m, "f-whitemocha" },
                    { new Guid("cd2d19bd-9c77-0cab-1b47-76bb59ee45b2"), "cheesecakes", "Oreo crust, chocolate cheesecake filling.", 6, null, true, true, "Oreo Cheesecake", 170m, "ch-oreo" },
                    { new Guid("d30cf99d-79b5-68a6-1a1c-79c08b7c7f3c"), "smoothies", "Fresh fruit, blended smooth.", 1, null, true, true, "Red Beach", 130m, "sm-redbeach" },
                    { new Guid("d3b43a6b-eae0-b367-9d45-58d2756bf394"), "noncoffee", "For the non-coffee crowd.", 7, null, true, true, "Hot Hazelnut Chocolate", 105m, "n-hothazelnutchoc" },
                    { new Guid("d52dee05-b966-54b1-81b0-f54da2e343aa"), "mojito", "Fruit and soda over ice, alcohol-free.", 6, null, true, true, "Strawberry", 140m, "mj-strawberry" },
                    { new Guid("d76b62c9-1fde-859d-2633-e718d2cea3f5"), "matcha", "Ceremonial-grade matcha, hot or iced.", 2, null, true, true, "Hot Coconut Matcha", 125m, "m-hotcoconut" },
                    { new Guid("d77e3311-664c-e9f7-57e3-b574893a37fa"), "matcha", "Ceremonial-grade matcha, hot or iced.", 10, null, true, true, "Hazelnut Matcha", 130m, "m-hazelnut" },
                    { new Guid("d7d2c163-99d8-1a86-9cbb-02ccab5b703c"), "classic", "Espresso-based, made the classic way.", 10, null, true, true, "Hazelnut Coffee", 105m, "c-hazelnut" },
                    { new Guid("d7f4906b-8ac7-5688-eab2-a2e196fca05e"), "mojito", "Fruit and soda over ice, alcohol-free.", 2, null, true, true, "Orange Sky (Sunset)", 140m, "mj-orangesky" },
                    { new Guid("d8069e43-26ae-d3ce-64ab-d060a0b88730"), "shakes", "Thick, blended, and topped up.", 4, null, true, true, "Cashew Shake", 150m, "sh-cashew" },
                    { new Guid("d89d0368-b666-3c5b-0e0f-2415b3c812f9"), "sweetbakeries", "Filled and finished, straight from the oven.", 3, null, true, true, "Pistachio Supreme", 115m, "sb-pistachiosupreme" },
                    { new Guid("dad0a602-60e3-34f7-fdd7-e75903bf1cd4"), "cheesecakes", "Baked cheesecake, mixed berry compote.", 9, null, true, true, "Mixed Berry Cheesecake", 170m, "ch-mixedberry" },
                    { new Guid("dae49123-9199-3b0f-2d68-ae19e4a70ed5"), "black", "Bold, no milk, no fuss.", 1, null, true, true, "Americano", 80m, "b-americano" },
                    { new Guid("db23d038-b11e-10b0-f9e9-5c6b55d7f7f6"), "breakfast", "Morning plates, all day long.", 2, null, true, true, "Halloumi Croissant", 100m, "bf-halloumicroissant" },
                    { new Guid("dd14c586-050b-ad06-cab2-16f3e95ed152"), "salads", "Fresh and hearty, for the health-conscious.", 3, null, true, true, "Omega 3", 175m, "sl-omega3" },
                    { new Guid("df5a4f25-6b01-de17-987d-5dd9c40dad6e"), "sigdrinks", "CROWD originals — the ones only we make.", 8, null, true, true, "Crowd Blend", 140m, "sd-crowdblend" },
                    { new Guid("e2571ac9-ec6d-2575-73b6-dc401b45342b"), "sigdrinks", "CROWD originals — the ones only we make.", 7, null, true, true, "Affogato", 135m, "sd-affogato" },
                    { new Guid("e8396477-391e-be29-41c3-d338454a0405"), "sigdrinks", "CROWD originals — the ones only we make.", 4, null, true, true, "Pumpkin Spiced Latte (Iced or Shaked)", 135m, "sd-pumpkinspiced" },
                    { new Guid("ea0a0e5e-8cda-9fea-2362-a92601d72818"), "frappe", "Blended, creamy, and cold.", 10, null, true, true, "Orange Caramel", 145m, "f-orangecaramel" },
                    { new Guid("eb6d2a95-17d7-4e46-585b-115b5aabcdb3"), "shakes", "Thick, blended, and topped up.", 3, null, true, true, "Strawberry", 135m, "sh-strawberry" },
                    { new Guid("ec50b1a1-98b7-7309-a9b7-c9765984f8f2"), "pastries", "Cookies and cakes, baked in-house.", 4, null, true, true, "Chocolate Chips Cookies", 50m, "pa-chocchipcookies" },
                    { new Guid("ee1b42c8-556e-4a54-dd14-f99ee5eb7ebb"), "salads", "Fresh and hearty, for the health-conscious.", 1, null, true, true, "Cajun BBQ Chicken Salad", 170m, "sl-cajunbbq" },
                    { new Guid("f37318d1-9e5a-4bc2-87f6-03e8a77b24ae"), "sigdesserts", "House specialties from the pastry case.", 2, null, true, true, "Dark Belgium Cherry Mousse", 150m, "sg-darkcherrymousse" },
                    { new Guid("f982d1c7-195b-3df6-e5bc-3ca4babd6b55"), "breakfast", "Signature. Morning plates, all day long.", 6, null, true, true, "Crowd's Scrambled Eggs", 140m, "bf-scrambledeggs" },
                    { new Guid("fa17cf93-ac33-30dc-d6b6-936427adab0d"), "matcha", "Ceremonial-grade matcha, hot or iced.", 9, null, true, true, "Vanilla Matcha", 125m, "m-vanilla" },
                    { new Guid("fa570b86-f56f-7fee-e110-5f9b4435766c"), "matcha", "Ceremonial-grade matcha, hot or iced.", 4, null, true, true, "Strawberry Matcha", 135m, "m-strawberry" },
                    { new Guid("fd3ca24f-6029-4aa2-d370-1c1fe9406e06"), "matcha", "Ceremonial-grade matcha, hot or iced.", 7, null, true, true, "Salted Caramel Matcha", 140m, "m-saltedcaramel" },
                    { new Guid("fe21464d-e879-13d9-fa68-2b855d669b10"), "smoothies", "Fresh fruit, blended smooth.", 2, null, true, true, "Orange Sky (Sunset)", 130m, "sm-orangesky" },
                    { new Guid("ff70f19f-ec24-dc02-1e06-65a69518d426"), "frappe", "Blended, creamy, and cold.", 7, null, true, true, "Salted Brownies", 150m, "f-saltedbrownies" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModifierGroups_Slug",
                table: "ModifierGroups",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModifierOptions_ModifierGroupId_Slug",
                table: "ModifierOptions",
                columns: new[] { "ModifierGroupId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemModifiers_ModifierOptionId",
                table: "OrderItemModifiers",
                column: "ModifierOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemModifiers_OrderItemId",
                table: "OrderItemModifiers",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductId",
                table: "OrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_IdempotencyKey",
                table: "Orders",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNumber",
                table: "Orders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusHistories_OrderId",
                table: "OrderStatusHistories",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductModifierGroups_CategorySlug_ModifierGroupId",
                table: "ProductModifierGroups",
                columns: new[] { "CategorySlug", "ModifierGroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductModifierGroups_ModifierGroupId",
                table: "ProductModifierGroups",
                column: "ModifierGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategorySlug",
                table: "Products",
                column: "CategorySlug");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Slug",
                table: "Products",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_ModifierOptionId",
                table: "Recipes",
                column: "ModifierOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_ProductId",
                table: "Recipes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_StockItemId",
                table: "Recipes",
                column: "StockItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffUsers_Username",
                table: "StaffUsers",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockItems_Name",
                table: "StockItems",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_StockItemId_CreatedAt",
                table: "StockMovements",
                columns: new[] { "StockItemId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItemModifiers");

            migrationBuilder.DropTable(
                name: "OrderStatusHistories");

            migrationBuilder.DropTable(
                name: "ProductModifierGroups");

            migrationBuilder.DropTable(
                name: "Recipes");

            migrationBuilder.DropTable(
                name: "StaffUsers");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "ModifierOptions");

            migrationBuilder.DropTable(
                name: "StockItems");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "ModifierGroups");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
