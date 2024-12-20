using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class AuctionsController : ControllerBase
    {
        private readonly AuctionDbContext _context;
        private readonly IMapper _mapper;
        private readonly IPublishEndpoint _publishEndpoint;

        public AuctionsController(AuctionDbContext context, IMapper mapper, 
            IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _mapper = mapper;
            _publishEndpoint = publishEndpoint;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions(string date)
        {
            var query = _context.Auctions.OrderBy(x => x.Item.Make).AsQueryable();
            
            if(!string.IsNullOrEmpty(date))
                query = query.Where(x => x.UpdatedAt.CompareTo(DateTime.Parse(date).ToUniversalTime()) > 0);
                
            return await query.ProjectTo<AuctionDto>(_mapper.ConfigurationProvider).ToListAsync();
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<AuctionDto>> GetAuctionById (Guid id)
        {
            var auction = await _context.Auctions
                    .Include(x => x.Item)
                    .FirstOrDefaultAsync(x => x.Id == id);

            if (auction is null) return NotFound();

            return _mapper.Map<AuctionDto>(auction);
        }

        [HttpPost]
        public async Task<ActionResult<AuctionDto>> CreateAuction (CreateAuctionDto auctionDto)
        {
            var auction = _mapper.Map<Auction>(auctionDto);
            auction.Seller = User.Identity.Name;

            _context.Auctions.Add(auction);
            
            var newAuction = _mapper.Map<AuctionDto>(auction);
            await _publishEndpoint.Publish(_mapper.Map<AuctionCreated>(newAuction));
            
            var result = await _context.SaveChangesAsync() > 0;

            if(!result) return BadRequest("Could not save chnages to DB");

            
            return CreatedAtAction(nameof(GetAuctionById), new {auction.Id}, _mapper.Map<AuctionDto>(auction));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDto updateAuction)
        {
            var auction = await _context.Auctions.Include(x => x.Item)
            .FirstAsync(x => x.Id == id);
             
             if (auction.Seller != User.Identity.Name) return Forbid();

             if (auction is null) return NotFound();

             auction.Item.Make = updateAuction.Make ?? auction.Item.Make;
             auction.Item.Color = updateAuction.Color ?? auction.Item.Color;
             auction.Item.Model = updateAuction.Model ?? auction.Item.Model;
             auction.Item.Mileage = updateAuction.Mileage ?? auction.Item.Mileage;
             auction.Item.Year = updateAuction.Year ?? auction.Item.Year;
            
             await _publishEndpoint.Publish(_mapper.Map<AuctionUpdated>(auction));

             var result = await _context.SaveChangesAsync() > 0;

            if(!result) return BadRequest("Could not save chnages to DB");
            
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAuction (Guid id)
        {
            var auction = await _context.Auctions.FindAsync(id);

            if (auction == null) return NotFound();

            if (auction.Seller != User.Identity.Name) return Forbid();

            _context.Auctions.Remove(auction);
            
            await _publishEndpoint.Publish<AuctionDeleted>(new { Id = auction.Id.ToString()});
             var result = await _context.SaveChangesAsync() > 0;

            if(!result) return BadRequest("Could not save chnages to DB");
            
            return Ok();
        }
    }
}
