using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tutorial8.Models.DTOs;
using Tutorial8.Services;

namespace Tutorial8.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly ITripsService _tripsService;

        public TripsController(ITripsService tripsService)
        {
            _tripsService = tripsService;
        }

        [HttpGet("trips")]
        public async Task<IActionResult> GetTrips()
        {
            var trips = await _tripsService.GetTrips();
            return Ok(trips);
        }

        [HttpGet("clients/{id}/trips")]
        public async Task<IActionResult> GetTrip(int id)
        {
            try
            {
                return Ok(await _tripsService.GetTripsForClient(id));

            }
            catch (Exception ex)
            {
                return NotFound(ex);
            }
            
        }

        [HttpPost("clients")]
        public async Task<IActionResult> AddClient([FromBody] ClientDTO client)
        {
            try
            {
                int newClientId = await _tripsService.InsertClient(client);
                return Created(String.Empty, new { IdClient = newClientId });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "An unexpected error occurred while creating the client.");
            }
        }
        
        [HttpPut("api/clients/{id}/trips/{tripId}")]
        public async Task<IActionResult> RegisterClientForTrip(int id, int tripId)
        {
            try
            {
                await _tripsService.RegisterClientForTrip(id, tripId);
                return Ok("Client successfully registered for the trip.");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
        
        [HttpDelete("api/clients/{id}/trips/{tripId}")]
        public async Task<IActionResult> DeleteClientTrip(int id, int tripId)
        {
            try
            {
                await _tripsService.DeleteClientTrip(id, tripId);
                return Ok("Client was successfully unregistered from the trip.");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }


        
    }
}
