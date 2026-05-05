using System.ComponentModel.DataAnnotations;

namespace NokiaHome.Models;

public class CalendarEvent
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public bool AllDay { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public class CalendarIndexViewModel
{
    public IReadOnlyList<CalendarEvent> Events { get; init; } = [];
    public CreateEventForm Form { get; init; } = new();
}

public class CreateEventForm
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(300)]
    public string? Location { get; set; }

    [Required]
    public DateTime Start { get; set; } = DateTime.Today.AddHours(9);

    [Required]
    public DateTime End { get; set; } = DateTime.Today.AddHours(10);

    public bool AllDay { get; set; }
}

