using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;

app.MapGet("/api/trips", async () =>
{
    var trips = new List<object>();
    using var connection = new SqlConnection(connectionString);
    var query = @"
        SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
               c.IdCountry, c.Name AS CountryName
        FROM Trip t
        LEFT JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
        LEFT JOIN Country c ON ct.IdCountry = c.IdCountry";

    using var command = new SqlCommand(query, connection);
    await connection.OpenAsync();
    using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        trips.Add(new
        {
            IdTrip = reader["IdTrip"],
            Name = reader["Name"],
            Description = reader["Description"],
            DateFrom = reader["DateFrom"],
            DateTo = reader["DateTo"],
            MaxPeople = reader["MaxPeople"],
            Country = reader["CountryName"]
        });
    }

    return Results.Ok(trips);
});

app.MapGet("/api/clients/{id}/trips", async (int id) =>
{
    var trips = new List<object>();
    using var connection = new SqlConnection(connectionString);
    var query = @"
        SELECT t.IdTrip, t.Name, t.Description, ct.RegisteredAt
        FROM Client_Trip ct
        JOIN Trip t ON ct.IdTrip = t.IdTrip
        WHERE ct.IdClient = @IdClient";

    using var command = new SqlCommand(query, connection);
    command.Parameters.AddWithValue("@IdClient", id);
    await connection.OpenAsync();
    using var reader = await command.ExecuteReaderAsync();
    if (!reader.HasRows) return Results.NotFound("Brak wycieczek lub klient nie istnieje");
    while (await reader.ReadAsync())
    {
        trips.Add(new
        {
            IdTrip = reader["IdTrip"],
            Name = reader["Name"],
            Description = reader["Description"],
            RegisteredAt = reader["RegisteredAt"]
        });
    }

    return Results.Ok(trips);
});

app.MapPost("/api/clients", async ([FromBody] Client client) =>
{
    if (string.IsNullOrWhiteSpace(client.FirstName) ||
        string.IsNullOrWhiteSpace(client.LastName) ||
        string.IsNullOrWhiteSpace(client.Email) ||
        string.IsNullOrWhiteSpace(client.Telephone) ||
        string.IsNullOrWhiteSpace(client.Pesel))
    {
        return Results.BadRequest("Wszystkie pola są wymagane.");
    }

    var query = @"
        INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
        VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel);
        SELECT SCOPE_IDENTITY();";

    using var connection = new SqlConnection(connectionString);
    using var command = new SqlCommand(query, connection);
    command.Parameters.AddWithValue("@FirstName", client.FirstName);
    command.Parameters.AddWithValue("@LastName", client.LastName);
    command.Parameters.AddWithValue("@Email", client.Email);
    command.Parameters.AddWithValue("@Telephone", client.Telephone);
    command.Parameters.AddWithValue("@Pesel", client.Pesel);

    await connection.OpenAsync();
    var newId = Convert.ToInt32(await command.ExecuteScalarAsync());
    return Results.Created($"/api/clients/{newId}", new { IdClient = newId });
});

app.MapPut("/api/clients/{id}/trips/{IdTrip}", async (int id, int IdTrip) =>
{
    using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();

    var checkQuery = @"
        SELECT COUNT(*) FROM Trip WHERE IdTrip = @IdTrip;
        SELECT COUNT(*) FROM Client WHERE IdClient = @IdClient;
        SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @IdTrip;
        SELECT MaxPeople FROM Trip WHERE IdTrip = @IdTrip;";

    using var cmd = new SqlCommand(checkQuery, connection);
    cmd.Parameters.AddWithValue("@IdClient", id);
    cmd.Parameters.AddWithValue("@IdTrip", IdTrip);

    using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync() || (int)reader[0] == 0) return Results.NotFound("Wycieczka nie istnieje");
    await reader.NextResultAsync();
    if (!await reader.ReadAsync() || (int)reader[0] == 0) return Results.NotFound("Klient nie istnieje");
    await reader.NextResultAsync();
    var registered = await reader.ReadAsync() ? (int)reader[0] : 0;
    await reader.NextResultAsync();
    var max = await reader.ReadAsync() ? (int)reader[0] : 0;

    if (registered >= max) return Results.Conflict("Osiągnięto limit uczestników");
    reader.Close();

    var insert = @"INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt)
                    VALUES (@IdClient, @IdTrip, GETDATE());";
    using var insertCmd = new SqlCommand(insert, connection);
    insertCmd.Parameters.AddWithValue("@IdClient", id);
    insertCmd.Parameters.AddWithValue("@IdTrip", IdTrip);
    await insertCmd.ExecuteNonQueryAsync();

    return Results.Ok("Zarejestrowano klienta na wycieczkę");
});

app.MapDelete("/api/clients/{id}/trips/{IdTrip}", async (int id, int IdTrip) =>
{
    using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();

    var check = "SELECT COUNT(*) FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip";
    using var checkCmd = new SqlCommand(check, connection);
    checkCmd.Parameters.AddWithValue("@IdClient", id);
    checkCmd.Parameters.AddWithValue("@IdTrip", IdTrip);

    var exists = (int)await checkCmd.ExecuteScalarAsync() > 0;
    if (!exists) return Results.NotFound("Rejestracja nie istnieje");

    var delete = "DELETE FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip";
    using var delCmd = new SqlCommand(delete, connection);
    delCmd.Parameters.AddWithValue("@IdClient", id);
    delCmd.Parameters.AddWithValue("@IdTrip", IdTrip);
    await delCmd.ExecuteNonQueryAsync();

    return Results.Ok("Usunięto rejestrację klienta");
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();

public record Client(string FirstName, string LastName, string Email, string Telephone, string Pesel);