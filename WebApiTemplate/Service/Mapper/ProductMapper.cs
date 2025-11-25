using AutoMapper;
using WebApiTemplate.Models;
using WebApiTemplate.Repository.Database.Entities;
using WebApiTemplate.Service.Helpers;

namespace WebApiTemplate.Service.Mapper
{
    /// <summary>
    /// AutoMapper profile for Product-related mappings
    /// Note: Current implementation uses manual mapping in service layer for better control
    /// This profile is available for future use if needed
    /// </summary>
    public class ProductMapper : Profile
    {
        public ProductMapper()
        {
            // Product to ProductDto (legacy)
            CreateMap<Product, ProductDto>()
                .ForMember(d => d.ProductId, opt => opt.MapFrom(s => s.ProductId))
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name))
                .ForMember(d => d.Description, opt => opt.MapFrom(s => s.Description))
                .ForMember(d => d.Category, opt => opt.MapFrom(s => s.Category))
                .ForMember(d => d.StartingPrice, opt => opt.MapFrom(s => s.StartingPrice))
                .ForMember(d => d.AuctionDuration, opt => opt.MapFrom(s => s.AuctionDuration))
                .ForMember(d => d.OwnerId, opt => opt.MapFrom(s => s.OwnerId))
                .ForMember(d => d.HighestBidId, opt => opt.MapFrom(s => s.HighestBidId))
                .ForMember(d => d.ExpiryTime, opt => opt.MapFrom(s => s.ExpiryTime))
                .ReverseMap();

            // Product to ProductListDto
            CreateMap<Product, ProductListDto>()
                .ForMember(d => d.HighestBidAmount, opt => opt.MapFrom(s => s.HighestBid != null ? s.HighestBid.Amount : (decimal?)null))
                .ForMember(d => d.AuctionStatus, opt => opt.MapFrom(s => s.Auction != null ? s.Auction.Status : null))
                .ForMember(d => d.TimeRemainingMinutes, opt => opt.Ignore()); // Calculated in service

            // CreateProductDto to Product
            CreateMap<CreateProductDto, Product>()
                .ForMember(d => d.ProductId, opt => opt.Ignore())
                .ForMember(d => d.OwnerId, opt => opt.Ignore()) // Set from JWT
                .ForMember(d => d.ExpiryTime, opt => opt.Ignore()) // Calculated
                .ForMember(d => d.HighestBidId, opt => opt.Ignore())
                .ForMember(d => d.Owner, opt => opt.Ignore())
                .ForMember(d => d.HighestBid, opt => opt.Ignore())
                .ForMember(d => d.Auction, opt => opt.Ignore());

            // UpdateProductDto to Product (partial update)
            CreateMap<UpdateProductDto, Product>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Bid to BidDto
            CreateMap<Bid, BidDto>()
                .ForMember(d => d.BidderName, opt => opt.MapFrom(s => AuctionHelpers.GetUserDisplayName(s.Bidder)));

            // Auction to ActiveAuctionDto
            CreateMap<Auction, ActiveAuctionDto>()
                .ForMember(d => d.ProductId, opt => opt.MapFrom(s => s.ProductId))
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Product.Name))
                .ForMember(d => d.Description, opt => opt.MapFrom(s => s.Product.Description))
                .ForMember(d => d.Category, opt => opt.MapFrom(s => s.Product.Category))
                .ForMember(d => d.StartingPrice, opt => opt.MapFrom(s => s.Product.StartingPrice))
                .ForMember(d => d.HighestBidAmount, opt => opt.MapFrom(s => s.HighestBid != null ? s.HighestBid.Amount : (decimal?)null))
                .ForMember(d => d.HighestBidderName, opt => opt.MapFrom(s => s.HighestBid != null ? AuctionHelpers.GetUserDisplayName(s.HighestBid.Bidder) : null))
                .ForMember(d => d.AuctionStatus, opt => opt.MapFrom(s => s.Status))
                .ForMember(d => d.TimeRemainingMinutes, opt => opt.Ignore()); // Calculated in service

            // Auction to AuctionDetailDto
            CreateMap<Auction, AuctionDetailDto>()
                .ForMember(d => d.ProductId, opt => opt.MapFrom(s => s.ProductId))
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Product.Name))
                .ForMember(d => d.Description, opt => opt.MapFrom(s => s.Product.Description))
                .ForMember(d => d.Category, opt => opt.MapFrom(s => s.Product.Category))
                .ForMember(d => d.StartingPrice, opt => opt.MapFrom(s => s.Product.StartingPrice))
                .ForMember(d => d.AuctionDuration, opt => opt.MapFrom(s => s.Product.AuctionDuration))
                .ForMember(d => d.OwnerId, opt => opt.MapFrom(s => s.Product.OwnerId))
                .ForMember(d => d.OwnerName, opt => opt.MapFrom(s => AuctionHelpers.GetUserDisplayName(s.Product.Owner)))
                .ForMember(d => d.ExpiryTime, opt => opt.MapFrom(s => s.ExpiryTime))
                .ForMember(d => d.HighestBidAmount, opt => opt.MapFrom(s => s.HighestBid != null ? s.HighestBid.Amount : (decimal?)null))
                .ForMember(d => d.AuctionStatus, opt => opt.MapFrom(s => s.Status))
                .ForMember(d => d.TimeRemainingMinutes, opt => opt.Ignore()) // Calculated in service
                .ForMember(d => d.Bids, opt => opt.Ignore()); // Loaded separately
        }
    }
}