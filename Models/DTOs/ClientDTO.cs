namespace Tutorial8.Models.DTOs;

public class ClientDTO {
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Telephone { get; set; }
    public string Pesel { get; set; }
}

public class ClientResponseDTO {
    public int Id { get; set; }
    public string? Message { get; set; }
}

public class RegisterForTripResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
}public class UnregisterResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
}