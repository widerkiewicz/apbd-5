using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tutorial8.Models.DTOs;
using Tutorial8.Services;

namespace Tutorial8.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientController : ControllerBase
    {
        private readonly IClientsService _clientService;

        public ClientController(IClientsService clientService)
        {
            _clientService = clientService;
        }

        //Get all trips assigned to a client's {id}
        [HttpGet("{id}/trips")]
        public async Task<IActionResult> GetClientTrips(int id)
        {
            try
            {
                var trips = await _clientService.GetTripsByClientId(id);
                if (trips == null || trips.Count == 0)
                {
                    return NotFound($"No trips found for client with ID {id}");
                }
                return Ok(trips);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        //Create a new client with information provided in the request's body
        [HttpPost]
        public async Task<IActionResult> CreateClient([FromBody] ClientDTO clientDto) {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var result = await _clientService.CreateClient(clientDto);
                return CreatedAtAction(nameof(CreateClient), new { id = result.Id }, result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        //Register a client on a trip using client's {id} and trip's {tripId}
        [HttpPut("{id}/trips/{tripId}")]
        public async Task<IActionResult> RegisterClientForTrip(int id, int tripId)
        {
            try
            {
                var result = await _clientService.RegisterClientForTrip(id, tripId);
                
                if (!result.Success)
                {
                    return BadRequest(new { result.Message });
                }

                return Ok(new { result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Internal server error: {ex.Message}" });
            }
        }

        //Unregister a client from a trip using client's {id} and trip's {tripId}
        [HttpDelete("{id}/trips/{tripId}")]
        public async Task<IActionResult> UnregisterClientFromTrip(int id, int tripId)
        {
            try
            {
            var result = await _clientService.UnregisterClientFromTrip(id, tripId);
            
            if (!result.Success)
            {
                if (result.Message.Contains("Error"))
                {
                    return StatusCode(500, new { result.Message });
                }
                return BadRequest(new { result.Message });
            }

            return Ok(new { result.Message });
            }
            catch (Exception ex)
            {
            return StatusCode(500, new { Message = $"Internal server error: {ex.Message}" });
            }
        }
    }
}