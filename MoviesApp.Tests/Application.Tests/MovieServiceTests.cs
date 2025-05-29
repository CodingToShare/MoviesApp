using Microsoft.Extensions.Logging;
using Moq;
using MoviesApp.Application.DTOs;
using MoviesApp.Application.Services;
using MoviesApp.Domain.Entities;
using MoviesApp.Domain.Interfaces;
using Xunit;
using AutoMapper;
using MoviesApp.Application.Mappings;
using FluentValidation;
using FluentValidation.Results;

namespace MoviesApp.Tests.Application.Tests;

/// <summary>
/// Pruebas unitarias para MovieService
/// </summary>
public class MovieServiceTests
{
    private readonly Mock<IMovieRepository> _mockRepository;
    private readonly Mock<ILogger<MovieService>> _mockLogger;
    private readonly Mock<IValidator<CreateMovieDto>> _mockValidator;
    private readonly IMapper _mapper;
    private readonly MovieService _movieService;

    public MovieServiceTests()
    {
        _mockRepository = new Mock<IMovieRepository>();
        _mockLogger = new Mock<ILogger<MovieService>>();
        _mockValidator = new Mock<IValidator<CreateMovieDto>>();

        // Configurar AutoMapper
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MovieMappingProfile>();
        });
        _mapper = configuration.CreateMapper();

        _movieService = new MovieService(
            _mockRepository.Object,
            _mapper,
            _mockLogger.Object,
            _mockValidator.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsMovieDto()
    {
        // Arrange
        var movieId = 1;
        var movie = new Movie(movieId, "Test Movie", "Action", "Test Studio", 85, 2023);
        
        _mockRepository.Setup(x => x.GetByIdAsync(movieId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(movie);

        // Act
        var result = await _movieService.GetByIdAsync(movieId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(movieId, result.Id);
        Assert.Equal("Test Movie", result.Film);
        Assert.Equal("Action", result.Genre);
        Assert.Equal("Test Studio", result.Studio);
        Assert.Equal(85, result.Score);
        Assert.Equal(2023, result.Year);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var movieId = 999;
        _mockRepository.Setup(x => x.GetByIdAsync(movieId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Movie?)null);

        // Act
        var result = await _movieService.GetByIdAsync(movieId);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByIdAsync_WithInvalidIdValues_ReturnsNull(int invalidId)
    {
        // Act
        var result = await _movieService.GetByIdAsync(invalidId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithMovies_ReturnsOrderedMovies()
    {
        // Arrange
        var movies = new List<Movie>
        {
            new Movie(1, "Movie A", "Action", "Studio A", 90, 2023),
            new Movie(2, "Movie B", "Comedy", "Studio B", 80, 2022),
            new Movie(3, "Movie C", "Drama", "Studio C", 70, 2021)
        };

        _mockRepository.Setup(x => x.GetAllAsync(0, 3, "Year", true, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(movies);

        // Act
        var result = await _movieService.GetAllAsync(total: 3, order: "asc");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_WithDescOrder_ReturnsDescOrderedMovies()
    {
        // Arrange
        var movies = new List<Movie>
        {
            new Movie(3, "Movie C", "Drama", "Studio C", 70, 2021),
            new Movie(2, "Movie B", "Comedy", "Studio B", 80, 2022),
            new Movie(1, "Movie A", "Action", "Studio A", 90, 2023)
        };

        _mockRepository.Setup(x => x.GetAllAsync(0, 3, "Year", false, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(movies);

        // Act
        var result = await _movieService.GetAllAsync(total: 3, order: "desc");

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_WithTotalLimit_ReturnsLimitedResults()
    {
        // Arrange
        var movies = new List<Movie>
        {
            new Movie(1, "Movie A", "Action", "Studio A", 90, 2023),
            new Movie(2, "Movie B", "Comedy", "Studio B", 80, 2022)
        };

        _mockRepository.Setup(x => x.GetAllAsync(0, 2, "Year", true, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(movies);

        // Act
        var result = await _movieService.GetAllAsync(total: 2, order: "asc");

        // Assert
        Assert.Equal(2, result.Count());
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WithValidMovie_ReturnsCreatedMovie()
    {
        // Arrange
        var createDto = new CreateMovieDto
        {
            Id = 1,
            Film = "New Movie",
            Genre = "Action",
            Studio = "New Studio",
            Score = 85,
            Year = 2023
        };

        var validationResult = new ValidationResult();
        _mockValidator.Setup(x => x.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(validationResult);

        _mockRepository.Setup(x => x.GetByIdAsync(createDto.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Movie?)null);

        _mockRepository.Setup(x => x.AddAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Movie movie, CancellationToken _) => movie);

        // Act
        var result = await _movieService.AddAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createDto.Id, result.Id);
        Assert.Equal(createDto.Film, result.Film);
    }

    [Fact]
    public async Task AddAsync_WithExistingId_ThrowsInvalidOperationException()
    {
        // Arrange
        var createDto = new CreateMovieDto
        {
            Id = 1,
            Film = "Existing Movie",
            Genre = "Action",
            Studio = "Studio",
            Score = 85,
            Year = 2023
        };

        var existingMovie = new Movie(1, "Existing Movie", "Action", "Studio", 85, 2023);
        var validationResult = new ValidationResult();
        
        _mockValidator.Setup(x => x.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(validationResult);

        _mockRepository.Setup(x => x.GetByIdAsync(createDto.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(existingMovie);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _movieService.AddAsync(createDto));
        
        Assert.Contains("Ya existe", exception.Message);
    }

    [Fact]
    public async Task AddAsync_WithInvalidData_ThrowsValidationException()
    {
        // Arrange
        var createDto = new CreateMovieDto
        {
            Id = -1,
            Film = "",
            Genre = "",
            Studio = "",
            Score = 150,
            Year = 1800
        };

        var validationResult = new ValidationResult(new[]
        {
            new ValidationFailure("Id", "ID debe ser mayor a 0"),
            new ValidationFailure("Film", "Film es requerido"),
            new ValidationFailure("Score", "Score debe estar entre 0 y 100")
        });

        _mockValidator.Setup(x => x.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(validationResult);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _movieService.AddAsync(createDto));
        
        Assert.NotNull(exception.Errors);
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task GetByIdAsync_WhenRepositoryThrows_LogsErrorAndRethrows()
    {
        // Arrange
        var movieId = 1;
        var expectedException = new Exception("Database error");

        _mockRepository.Setup(x => x.GetByIdAsync(movieId, It.IsAny<CancellationToken>()))
                      .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _movieService.GetByIdAsync(movieId));
        
        Assert.Equal("Database error", exception.Message);
    }

    [Fact]
    public async Task GetAllAsync_WhenRepositoryThrows_LogsErrorAndRethrows()
    {
        // Arrange
        var expectedException = new Exception("Database connection failed");

        _mockRepository.Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                      .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _movieService.GetAllAsync());
        
        Assert.Equal("Database connection failed", exception.Message);
    }

    [Fact]
    public async Task AddAsync_WhenRepositoryThrows_LogsErrorAndRethrows()
    {
        // Arrange
        var createDto = new CreateMovieDto
        {
            Id = 1,
            Film = "Test Movie",
            Genre = "Action",
            Studio = "Test Studio",
            Score = 85,
            Year = 2023
        };

        var validationResult = new ValidationResult();
        var expectedException = new Exception("Database insert failed");

        _mockValidator.Setup(x => x.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(validationResult);

        _mockRepository.Setup(x => x.GetByIdAsync(createDto.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Movie?)null);

        _mockRepository.Setup(x => x.AddAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()))
                      .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _movieService.AddAsync(createDto));
        
        Assert.Equal("Database insert failed", exception.Message);
    }

    #endregion
} 