using AutoMapper;
using MoviesApp.Application.DTOs;
using MoviesApp.Application.DTOs.Auth;
using MoviesApp.Domain.Entities;

namespace MoviesApp.Application.Mappings;

/// <summary>
/// Perfil de AutoMapper para mapeos de entidades
/// </summary>
public class MovieMappingProfile : Profile
{
    public MovieMappingProfile()
    {
        // Mapeos para Movie
        CreateMap<Movie, MovieDto>();
        CreateMap<CreateMovieDto, Movie>();
        CreateMap<UpdateMovieDto, Movie>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // Mapeos para User
        CreateMap<User, UserInfoDto>();
        CreateMap<RegisterRequestDto, User>()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // Se maneja por separado
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => "User"))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));
    }
} 