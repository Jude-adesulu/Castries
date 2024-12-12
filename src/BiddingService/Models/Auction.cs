using System;
using MongoDB.Entities;

namespace BiddingService.Models;

public class Auction : Entity
{
    public DateTime AuctionEnd { get; set; }
    public string Seller { get; set; }
    public int ReservationPrice { get; set; }
    public bool Finished { get; set; }
}
