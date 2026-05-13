using FrontendConfDoCalendar.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FrontendConfDoCalendar.Test;

public class AgendaTests
{
    [Test]
    public async Task GetSessionsAsync_Test()
    {
        static string ToString(DateTime dateTime) => dateTime.AddHours(9).ToString("MM/dd/yyyy HH:mm");

        // Given
        using var services = TestHost.GetServiceProvider();
        var agenda = services.GetRequiredService<Agenda>();

        // When
        var sessions = await agenda.GetSessionsAsync();

        // Then
        sessions.Select(s => $"{ToString(s.StartTime)} - {ToString(s.EndTime)} | {s.Location} | {s.Speaker} | {s.Title}").Is(
            "06/06/2026 10:40 - 06/06/2026 11:00 | カケハシルーム |  | オープニング",
            "06/06/2026 18:25 - 06/06/2026 18:45 | カケハシルーム |  | エンディング",
            "06/06/2026 12:30 - 06/06/2026 12:45 | スポンサールーム |  | Ask the speaker",
            "06/06/2026 15:20 - 06/06/2026 15:35 | スポンサールーム |  | Ask the speaker",
            "06/06/2026 16:50 - 06/06/2026 17:05 | スポンサールーム |  | Ask the speaker",
            "06/06/2026 10:30 - 06/06/2026 10:50 | Location X | Speaker Alpha | Lorem ipsum dolor",
            "06/06/2026 11:00 - 06/06/2026 11:10 | Location Y | Speaker Beta | Amet nisl wisi",
            "06/06/2026 11:35 - 06/06/2026 11:40 | Location X | Speaker Gamma | Vulputate diam takimata",
            "06/06/2026 17:50 - 06/06/2026 17:55 | Location Y | Speaker Theta | Ea duis elitr"
        );
    }
}