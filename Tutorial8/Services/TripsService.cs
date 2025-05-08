using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class TripsService : ITripsService
{
    private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=APBD;Integrated Security=True;";
    
    public async Task<List<TripDTO>> GetTrips()
    {
        var trips = new List<TripDTO>();

        string command = "SELECT IdTrip, Name, Description, DateFrom, DateTo, MaxPeople FROM Trip";

        using SqlConnection conn = new SqlConnection(_connectionString);
        using SqlCommand cmd = new SqlCommand(command, conn);
        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var trip = new TripDTO
            {
                Id = reader.GetInt32(reader.GetOrdinal("IdTrip")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Description = reader.GetString(reader.GetOrdinal("Description")),
                DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                Countries = new List<CountryDTO>()
            };

            trips.Add(trip);
        }

        foreach (var trip in trips)
        {
            string countryQuery = @"
            SELECT c.IdCountry, c.Name 
            FROM Country_Trip ct
            JOIN Country c ON ct.IdCountry = c.IdCountry
            WHERE ct.IdTrip = @TripId";

            using SqlCommand countryCmd = new SqlCommand(countryQuery, conn);
            countryCmd.Parameters.AddWithValue("@TripId", trip.Id);

            using var countryReader = await countryCmd.ExecuteReaderAsync();
            while (await countryReader.ReadAsync())
            {
                trip.Countries.Add(new CountryDTO
                {
                    IdCountry = countryReader.GetInt32(countryReader.GetOrdinal("IdCountry")),
                    Name = countryReader.GetString(countryReader.GetOrdinal("Name"))
                });
            }

            countryReader.Close();
        }

        return trips;
    }


    public async Task<ClientDTO> GetTripsForClient(int clientId)
    {
        ClientDTO client = null;

        string query = @"
            SELECT 
                c.IdClient, c.FirstName, c.LastName, c.Email, c.Telephone, c.Pesel,
                t.IdTrip, t.Name AS TripName, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
                ct.RegisteredAt, ct.PaymentDate
            FROM Client c
            JOIN Client_Trip ct ON c.IdClient = ct.IdClient
            JOIN Trip t ON ct.IdTrip = t.IdTrip
            WHERE c.IdClient = @ClientId";

        using SqlConnection conn = new SqlConnection(_connectionString);
        using SqlCommand cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@ClientId", clientId);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        if (!reader.HasRows)
            throw new Exception("Client not found or client has no trips");

        while (await reader.ReadAsync())
        {
            if (client == null)
            {
                client = new ClientDTO
                {
                    IdClient = reader.GetInt32(reader.GetOrdinal("IdClient")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    Telephone = reader.GetString(reader.GetOrdinal("Telephone")),
                    Pesel = reader.GetString(reader.GetOrdinal("Pesel")),
                    RegisteredAt = reader.GetDateTime(reader.GetOrdinal("RegisteredAt")),
                    PaymentDate = reader.GetDateTime(reader.GetOrdinal("PaymentDate")),
                    Trips = new List<TripDTO>()
                };
            }

            var trip = new TripDTO
            {
                Id = reader.GetInt32(reader.GetOrdinal("IdTrip")),
                Name = reader.GetString(reader.GetOrdinal("TripName")),
                Description = reader.GetString(reader.GetOrdinal("Description")),
                DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                Countries = new List<CountryDTO>()
            };

            client.Trips.Add(trip);
        }

        return client;
    }
    
    public async Task<int> InsertClient(ClientDTO client)
    {
        // Walidacja logiczna i semantyczna
        if (string.IsNullOrWhiteSpace(client.FirstName) ||
            string.IsNullOrWhiteSpace(client.LastName) ||
            string.IsNullOrWhiteSpace(client.Email) ||
            string.IsNullOrWhiteSpace(client.Telephone) ||
            string.IsNullOrWhiteSpace(client.Pesel))
        {
            throw new ArgumentException("All fields (FirstName, LastName, Email, Telephone, Pesel) are required.");
        }

        if (!Regex.IsMatch(client.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            throw new ArgumentException("Invalid email format.");
        }

        if (!Regex.IsMatch(client.Pesel, @"^\d{11}$"))
        {
            throw new ArgumentException("PESEL must be 11 digits.");
        }

        const string query = @"
        INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
        OUTPUT INSERTED.IdClient
        VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)";

        using SqlConnection conn = new SqlConnection(_connectionString);
        using SqlCommand cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@FirstName", client.FirstName);
        cmd.Parameters.AddWithValue("@LastName", client.LastName);
        cmd.Parameters.AddWithValue("@Email", client.Email);
        cmd.Parameters.AddWithValue("@Telephone", client.Telephone);
        cmd.Parameters.AddWithValue("@Pesel", client.Pesel);

        await conn.OpenAsync();
        var insertedId = await cmd.ExecuteScalarAsync();

        if (insertedId == null)
            throw new Exception("Client insertion failed unexpectedly.");

        return Convert.ToInt32(insertedId);
    }
    
    public async Task RegisterClientForTrip(int clientId, int tripId)
    {
        using SqlConnection conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using (var checkClient = new SqlCommand("SELECT 1 FROM Client WHERE IdClient = @Id", conn))
        {
            checkClient.Parameters.AddWithValue("@Id", clientId);
            if ((await checkClient.ExecuteScalarAsync()) == null)
                throw new KeyNotFoundException("Client not found.");
        }

        using (var checkTrip = new SqlCommand("SELECT MaxPeople FROM Trip WHERE IdTrip = @Id", conn))
        {
            checkTrip.Parameters.AddWithValue("@Id", tripId);
            object result = await checkTrip.ExecuteScalarAsync();
            if (result == null)
                throw new KeyNotFoundException("Trip not found.");

            int maxPeople = Convert.ToInt32(result);

            using var countCmd = new SqlCommand("SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @TripId", conn);
            countCmd.Parameters.AddWithValue("@TripId", tripId);
            int currentCount = (int)(await countCmd.ExecuteScalarAsync());

            if (currentCount >= maxPeople)
                throw new InvalidOperationException("Trip is already full.");
        }

        using (var checkDuplicate = new SqlCommand(
            "SELECT 1 FROM Client_Trip WHERE IdClient = @ClientId AND IdTrip = @TripId", conn))
        {
            checkDuplicate.Parameters.AddWithValue("@ClientId", clientId);
            checkDuplicate.Parameters.AddWithValue("@TripId", tripId);
            if ((await checkDuplicate.ExecuteScalarAsync()) != null)
                throw new InvalidOperationException("Client is already registered for this trip.");
        }

        using (var insertCmd = new SqlCommand(
            @"INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt, PaymentDate)
              VALUES (@ClientId, @TripId, @RegisteredAt, NULL)", conn))
        {
            insertCmd.Parameters.AddWithValue("@ClientId", clientId);
            insertCmd.Parameters.AddWithValue("@TripId", tripId);
            insertCmd.Parameters.AddWithValue("@RegisteredAt", DateTime.Now);
            await insertCmd.ExecuteNonQueryAsync();
        }
    }

    public async Task DeleteClientTrip(int clientId, int tripId)
    {
        using SqlConnection conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using (var checkCmd = new SqlCommand(
                   "SELECT 1 FROM Client_Trip WHERE IdClient = @ClientId AND IdTrip = @TripId", conn))
        {
            checkCmd.Parameters.AddWithValue("@ClientId", clientId);
            checkCmd.Parameters.AddWithValue("@TripId", tripId);

            if ((await checkCmd.ExecuteScalarAsync()) == null)
                throw new KeyNotFoundException("Client is not registered for this trip.");
        }

        using (var deleteCmd = new SqlCommand(
                   "DELETE FROM Client_Trip WHERE IdClient = @ClientId AND IdTrip = @TripId", conn))
        {
            deleteCmd.Parameters.AddWithValue("@ClientId", clientId);
            deleteCmd.Parameters.AddWithValue("@TripId", tripId);
            await deleteCmd.ExecuteNonQueryAsync();
        }
    }

    
}