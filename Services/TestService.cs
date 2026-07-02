using Dapper;

namespace IASDMagnolia.Services;

public class TestService
{
    private readonly DatabaseService _database;

    public TestService(DatabaseService database)
    {
        _database = database;
    }

    public async Task<int> TestConnectionAsync()
    {
        using var connection = _database.CreateConnection();

        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM users;");
    }
}