using APBD_Test1_Example.DTOs;
using APBD_Test1_Example.Exceptions;
using APBD_Test1_Example.Services;
using Microsoft.AspNetCore.Mvc;

namespace APBD_Test1_Example.Controllers;

[ApiController]
[Route("[controller]")]
public class PolitycyController(IDbService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPolitycyDetails([FromQuery] string? fName)
    {
        return Ok(await service.GetPolitykDetailsAsync(fName));
    }

    [HttpPost]
    public async Task<IActionResult> CreatePolityk([FromBody] PolitykCreateDto body)
    {
        try
        {
            var polityk = await service.CreatePolitykAsync(body);
            return Created($"students/{polityk.ID}", polityk);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}