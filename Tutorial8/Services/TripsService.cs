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


    public Task<TripDTO> GetTripsForClient(int id)
    {
        throw new NotImplementedException();
    }
}