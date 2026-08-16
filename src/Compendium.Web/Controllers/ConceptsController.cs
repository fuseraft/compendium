using Microsoft.AspNetCore.Mvc;
using Compendium.Agent;
using Compendium.Web.Services;

namespace Compendium.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConceptsController : ControllerBase
{
    private readonly BundleService _bundleService;

    public ConceptsController(BundleService bundleService)
    {
        _bundleService = bundleService;
    }

    [HttpGet]
    public IActionResult ListConcepts([FromQuery] string? type = null)
    {
        if (_bundleService.CurrentBundle == null)
            return BadRequest("No bundle loaded");

        var tools = new CompendiumTools(_bundleService.CurrentBundle);
        var result = tools.ListConcepts(type);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public IActionResult GetConcept([FromRoute] string id)
    {
        if (_bundleService.CurrentBundle == null)
            return BadRequest("No bundle loaded");

        var tools = new CompendiumTools(_bundleService.CurrentBundle);
        var result = tools.ReadConcept(id);

        if (result.StartsWith("No concept with id"))
            return NotFound(result);

        return Ok(result);
    }

    [HttpGet("search")]
    public IActionResult SearchConcepts([FromQuery] string query)
    {
        if (_bundleService.CurrentBundle == null)
            return BadRequest("No bundle loaded");

        var tools = new CompendiumTools(_bundleService.CurrentBundle);
        var result = tools.SearchConcepts(query);
        return Ok(result);
    }

    [HttpPost]
    public IActionResult CreateConcept([FromBody] CreateConceptRequest request)
    {
        if (_bundleService.CurrentBundle == null)
            return BadRequest("No bundle loaded");

        var tools = new CompendiumTools(_bundleService.CurrentBundle);
        var result = tools.CreateConcept(
            request.Type,
            request.Title,
            request.Description,
            request.Body,
            request.Tags);

        _bundleService.ReloadBundle();
        return Ok(result);
    }

    [HttpPut("{id}/body")]
    public IActionResult UpdateConceptBody([FromRoute] string id, [FromBody] UpdateBodyRequest request)
    {
        if (_bundleService.CurrentBundle == null)
            return BadRequest("No bundle loaded");

        var tools = new CompendiumTools(_bundleService.CurrentBundle);
        var result = tools.UpdateConceptBody(id, request.Body);

        _bundleService.ReloadBundle();
        return Ok(result);
    }

    [HttpPost("{id}/links")]
    public IActionResult AddLink([FromRoute] string id, [FromBody] AddLinkRequest request)
    {
        if (_bundleService.CurrentBundle == null)
            return BadRequest("No bundle loaded");

        var tools = new CompendiumTools(_bundleService.CurrentBundle);
        var result = tools.AddLink(id, request.ToId, request.LinkText, request.Section);

        _bundleService.ReloadBundle();
        return Ok(result);
    }

    [HttpPost("{id}/flag")]
    public IActionResult FlagForReview([FromRoute] string id, [FromBody] FlagRequest request)
    {
        if (_bundleService.CurrentBundle == null)
            return BadRequest("No bundle loaded");

        var tools = new CompendiumTools(_bundleService.CurrentBundle);
        var result = tools.FlagForReview(id, request.Reason);
        return Ok(result);
    }
}

public record CreateConceptRequest(string Type, string Title, string Description, string Body, string[]? Tags);
public record UpdateBodyRequest(string Body);
public record AddLinkRequest(string ToId, string LinkText, string Section);
public record FlagRequest(string Reason);
