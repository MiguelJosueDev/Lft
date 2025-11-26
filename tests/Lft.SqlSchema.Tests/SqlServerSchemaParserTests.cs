using FluentAssertions;
using Lft.SqlSchema;

namespace Lft.SqlSchema.Tests;

public class SqlServerSchemaParserTests
{
    private readonly SqlServerSchemaParser _parser = new();

    [Fact]
    public void Parse_CreateTable_ShouldReturnColumns()
    {
        var sql = @"CREATE TABLE [dbo].[Users](
            [Id] INT IDENTITY(1,1) PRIMARY KEY,
            [UserName] NVARCHAR(100) NOT NULL,
            [IsActive] BIT NOT NULL,
            [CreatedAt] DATETIME2 NULL
        );";

        var result = _parser.Parse(sql);

        result.Kind.Should().Be(SqlObjectKind.Table);
        result.SchemaName.Should().Be("dbo");
        result.Name.Should().Be("Users");
        result.Columns.Should().HaveCount(4);
        var idColumn = result.Columns.Single(c => c.Name == "Id");
        idColumn.IsIdentity.Should().BeTrue();
        idColumn.IsPrimaryKey.Should().BeTrue();
        idColumn.IsNullable.Should().BeFalse();
        var userNameColumn = result.Columns.Single(c => c.Name == "UserName");
        userNameColumn.SqlType.Should().Be("NVARCHAR(100)");
        userNameColumn.MaxLength.Should().Be(100);
        userNameColumn.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void Parse_CreateView_ShouldMarkAsView()
    {
        var sql = @"CREATE VIEW [dbo].[UsersView] AS SELECT Id, UserName FROM Users;";

        var result = _parser.Parse(sql);

        result.Kind.Should().Be(SqlObjectKind.View);
        result.SchemaName.Should().Be("dbo");
        result.Name.Should().Be("UsersView");
        result.Columns.Should().Contain(c => c.Name == "Id");
        result.Columns.Should().Contain(c => c.Name == "UserName");
    }

    [Fact]
    public void Parse_CreateTable_WithAlterAddColumns_ShouldMergeColumns()
    {
        var sql = @"CREATE TABLE dbo.Users ( Id INT PRIMARY KEY );
ALTER TABLE dbo.Users ADD UserName nvarchar(50) NOT NULL, IsActive bit null;";

        var result = _parser.Parse(sql, SqlObjectKind.Table, "Users");

        result.Columns.Should().HaveCount(3);
        result.Columns.Single(c => c.Name == "UserName").SqlType.Should().Be("NVARCHAR(50)");
        result.Columns.Single(c => c.Name == "IsActive").IsNullable.Should().BeTrue();
    }
}
