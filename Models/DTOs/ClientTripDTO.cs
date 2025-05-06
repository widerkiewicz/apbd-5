namespace Tutorial8.Models.DTOs;

public class ClientTripDTO
{
    public int IdTrip { get; set; }
    public string Name { get; set; }
    public int? PaymentDate { get; set; }
    public int RegisteredAt { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public List<CountryDTO> Countries { get; set; } = new List<CountryDTO>();
}



