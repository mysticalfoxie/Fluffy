using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Fluffy.DatabaseManagement;

public class Storage : DbContext
{
    private static string _connectionString;

    public static async Task EnsureCanConnect()
    {
        await using var database = new Storage();
        if (!await database.Database.CanConnectAsync())
            throw new Exception("Cannot connect to database.");
    }

    public static void ConfigureConnection(string address, string username, string password, string database)
    {
        _connectionString = new DbConnectionStringBuilder
        {
            ["Server"] = address,
            ["User ID"] = username,
            ["Password"] = password,
            ["Database"] = database
        }.ToString();
    }

    private const string MIGRATIONS_CONNECTION =
        "Server=84.252.122.171;User ID=SA;Password=Kaktus02178239;Database=Production";

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder ??= new DbContextOptionsBuilder()
            .UseSqlServer(_connectionString ?? MIGRATIONS_CONNECTION)
            .EnableSensitiveDataLogging()
            .LogTo(msg => Debug.WriteLine(msg));

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var assembly = typeof(Storage).Assembly;
        modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        base.OnModelCreating(modelBuilder);
    }
}