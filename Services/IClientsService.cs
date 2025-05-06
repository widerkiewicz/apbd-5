using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public interface IClientsService
{
    Task<List<ClientTripDTO>> GetTripsByClientId(int id);
    Task<ClientResponseDTO> CreateClient(ClientDTO clientDto);
    Task<RegisterForTripResponse> RegisterClientForTrip(int clientId, int tripId);
    Task<UnregisterResponse> UnregisterClientFromTrip(int clientId, int tripId);


}