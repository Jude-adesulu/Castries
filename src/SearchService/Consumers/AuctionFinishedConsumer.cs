using System;
using AutoMapper;
using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Consumers;

public class AuctionFinishedConsumer : IConsumer<AuctionFinished>
{
    private readonly IMapper _mapper;

    public AuctionFinishedConsumer(IMapper mapper)
    {
        _mapper = mapper;
    }
    public async Task Consume(ConsumeContext<AuctionFinished> context)
    {
        Console.WriteLine("--> Consuming auction finished: " + context.Message.AuctionId);
        var auction = await DB.Find<Item>().OneAsync(context.Message.AuctionId);

        if (context.Message.ItemSold)
        {
            auction.CurrentHighBid = context.Message.Amount;
            auction.Winner = context.Message.Winner;
        };

        auction.Status = "Finished";
        await auction.SaveAsync();
    }
}
