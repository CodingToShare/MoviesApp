using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoviesApp.Domain.Entities;
using MoviesApp.Infrastructure.Data;
using MoviesApp.Infrastructure.Repositories;
using Xunit;

namespace MoviesApp.Tests.Infrastructure.Tests;

/// <summary>
/// Pruebas unitarias para MovieRepository
/// </summary>
public class MovieRepositoryTests : IDisposable
{
    private readonly MoviesDbContext _context;
    private readonly MovieRepository _repository;
    private readonly ILogger<MovieRepository> _logger;
    private readonly LoggerFactory _loggerFactory;

    public MovieRepositoryTests()
    {
        // Configurar base de datos en memoria
        var options = new DbContextOptionsBuilder<MoviesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MoviesDbContext(options);
        _loggerFactory = new LoggerFactory();
        _logger = _loggerFactory.CreateLogger<MovieRepository>();
        _repository = new MovieRepository(_context, _logger);

        // Asegurar que la base de datos esté creada
        _context.Database.EnsureCreated();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsMovie()
    {
        // Arrange
        var movie = new Movie(1, "Test Movie", "Action", "Test Studio", 85, 2023);
        await _context.Movies.AddAsync(movie);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test Movie", result.Film);
        Assert.Equal("Action", result.Genre);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull(int invalidId)
    {
        // Act
        var result = await _repository.GetByIdAsync(invalidId);

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

        await _context.Movies.AddRangeAsync(movies);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync(orderBy: "Year", ascending: true);

        // Assert
        var moviesList = result.ToList();
        Assert.Equal(3, moviesList.Count);
        Assert.Equal(2021, moviesList[0].Year); // Ordenado por año ascendente
        Assert.Equal(2022, moviesList[1].Year);
        Assert.Equal(2023, moviesList[2].Year);
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var movies = new List<Movie>
        {
            new Movie(1, "Movie A", "Action", "Studio A", 90, 2021),
            new Movie(2, "Movie B", "Comedy", "Studio B", 80, 2022),
            new Movie(3, "Movie C", "Drama", "Studio C", 70, 2023),
            new Movie(4, "Movie D", "Horror", "Studio D", 60, 2024)
        };

        await _context.Movies.AddRangeAsync(movies);
        await _context.SaveChangesAsync();

        // Act - Obtener la segunda página con 2 elementos por página
        var result = await _repository.GetAllAsync(skip: 2, take: 2, orderBy: "Year", ascending: true);

        // Assert
        var moviesList = result.ToList();
        Assert.Equal(2, moviesList.Count);
        Assert.Equal(2023, moviesList[0].Year);
        Assert.Equal(2024, moviesList[1].Year);
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WithValidMovie_AddsMovieSuccessfully()
    {
        // Arrange
        var movie = new Movie(1, "New Movie", "Action", "New Studio", 85, 2023);

        // Act
        var result = await _repository.AddAsync(movie);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(movie.Id, result.Id);
        Assert.Equal(movie.Film, result.Film);

        // Verificar que se guardó en la base de datos
        var savedMovie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == 1);
        Assert.NotNull(savedMovie);
        Assert.Equal("New Movie", savedMovie.Film);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateId_ThrowsInvalidOperationException()
    {
        // Arrange
        var movie1 = new Movie(1, "First Movie", "Action", "Studio", 85, 2023);
        var movie2 = new Movie(1, "Second Movie", "Drama", "Studio", 90, 2024);

        await _repository.AddAsync(movie1);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _repository.AddAsync(movie2));
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingMovie_UpdatesSuccessfully()
    {
        // Arrange
        var movie = new Movie(1, "Original Movie", "Action", "Original Studio", 80, 2023);
        await _repository.AddAsync(movie);

        // Modificar la película
        movie.Film = "Updated Movie";
        movie.Genre = "Drama";
        movie.Score = 95;

        // Act
        var result = await _repository.UpdateAsync(movie);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Movie", result.Film);
        Assert.Equal("Drama", result.Genre);
        Assert.Equal(95, result.Score);

        // Verificar en la base de datos
        var updatedMovie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == 1);
        Assert.NotNull(updatedMovie);
        Assert.Equal("Updated Movie", updatedMovie.Film);
        Assert.Equal("Drama", updatedMovie.Genre);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingMovie_DeletesSuccessfully()
    {
        // Arrange
        var movie = new Movie(1, "Movie to Delete", "Action", "Studio", 80, 2023);
        await _repository.AddAsync(movie);

        // Act
        var result = await _repository.DeleteAsync(1);

        // Assert
        Assert.True(result);

        // Verificar que se eliminó de la base de datos
        var deletedMovie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == 1);
        Assert.Null(deletedMovie);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingMovie_ReturnsFalse()
    {
        // Act
        var result = await _repository.DeleteAsync(999);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WithExistingMovie_ReturnsTrue()
    {
        // Arrange
        var movie = new Movie(1, "Existing Movie", "Action", "Studio", 80, 2023);
        await _repository.AddAsync(movie);

        // Act
        var result = await _repository.ExistsAsync(1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistingMovie_ReturnsFalse()
    {
        // Act
        var result = await _repository.ExistsAsync(999);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetCountAsync Tests

    [Fact]
    public async Task GetCountAsync_WithMovies_ReturnsCorrectCount()
    {
        // Arrange
        var movies = new List<Movie>
        {
            new Movie(1, "Movie 1", "Action", "Studio 1", 90, 2023),
            new Movie(2, "Movie 2", "Comedy", "Studio 2", 80, 2022),
            new Movie(3, "Movie 3", "Drama", "Studio 3", 70, 2021)
        };

        await _context.Movies.AddRangeAsync(movies);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetCountAsync();

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public async Task GetCountAsync_WithEmptyDatabase_ReturnsZero()
    {
        // Act
        var result = await _repository.GetCountAsync();

        // Assert
        Assert.Equal(0, result);
    }

    #endregion

    #region GetByGenreAsync Tests

    [Fact]
    public async Task GetByGenreAsync_WithMatchingGenre_ReturnsMovies()
    {
        // Arrange
        var movies = new List<Movie>
        {
            new Movie(1, "Action Movie 1", "Action", "Studio 1", 90, 2023),
            new Movie(2, "Comedy Movie", "Comedy", "Studio 2", 80, 2022),
            new Movie(3, "Action Movie 2", "Action", "Studio 3", 85, 2021)
        };

        await _context.Movies.AddRangeAsync(movies);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByGenreAsync("Action");

        // Assert
        var actionMovies = result.ToList();
        Assert.Equal(2, actionMovies.Count);
        Assert.All(actionMovies, movie => Assert.Equal("Action", movie.Genre));
    }

    #endregion

    #region GetByYearAsync Tests

    [Fact]
    public async Task GetByYearAsync_WithMatchingYear_ReturnsMovies()
    {
        // Arrange
        var movies = new List<Movie>
        {
            new Movie(1, "Movie 2023-1", "Action", "Studio 1", 90, 2023),
            new Movie(2, "Movie 2022", "Comedy", "Studio 2", 80, 2022),
            new Movie(3, "Movie 2023-2", "Drama", "Studio 3", 85, 2023)
        };

        await _context.Movies.AddRangeAsync(movies);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByYearAsync(2023);

        // Assert
        var movies2023 = result.ToList();
        Assert.Equal(2, movies2023.Count);
        Assert.All(movies2023, movie => Assert.Equal(2023, movie.Year));
    }

    #endregion

    #region GetByMinScoreAsync Tests

    [Fact]
    public async Task GetByMinScoreAsync_WithMinScore_ReturnsMatchingMovies()
    {
        // Arrange
        var movies = new List<Movie>
        {
            new Movie(1, "High Score Movie", "Action", "Studio 1", 95, 2023),
            new Movie(2, "Medium Score Movie", "Comedy", "Studio 2", 75, 2022),
            new Movie(3, "Another High Score", "Drama", "Studio 3", 90, 2021)
        };

        await _context.Movies.AddRangeAsync(movies);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByMinScoreAsync(80);

        // Assert
        var highScoreMovies = result.ToList();
        Assert.Equal(2, highScoreMovies.Count);
        Assert.All(highScoreMovies, movie => Assert.True(movie.Score >= 80));
    }

    #endregion

    #region Dispose

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _loggerFactory.Dispose();
    }

    #endregion
} 