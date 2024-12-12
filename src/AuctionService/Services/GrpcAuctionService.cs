using AuctionService.Data;
using Grpc.Core;

namespace AuctionService.Services;

public class GrpcAuctionService(AuctionDbContext dbContext) : GrpcAuction.GrpcAuctionBase
{
    public override async Task<GrpcAuctionResponse> GetAuction(GetAuctionRequest request, ServerCallContext context)
    {
        Console.WriteLine("==> Received Grpc Request for auction");

        var auction = await dbContext.Auctions.FindAsync(Guid.Parse(request.Id)) 
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Not Found"));

        var response = new GrpcAuctionResponse
        {
            Auction = new GrpcAuctionModel
            {
                AuctionEnd = auction.AuctionEnd.ToString(),
                Id = auction.Id.ToString(),
                ReservationPrice = auction.ReservationPrice,
                Seller = auction.Seller
            }
        };

        return response;
    }
}
