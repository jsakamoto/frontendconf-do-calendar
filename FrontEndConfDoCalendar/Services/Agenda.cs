using System.Globalization;
using AngleSharp.Html.Parser;
using FrontendConfDoCalendar.Models;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;

namespace FrontendConfDoCalendar.Services;

internal class Agenda
{
    private readonly IHttpClientFactory _httpClientFactory;

    public Agenda(IHttpClientFactory httpClientFactory)
    {
        this._httpClientFactory = httpClientFactory;
    }

    internal async ValueTask<IEnumerable<Session>> GetSessionsAsync()
    {
        static DateTime ParseDateTime(string? dateTime) => DateTime.TryParse($"{dateTime}+9:00", null, DateTimeStyles.AdjustToUniversal, out var date) ? date : DateTime.MinValue;
        var sessionList = new List<Session>();

        sessionList.AddRange([
            new() {
                Title = "オープニング",
                StartTime = ParseDateTime("2024/08/24 10:00"),
                EndTime = ParseDateTime("2024/08/24 10:20"),
                Location = "カケハシ"
            },
            new() {
                Title = "クロージング",
                StartTime = ParseDateTime("2024/08/24 18:35"),
                EndTime = ParseDateTime("2024/08/24 18:50"),
                Location = "カケハシ"
            },
            new() {
                Title = "懇親会",
                StartTime = ParseDateTime("2024/08/24 19:15"),
                EndTime = ParseDateTime("2024/08/24 21:15"),
                Location = "LINEヤフー WOWルーム"
            }
        ]);

        var authority = "https://fortee.jp";
        var httpClient = this._httpClientFactory.CreateClient();
        var parser = new HtmlParser();

        // Fetch the time table page
        var timetablePage = await httpClient.GetStringAsync("https://fortee.jp/frontend-conf-hokkaido-2024/timetable");
        var timetableDoc = await parser.ParseDocumentAsync(timetablePage);

        // Traverse each session detail page
        var linksToSession = timetableDoc.QuerySelectorAll(".proposal .title a").AsEnumerable();
        foreach (var link in linksToSession)
        {
            // Fetch the session detail page
            await Task.Delay(10);
            var sessionUrl = link.GetAttribute("href");
            var sessionPage = await httpClient.GetStringAsync(authority + sessionUrl);
            var sessionDoc = await parser.ParseDocumentAsync(sessionPage);

            // Parse the session detail page
            var titleElement = sessionDoc.QuerySelector("h2");
            if (titleElement is null) continue;
            var title = titleElement.TextContent.Trim();
            var sessionInfoBlock = titleElement.ParentElement?.QuerySelector(".type");
            var location = sessionInfoBlock?.QuerySelector(".track")?.TextContent.Trim() ?? "";
            var startTime = ParseDateTime(sessionInfoBlock?.QuerySelector(".schedule")?.TextContent.Trim().TrimEnd('〜'));
            var durationtext = sessionInfoBlock?.QuerySelector(".name")?.TextContent;
            var duration = sessionInfoBlock?.QuerySelector(".name")?.TextContent switch
            {
                "レギュラートーク(20分)" => 20,
                "スポンサートーク(10分)" => 10,
                "スポンサーLT(5分)" => 5,
                "LT(5分)" => 5,
                _ => 0
            };
            var endTime = startTime.AddMinutes(duration);

            var speakerBlock = titleElement.ParentElement?.QuerySelector(".speaker");
            var speaker = speakerBlock?.QuerySelector("span")?.TextContent.Trim() ?? "";

            var descriptionBlock = titleElement.ParentElement?.QuerySelector(".abstract");
            var description = string.Join("\n", (descriptionBlock?.TextContent.Trim() ?? "").Split('\n').Select(s => s.Trim())).Replace("\n\n", "\n");

            // Add the session to the list
            sessionList.Add(new Session
            {
                Speaker = speaker,
                Title = title == "" ? speaker : title,
                StartTime = startTime,
                EndTime = endTime,
                Description = description,
                Location = location,
            });
        }

        return sessionList;
    }

    internal async ValueTask<string> GetSessionsAsICalAsync()
    {
        var sessionList = await this.GetSessionsAsync();
        var calendar = new Ical.Net.Calendar();
        calendar.AddProperty("X-WR-CALNAME", "Frontend Conference HOKKAIDO");
        calendar.AddProperty("X-WR-CALDESC", "フロントエンドカンファレンス北海道");
        foreach (var session in sessionList)
        {
            var icalEvent = new CalendarEvent
            {
                IsAllDay = false,
                Uid = session.GetHashForUID(),
                DtStart = new CalDateTime(session.StartTime) { HasTime = true },
                DtEnd = new CalDateTime(session.EndTime) { HasTime = true },
                Summary = session.Title,
                Description = $"<b>Speaker:</b>\r\n{session.Speaker}\r\n\r\n<b>Description:</b>\r\n{session.Description}",
                Location = session.Location,
            };
            calendar.Events.Add(icalEvent);
        }

        var serializer = new CalendarSerializer(new SerializationContext());
        return serializer.SerializeToString(calendar);
    }
}