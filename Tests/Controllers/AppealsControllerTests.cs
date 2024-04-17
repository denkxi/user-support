using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using WebApp.Controllers;
using WebApp.Domain;

namespace Tests.Controllers;

public class AppealsControllerTests
{
    private readonly Mock<IMemoryCache> _cache = new Mock<IMemoryCache>();
    private readonly AppealsController _controller;

    public AppealsControllerTests()
    {
        _controller = new AppealsController(_cache.Object);
    }

    [Fact]
    public void IndexTest()
    {
        // Arrange
        var appeals = new List<Appeal>
        {
            new Appeal {Id = Guid.NewGuid(), IsResolved = false, ResolutionDeadline = DateTime.Now.AddDays(1)},
            new Appeal {Id = Guid.NewGuid(), IsResolved = false, ResolutionDeadline = DateTime.Now.AddDays(3)},
            new Appeal {Id = Guid.NewGuid(), IsResolved = false, ResolutionDeadline = DateTime.Now.AddDays(2)}
        };

        // Mock the cache
        var cacheEntry = new Mock<ICacheEntry>();
        _cache.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(cacheEntry.Object);
        object cacheValue = appeals;
        _cache.Setup(m => m.TryGetValue("ActiveAppeals", out cacheValue)).Returns(true);

        // Act
        var result = _controller.Index() as ViewResult;
        var model = result?.Model as List<Appeal>;

        // Assert
        Assert.NotNull(model); // Appeals present in model
        Assert.Equal(3, model.Count); // 3 unresolved appeals
        Assert.Equal(appeals[1].ResolutionDeadline, model[0].ResolutionDeadline); // Sorted by deadline
    }

    [Fact]
    public void CreateSuccessTest()
    {
        // Arrange
        var newAppeal = new Appeal
        {
            Description = "I can't update my personal info on my account.",
            ResolutionDeadline = DateTime.Now.AddHours(3)
        };
        _controller.ModelState.Clear();

        var appeals = new List<Appeal>();
        var cacheEntry = new Mock<ICacheEntry>();
        _cache.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(cacheEntry.Object);

        object cachedAppeals = appeals;
        _cache.Setup(m => m.TryGetValue("ActiveAppeals", out cachedAppeals)).Returns(true);

        // Act
        var result = _controller.Create(newAppeal) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName); // Redirects after successful creation
        Assert.Contains(newAppeal, cachedAppeals as List<Appeal>); // Check if new appeal was added
    }

    [Fact]
    public void CreateInvalidDescriptionTest()
    {
        // Arrange
        var newAppeal = new Appeal
        {
            Description = "short", // description is too short
            ResolutionDeadline = DateTime.Now.AddHours(3)
        };

        _controller.ModelState.AddModelError("Description", "Description must be between 20 and 500 characters long.");

        // Act
        var result = _controller.Create(newAppeal) as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newAppeal, result.Model as Appeal); // The returned object should be the one passed in
        Assert.True(_controller.ModelState.ContainsKey("Description")); // Check if ModelState contains the specific error
        Assert.False(_controller.ModelState.IsValid); // ModelState should be invalid
    }

    [Fact]
    public void CreateInvalidDatetimeTest()
    {
        // Arrange
        var newAppeal = new Appeal
        {
            Description = "I have problem updating my personal data",
            ResolutionDeadline = DateTime.Now.AddMinutes(10) // the deadline is too close to entry datetime
        };

        _controller.ModelState.AddModelError("ResolutionDeadline", "Resolution deadline must be at least 30 minutes after entry time.");

        // Act
        var result = _controller.Create(newAppeal) as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newAppeal, result.Model as Appeal); // The returned object should be the one passed in
        Assert.True(_controller.ModelState.ContainsKey("ResolutionDeadline")); // Check if ModelState contains the specific error
        Assert.False(_controller.ModelState.IsValid); // ModelState should be invalid
    }

    [Fact]
    public void ResolveTest()
    {
        // Arrange
        var appealId = Guid.NewGuid();
        var appeals = new List<Appeal>
        {
            new Appeal
            {
                Id = appealId, 
                Description = "I have problem updating my personal data",
                IsResolved = false, 
                ResolutionDeadline = DateTime.Now.AddDays(1),
                EntryTime = DateTime.Now.AddHours(-3)
            }
        };

        // Mock the cache
        var cacheEntry = new Mock<ICacheEntry>();
        _cache.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(cacheEntry.Object);
        object cacheValue = appeals;
        _cache.Setup(m => m.TryGetValue("ActiveAppeals", out cacheValue)).Returns(true);

        // Act
        var result = _controller.Resolve(appealId) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        
        var resolvedAppeal = appeals.FirstOrDefault(a => a.Id == appealId);
        Assert.NotNull(resolvedAppeal); 
        Assert.True(resolvedAppeal.IsResolved); // Ensure the appeal is marked as resolved
    }
    
    [Fact]
    public void DeleteTest()
    {
        // Arrange
        var appealId = Guid.NewGuid();
        var appeals = new List<Appeal>
        {
            new Appeal
            {
                Id = appealId, 
                Description = "I have problem updating my personal data",
                IsResolved = false, 
                ResolutionDeadline = DateTime.Now.AddDays(1),
                EntryTime = DateTime.Now.AddHours(-3)
            }
        };

        // Mock the cache
        var cacheEntry = new Mock<ICacheEntry>();
        _cache.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(cacheEntry.Object);
        object cacheValue = appeals;
        _cache.Setup(m => m.TryGetValue("ActiveAppeals", out cacheValue)).Returns(true);

        // Act
        var result = _controller.Delete(appealId) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName); // Redirects to Index after deleting the appeal
        Assert.DoesNotContain(appeals, a => a.Id == appealId); // Ensure the appeal is removed from the list
    }
}