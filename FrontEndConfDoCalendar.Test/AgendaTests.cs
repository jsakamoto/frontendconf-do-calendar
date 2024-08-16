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
            "08/24/2024 10:00 - 08/24/2024 10:20 | カケハシ |  | オープニング",
            "08/24/2024 18:35 - 08/24/2024 18:50 | カケハシ |  | クロージング",
            "08/24/2024 19:15 - 08/24/2024 21:15 | LINEヤフー WOWルーム |  | 懇親会",
            "08/24/2024 10:30 - 08/24/2024 10:50 | Location X | Speaker Alpha | Lorem ipsum dolor",
            "08/24/2024 11:00 - 08/24/2024 11:10 | Location Y | Speaker Beta | Amet nisl wisi",
            "08/24/2024 11:35 - 08/24/2024 11:40 | Location X | Speaker Gamma | Vulputate diam takimata",
            "08/24/2024 17:50 - 08/24/2024 17:55 | Location Y | Speaker Theta | Ea duis elitr"
        );
    }
}