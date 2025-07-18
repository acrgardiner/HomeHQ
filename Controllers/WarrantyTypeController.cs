using Microsoft.AspNetCore.Mvc;
using projectaardvarkx2.Entities;
using projectaardvarkx2.Services;

namespace projectaardvarkx2.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WarrantyTypesController : ControllerBase
{
    private readonly IEntityService<WarrantyType> _service;

    public WarrantyTypesController(IEntityService<WarrantyType> service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var warrantyTypes = await _service.GetAllAsync();
        return Ok(warrantyTypes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<IActionResult>> Get(Guid id)
    {
        //var warrantyType = await _service.GetByIdAsync(id);
        //if (warrantyType == null)
        //    return NotFound();
        //
        //return Ok(warrantyType);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WarrantyType warrantyType)
    {
        //if (warrantyType == null)
        //{
        //    return BadRequest("WarrantyType is null.");
        //}
        //
        //WarrantyType created = await _service.AddAsync(warrantyType);
        //return CreatedAtAction(nameof(Get), new { id = created.Id }, created);

        return Ok();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] WarrantyType warrantyType)
    {
        //if (warrantyType == null || id != warrantyType.Id)
        //{
        //    return BadRequest("WarrantyType is null or ID mismatch.");
        //}
        //
        //var existing = await _service.GetByIdAsync(id);
        //if (existing == null)
        //{
        //    return NotFound();
        //}
        //
        //await _service.UpdateAsync(warrantyType);
        //return NoContent();

        return Ok();
    }


    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);

        return Ok();
    }

}