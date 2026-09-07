using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BaselineCurrentSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 该迁移仅用于建立 EF Core 的当前模型基线。
            // 真实 Oracle 表已存在，因此这里不再执行任何 DDL。
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 基线迁移不回滚真实数据库结构。
        }
    }
}
