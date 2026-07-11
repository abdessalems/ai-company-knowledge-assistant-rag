using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIKnowledgeAssistant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "User's email address - used for login"),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Bcrypt hashed password - never plaintext"),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "User's first name"),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "User's last name"),
                    Role = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "User's authorization level: 0=User, 1=Admin"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'", comment: "When user account was created (UTC)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Question = table.Column<string>(type: "TEXT", nullable: false, comment: "Question asked by user"),
                    Answer = table.Column<string>(type: "TEXT", nullable: false, comment: "AI-generated answer"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'", comment: "When this Q&A occurred"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "When answer was last updated (if corrected)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Conversations_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, comment: "Original filename as uploaded by user"),
                    FilePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, comment: "Full path where file is stored (S3, disk, etc)"),
                    FileType = table.Column<int>(type: "integer", nullable: false, comment: "File type: 0=Pdf, 1=Docx, 2=Doc, 3=Txt"),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'", comment: "When document was uploaded"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Document owner (Foreign key to Users)"),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false, comment: "File size in bytes")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Hashed refresh token"),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When this token expires"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'", comment: "When token was issued"),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "When token was revoked (if applicable)"),
                    RevocationReason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Why was token revoked?"),
                    DeviceIdentifier = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Device that created this token")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentChunks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false, comment: "Text content of this chunk"),
                    PageNumber = table.Column<int>(type: "integer", nullable: false, comment: "Original page number in document"),
                    ChunkIndex = table.Column<int>(type: "integer", nullable: false, comment: "Sequential index of chunk within document"),
                    VectorEmbedding = table.Column<string>(type: "jsonb", nullable: true, comment: "Vector embedding for semantic similarity search (JSON array format)"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'", comment: "When chunk was created")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentChunks_Documents",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessageSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentChunkId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, comment: "Document name (denormalized for performance)"),
                    PageNumber = table.Column<int>(type: "integer", nullable: false, comment: "Page number in source document"),
                    RelevanceScore = table.Column<float>(type: "real", nullable: false, comment: "Similarity score: 0.0 to 1.0"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'", comment: "When this source was cited")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageSources_Conversations",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessageSources_DocumentChunks_DocumentChunkId",
                        column: x => x.DocumentChunkId,
                        principalTable: "DocumentChunks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_UserId",
                table: "Conversations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_DocumentId",
                table: "DocumentChunks",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_VectorEmbedding",
                table: "DocumentChunks",
                column: "VectorEmbedding")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_UserId",
                table: "Documents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageSources_ConversationId",
                table: "MessageSources",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageSources_DocumentChunkId",
                table: "MessageSources",
                column: "DocumentChunkId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpirationDate",
                table: "RefreshTokens",
                column: "ExpirationDate");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token_RevokedAt",
                table: "RefreshTokens",
                columns: new[] { "Token", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email_Unique",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MessageSources");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "Conversations");

            migrationBuilder.DropTable(
                name: "DocumentChunks");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
