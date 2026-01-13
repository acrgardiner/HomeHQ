using HomeHQ.Services;
using HomeHQ.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeHQ.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotesController : PolymorphicEntitiesController<Note>
{
    public NotesController(IEntityService<Note> entityService) : base(entityService)
    {
    }
}
