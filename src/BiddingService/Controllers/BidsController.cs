using AutoMapper;
using BiddingService.Models;
using MassTransit;
using Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Entities;
using BiddingService.DTOs;
using BiddingService.Services;

namespace BiddingService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BidsController(IMapper mapper, IPublishEndpoint publishEndpoint, GrpcAuctionClient grpcClient) : ControllerBase
    {
        [Authorize]
        public async Task<ActionResult<Bid>> PlaceBid(string auctionId, int amount)
        {
            var auction = await DB.Find<Auction>().OneAsync(auctionId);
            
            if (auction == null)
            {
                auction = await grpcClient.GetAuction(auctionId);

                if (auction is null) return BadRequest("Cannot accept bids on this auction at the moment");
            }

            if (auction.Seller == User.Identity.Name)
            {
                return BadRequest("You can't bid on your own auction");
            }

            var bid = new Bid()
            {
                Amount = amount,
                AuctionId = auctionId,
                Bidder = User.Identity.Name
            };

            if (auction.AuctionEnd < DateTime.UtcNow)
            {
                bid.BidStatus = BidStatus.Finished;
            }

            var highBid = await DB.Find<Bid>()
                .Match(a => a.AuctionId == auctionId)
                .Sort(b => b.Descending(x => x.Amount))
                .ExecuteFirstAsync();

            if (highBid == null || amount > highBid.Amount)
            {
                bid.BidStatus = amount > auction.ReservationPrice
                    ? BidStatus.Accepted
                    : BidStatus.AcceptedBelowReserve;
            }

            if (highBid != null && bid.Amount <= highBid.Amount)
            {
                bid.BidStatus = BidStatus.TooLow;
            }

            await DB.SaveAsync(bid);

            await publishEndpoint.Publish(mapper.Map<BidPlaced>(bid));

            return Ok(mapper.Map<BidDto>(bid));
        }

         [HttpGet("{auctionId}")]
        public async Task<ActionResult<List<BidDto>>> GetBidsForAuction(string auctionId)
        {
            var bids = await DB.Find<Bid>()
                .Match(a => a.AuctionId == auctionId)
                .Sort(b => b.Descending(a => a.BidTime))
                .ExecuteAsync();

            return bids.Select(mapper.Map<BidDto>).ToList(); ;
        }
    }
}
