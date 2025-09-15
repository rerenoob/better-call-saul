using Microsoft.AspNetCore.Mvc;
using BetterCallSaul.CaseService.Models.Entities;
using BetterCallSaul.CaseService.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace BetterCallSaul.CaseService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CasesController : ControllerBase
{
    private readonly ICaseRepository _caseRepository;
    private readonly ILogger<CasesController> _logger;

    public CasesController(ICaseRepository caseRepository, ILogger<CasesController> logger)
    {
        _caseRepository = caseRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CaseDocument>>> GetCases([FromQuery] string? userId = null)
    {
        try
        {
            IEnumerable<CaseDocument> cases;
            
            if (!string.IsNullOrEmpty(userId))
            {
                cases = await _caseRepository.GetByUserIdAsync(userId);
            }
            else
            {
                cases = await _caseRepository.GetAllAsync();
            }

            return Ok(cases);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cases");
            return StatusCode(500, "An error occurred while retrieving cases");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CaseDocument>> GetCase(string id)
    {
        try
        {
            var caseDocument = await _caseRepository.GetByIdAsync(id);
            if (caseDocument == null)
            {
                return NotFound();
            }

            return Ok(caseDocument);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving case {CaseId}", id);
            return StatusCode(500, "An error occurred while retrieving the case");
        }
    }

    [HttpPost]
    public async Task<ActionResult<CaseDocument>> CreateCase(CaseDocument caseDocument)
    {
        try
        {
            if (string.IsNullOrEmpty(caseDocument.UserId))
            {
                return BadRequest("UserId is required");
            }

            var createdCase = await _caseRepository.CreateAsync(caseDocument);
            return CreatedAtAction(nameof(GetCase), new { id = createdCase.Id }, createdCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating case");
            return StatusCode(500, "An error occurred while creating the case");
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CaseDocument>> UpdateCase(string id, CaseDocument caseDocument)
    {
        try
        {
            if (id != caseDocument.Id)
            {
                return BadRequest("ID mismatch");
            }

            if (!await _caseRepository.ExistsAsync(id))
            {
                return NotFound();
            }

            var updatedCase = await _caseRepository.UpdateAsync(id, caseDocument);
            return Ok(updatedCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating case {CaseId}", id);
            return StatusCode(500, "An error occurred while updating the case");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCase(string id)
    {
        try
        {
            if (!await _caseRepository.ExistsAsync(id))
            {
                return NotFound();
            }

            var success = await _caseRepository.DeleteAsync(id);
            if (!success)
            {
                return StatusCode(500, "Failed to delete case");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting case {CaseId}", id);
            return StatusCode(500, "An error occurred while deleting the case");
        }
    }
}