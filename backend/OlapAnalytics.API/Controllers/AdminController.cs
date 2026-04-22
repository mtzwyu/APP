using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OlapAnalytics.Domain.Interfaces;

namespace OlapAnalytics.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin")]
public class AdminController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IDatasetRepository _datasetRepository;

    public AdminController(IUserRepository userRepository, IDatasetRepository datasetRepository)
    {
        _userRepository = userRepository;
        _datasetRepository = datasetRepository;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userRepository.GetAllAsync();
        // Don't return password hashes!
        return Ok(users.Select(u => new
        {
            u.Id,
            u.Email,
            u.Role,
            u.CreatedAt
        }));
    }

    [HttpGet("datasets")]
    public async Task<IActionResult> GetDatasets()
    {
        var datasets = await _datasetRepository.GetAllAsync();
        return Ok(datasets);
    }

    [HttpDelete("dataset/{id}")]
    public async Task<IActionResult> DeleteDataset(int id)
    {
        await _datasetRepository.DeleteAsync(id);
        return NoContent();
    }
}
