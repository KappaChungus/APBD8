using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public interface ITripsService
{
    Task<List<TripDTO>> GetTrips();
    Task<ClientDTO> GetTripsForClient(int id);
    Task<int> InsertClient(ClientDTO client);
    Task RegisterClientForTrip(int clientId, int tripId);
    Task DeleteClientTrip(int clientId, int tripId);
}