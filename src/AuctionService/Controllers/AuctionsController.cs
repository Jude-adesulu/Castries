using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuctionsController : ControllerBase
    {
        private readonly AuctionDbContext _context;
        private readonly IMapper _mapper;

        public AuctionsController(AuctionDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions()
        {
            var auctions = await _context.Auctions
                    .Include(x => x.Item)
                    .OrderBy(x => x.Item.Make)
                    .ToListAsync();
            return _mapper.Map<List<AuctionDto>>(auctions);
        }

        [HttpGet("{id}")]
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
            auction.Seller = "Dave";

            _context.Auctions.Add(auction);
            var result = await _context.SaveChangesAsync() > 0;

            if(!result) return BadRequest("Could not save chnages to DB");
            
            return CreatedAtAction(nameof(GetAuctionById), new {auction.Id}, _mapper.Map<AuctionDto>(auction));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDto updateAuction)
        {
            var auction = await _context.Auctions.Include(x => x.Item)
            .FirstAsync(x => x.Id == id);
             
             //TODO: check seller name == username
             if (auction is null) return NotFound();

             auction.Item.Make = updateAuction.Make ?? auction.Item.Make;
             auction.Item.Color = updateAuction.Color ?? auction.Item.Color;
             auction.Item.Model = updateAuction.Model ?? auction.Item.Model;
             auction.Item.Mileage = updateAuction.Mileage ?? auction.Item.Mileage;
             auction.Item.Year = updateAuction.Year ?? auction.Item.Year;

             var result = await _context.SaveChangesAsync() > 0;

            if(!result) return BadRequest("Could not save chnages to DB");
            
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAuction (Guid id)
        {
            var auction = await _context.Auctions.FindAsync(id);

            if (auction == null) return NotFound();

            //TODO: check seller name == username

            _context.Auctions.Remove(auction);
            
             var result = await _context.SaveChangesAsync() > 0;

            if(!result) return BadRequest("Could not save chnages to DB");
            
            return Ok();
        }
    }
}
