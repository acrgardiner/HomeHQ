using HomeHQ.Application.Mapping;
using HomeHQ.DTOs;
using HomeHQ.Entities;
using HomeHQ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeHQ.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "BearerAndCookies")]
public class NotesController : PolymorphicEntitiesController<Note, NoteDto, CreateNoteRequest, UpdateNoteRequest>
{
    public NotesController(IEntityService<Note> entityService)
        : base(entityService, EntityMappings.Note)
    {
    }
}
