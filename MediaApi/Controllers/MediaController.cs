using MediaApi.Domain;
using MediaApi.Helpers;
using MediaApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaApi.Controllers
{
    public class MediaController : ControllerBase
    {
        MediaDataContext Context;
        ISystemTime SystemTime;

        public MediaController(MediaDataContext context, ISystemTime systemTime)
        {
            Context = context;
            SystemTime = systemTime;
        }

        [HttpPost("media/consumed")]
        public async Task<IActionResult> ConsumedMedia([FromBody] PostMediaConsumedRequest request)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(request);
            }

            var media = await Context.MediaItems
                .Where(m => m.Removed == false && m.Id == request.Id)
                .SingleOrDefaultAsync();

            if(media == null)
            {
                return BadRequest("Bad Meadia");
            } else
            {
                media.Consumed = true;
                media.DateConsumed = SystemTime.GetCurrent();
                await Context.SaveChangesAsync();
                return NoContent();
            }
        }


        [HttpDelete("media/{id:int}")]
        public async Task<IActionResult> RemoveMediaItem(int id)
        {
            var item = await Context.MediaItems
                .Where(m => m.Removed == false && m.Id == id)
                .SingleOrDefaultAsync();

            if(item != null)
            {
                item.Removed = true;
                await Context.SaveChangesAsync();
            }
            return NoContent();
        }



        [HttpPost("media")]
        public async Task<IActionResult> AddMedia([FromBody] PostMediaRequest mediaToAdd)
        {

            await Task.Delay(3000);


            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            else
            {
                var media = new MediaItem
                {
                    Title = mediaToAdd.Title,
                    Kind = mediaToAdd.Kind,
                    Consumed = false,
                    DateConsumed = null,
                    RecommendedBy = mediaToAdd.RecommendedBy,
                    Removed = false
                };
                Context.MediaItems.Add(media);
                await Context.SaveChangesAsync();

                var response = new MediaResponseItem
                {
                    Id = media.Id,
                    Title = media.Title,
                    Kind = media.Kind,
                    Consumed = media.Consumed,
                    DateConsumed = media.DateConsumed,
                    RecommendedBy = media.RecommendedBy
                };


                return CreatedAtRoute("media#getbyid", new { id = response.Id }, response);

            }
        }

        [HttpGet("media/{id:int}", Name ="media#getbyid")]
        public async Task<IActionResult> GetAMediaItem(int id)
        {
            var item = await Context.MediaItems
                .Where(m => m.Removed == false && m.Id == id)
                .Select(m => new MediaResponseItem
                {
                    Id = m.Id,
                    Title = m.Title,
                    Kind = m.Kind,
                    Consumed = m.Consumed,
                    DateConsumed = m.DateConsumed,
                    RecommendedBy = m.RecommendedBy
                }).SingleOrDefaultAsync();
            if(item == null)
            {
                return NotFound("No Item with that Id");

            } else
            {
                return Ok(item);
            }
        }

        [HttpGet("media")]
        public async Task<IActionResult> GetAllMedia([FromQuery] string kind = "All")
        {
            var query = Context.MediaItems
                .Where(m => m.Removed == false)
                .Select(m => new MediaResponseItem
                {
                    Id = m.Id,
                    Title = m.Title,
                    Consumed = m.Consumed,
                    DateConsumed = m.DateConsumed,
                    Kind = m.Kind,
                    RecommendedBy = m.RecommendedBy
                });
            if (kind != "All")
            {
                query = query.Where(q => q.Kind == kind);
            }
            var response = new GetMediaResponse
            {
                Data = await query.ToListAsync(),
                FilteredBy = kind
            };
            return Ok(response);
        }
    }
}
