using AutoMapper;
using WebApiTemplate.Models;
using WebApiTemplate.Repository.Database.Entities;

namespace WebApiTemplate.Service.Mapper
{
    public class ProductMapper : Profile
    {
        public ProductMapper()
        {
            // Explicit field mapping to avoid Id/CreatedAt mismatch and to include all relevant fields
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
                .ReverseMap(); // enables DTO -> Entity mapping for create/update
        }
    }
}