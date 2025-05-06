using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class ClientsService : IClientsService 
{
    private readonly string _connectionString = "Data Source=db-mssql16.pjwstk.edu.pl;Initial Catalog=2019SBD;Integrated Security=True;TrustServerCertificate=Yes";
    public async Task<List<ClientTripDTO>> GetTripsByClientId(int id)
{
    var clientTrips = new List<ClientTripDTO>();

    //Get trips that the client is registered on
    string query = @"
        SELECT t.IdTrip, t.Name, t.DateFrom, t.DateTo, ct.PaymentDate, ct.RegisteredAt, c.IdCountry,c.Name AS CountryName
        FROM Client_Trip ct
        JOIN Trip t ON ct.IdTrip = t.IdTrip
        LEFT JOIN Country_Trip cnt ON t.IdTrip = cnt.IdTrip
        LEFT JOIN Country c ON cnt.IdCountry = c.IdCountry
        WHERE ct.IdClient = @ClientId
        ORDER BY t.IdTrip";

    using (SqlConnection conn = new SqlConnection(_connectionString))
    using (SqlCommand cmd = new SqlCommand(query, conn))
    {
        cmd.Parameters.AddWithValue("@ClientId", id);
        await conn.OpenAsync();

        using (var reader = await cmd.ExecuteReaderAsync())
        {
            ClientTripDTO currentTrip = null;
            int lastTripId = -1;

            while (await reader.ReadAsync())
            {
                int tripId = reader.GetInt32(0);

                if (tripId != lastTripId)
                {
                    currentTrip = new ClientTripDTO()
                    {
                        IdTrip = tripId,
                        Name = reader.GetString(1),
                        DateFrom = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                        DateTo = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                        PaymentDate = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                        RegisteredAt = reader.GetInt32(5)
                    };
                    clientTrips.Add(currentTrip);
                    lastTripId = tripId;
                }

                if (!reader.IsDBNull(6))
                {
                    currentTrip.Countries.Add(new CountryDTO()
                    {
                        Name = reader.GetString(7)
                    });
                }
            }
        }
    }

    return clientTrips;
}
public async Task<ClientResponseDTO> CreateClient(ClientDTO clientDto)
{
    //Check if client with this Pesel currently exitsts
    string checkQuery = "SELECT COUNT(1) FROM Client WHERE Pesel = @Pesel";
    
    using (SqlConnection conn = new SqlConnection(_connectionString))
    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
    {
        checkCmd.Parameters.AddWithValue("@Pesel", clientDto.Pesel);
        await conn.OpenAsync();
        
        int existingCount = (int)await checkCmd.ExecuteScalarAsync();
        if (existingCount > 0)
        {
            throw new InvalidOperationException("Client with this PESEL already exists");
        }
    }

    //Insert new client
    string insertQuery = @"
        INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
        OUTPUT INSERTED.IdClient
        VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)";
    
    using (SqlConnection conn = new SqlConnection(_connectionString))
    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
    {
        cmd.Parameters.AddWithValue("@FirstName", clientDto.FirstName);
        cmd.Parameters.AddWithValue("@LastName", clientDto.LastName);
        cmd.Parameters.AddWithValue("@Email", clientDto.Email);
        cmd.Parameters.AddWithValue("@Telephone", clientDto.Telephone);
        cmd.Parameters.AddWithValue("@Pesel", clientDto.Pesel);

        await conn.OpenAsync();
        int newId = (int)await cmd.ExecuteScalarAsync();

        return new ClientResponseDTO
        {
            Id = newId,
            Message = "Client created successfully"
        };
    }
}
public async Task<RegisterForTripResponse> RegisterClientForTrip(int clientId, int tripId)
{
    using (SqlConnection conn = new SqlConnection(_connectionString))
    {
        await conn.OpenAsync();
        using (SqlTransaction transaction = conn.BeginTransaction())
        {
            try
            {
                //Check if client with this Id exists
                string checkClientQuery = "SELECT COUNT(1) FROM Client WHERE IdClient = @ClientId";
                using (SqlCommand checkClientCmd = new SqlCommand(checkClientQuery, conn, transaction))
                {
                    checkClientCmd.Parameters.AddWithValue("@ClientId", clientId);
                    int clientExists = (int)await checkClientCmd.ExecuteScalarAsync();
                    if (clientExists == 0)
                    {
                        return new RegisterForTripResponse 
                        { 
                            Success = false, 
                            Message = "Client not found" 
                        };
                    }
                }
                
                //Check the maximum capacity of this trip
                string checkTripQuery = "SELECT MaxPeople FROM Trip WHERE IdTrip = @TripId";
                int maxPeople = 0;
                
                using (SqlCommand checkTripCmd = new SqlCommand(checkTripQuery, conn, transaction))
                {
                    checkTripCmd.Parameters.AddWithValue("@TripId", tripId);
                    object result = await checkTripCmd.ExecuteScalarAsync();
                    if (result == null)
                    {
                        return new RegisterForTripResponse 
                        { 
                            Success = false, 
                            Message = "Trip not found" 
                        };
                    }
                    maxPeople = Convert.ToInt32(result);
                }

                //Check how many people are currently registered for the trip
                string countParticipantsQuery = "SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @TripId";
                int currentParticipants = 0;
                
                using (SqlCommand countCmd = new SqlCommand(countParticipantsQuery, conn, transaction))
                {
                    countCmd.Parameters.AddWithValue("@TripId", tripId);
                    currentParticipants = (int)await countCmd.ExecuteScalarAsync();
                }

                if (currentParticipants >= maxPeople)
                {
                    return new RegisterForTripResponse 
                    { 
                        Success = false, 
                        Message = "Trip has reached maximum capacity" 
                    };
                }

                //Check if the client is already registered for the trip
                string checkRegistrationQuery = @"
                    SELECT COUNT(1) 
                    FROM Client_Trip 
                    WHERE IdClient = @ClientId AND IdTrip = @TripId";
                
                using (SqlCommand checkRegCmd = new SqlCommand(checkRegistrationQuery, conn, transaction))
                {
                    checkRegCmd.Parameters.AddWithValue("@ClientId", clientId);
                    checkRegCmd.Parameters.AddWithValue("@TripId", tripId);
                    int alreadyRegistered = (int)await checkRegCmd.ExecuteScalarAsync();
                    if (alreadyRegistered > 0)
                    {
                        return new RegisterForTripResponse 
                        { 
                            Success = false, 
                            Message = "Client is already registered for this trip" 
                        };
                    }
                }

                //Insert new registration
                string insertQuery = @"
                    INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt)
                    VALUES (@ClientId, @TripId, @RegisteredAt)";
                
                using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn, transaction))
                {
                    insertCmd.Parameters.AddWithValue("@ClientId", clientId);
                    insertCmd.Parameters.AddWithValue("@TripId", tripId);
                    insertCmd.Parameters.AddWithValue("@RegisteredAt", int.Parse(DateTime.Now.ToString("yyyyMMdd")));
                    await insertCmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return new RegisterForTripResponse 
                { 
                    Success = true, 
                    Message = "Client successfully registered for the trip" 
                };
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}

public async Task<UnregisterResponse> UnregisterClientFromTrip(int clientId, int tripId)
{
    using (SqlConnection conn = new SqlConnection(_connectionString))
    {
        await conn.OpenAsync();
        using (SqlTransaction transaction = conn.BeginTransaction())
        {
            try
            {
                //Check if a client is registered for the trip
                string checkQuery = @"
                    SELECT COUNT(1) 
                    FROM Client_Trip 
                    WHERE IdClient = @ClientId AND IdTrip = @TripId";
                
                int registrationExists;
                using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn, transaction))
                {
                    checkCmd.Parameters.AddWithValue("@ClientId", clientId);
                    checkCmd.Parameters.AddWithValue("@TripId", tripId);
                    registrationExists = (int)await checkCmd.ExecuteScalarAsync();
                }

                if (registrationExists == 0)
                {
                    return new UnregisterResponse 
                    { 
                        Success = false,
                        Message = "Registration not found" 
                    };
                }

                //Delete the registration
                string deleteQuery = @"
                    DELETE FROM Client_Trip 
                    WHERE IdClient = @ClientId AND IdTrip = @TripId";
                
                int rowsAffected;
                using (SqlCommand deleteCmd = new SqlCommand(deleteQuery, conn, transaction))
                {
                    deleteCmd.Parameters.AddWithValue("@ClientId", clientId);
                    deleteCmd.Parameters.AddWithValue("@TripId", tripId);
                    rowsAffected = await deleteCmd.ExecuteNonQueryAsync();
                }

                if (rowsAffected == 0)
                {
                    transaction.Rollback();
                    return new UnregisterResponse 
                    { 
                        Success = false,
                        Message = "Failed to unregister client from trip" 
                    };
                }

                transaction.Commit();
                return new UnregisterResponse 
                { 
                    Success = true,
                    Message = "Successfully unregistered client from trip" 
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}

}