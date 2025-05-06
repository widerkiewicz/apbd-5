using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class TripsService : ITripsService
{
    private readonly string _connectionString = "Data Source=db-mssql16.pjwstk.edu.pl;Initial Catalog=2019SBD;Integrated Security=True;TrustServerCertificate=Yes";
    
    public async Task<List<TripDTO>> GetTrips()
    {
        var trips = new List<TripDTO>();

            //Get all trip with countries
            string command = @"SELECT t.IdTrip, t.Name, c.IdCountry, c.Name AS CountryName
                FROM Trip t
                LEFT JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
                LEFT JOIN Country c ON ct.IdCountry = c.IdCountry
                ORDER BY t.IdTrip";
        
        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            await conn.OpenAsync();

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                TripDTO currentTrip = null;
                int lastTripId = -1;
                
                while (await reader.ReadAsync())
                {
                    int tripId = reader.GetInt32(0);
                    
                    if (tripId != lastTripId)
                    {
                        currentTrip = new TripDTO()
                        {
                            Id = tripId,
                            Name = reader.GetString(1),
                            Countries = new List<CountryDTO>()
                        };
                        trips.Add(currentTrip);
                        lastTripId = tripId;
                    }
                    
                    if (!reader.IsDBNull(2))
                    {
                        currentTrip.Countries.Add(new CountryDTO()
                        {
                            Name = reader.GetString(3)
                        });
                    }
                }
            }
        }

        return trips;
    }


}