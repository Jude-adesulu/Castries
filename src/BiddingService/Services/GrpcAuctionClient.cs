using System;
using AuctionService;
using BiddingService.Models;
using Grpc.Net.Client;

namespace BiddingService.Services;

public class GrpcAuctionClient(ILogger<GrpcAuctionClient> logger, IConfiguration config)
{
    public async Task<Auction> GetAuction(string id)
    {
        logger.LogInformation("Calling GRPC Service");
        var channel = GrpcChannel.ForAddress(config["GrpcAuction"]);
        var Client = new GrpcAuction.GrpcAuctionClient(channel);
        var request = new GetAuctionRequest { Id = id };

        try
        {
            var response = await Client.GetAuctionAsync(request);
            var auction = new Auction
            {
                ID = response.Auction.Id,
                AuctionEnd = DateTime.Parse(response.Auction.AuctionEnd),
                Seller = response.Auction.Seller,
                ReservationPrice =response.Auction.ReservationPrice
            };

            return auction;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not call gRPC server");
            return null;
        }
    }
}
