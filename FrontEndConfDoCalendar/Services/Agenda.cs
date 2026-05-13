using System.Globalization;
using System.Text.RegularExpressions;
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
                StartTime = ParseDateTime("2026/06/06 10:40"),
                EndTime = ParseDateTime("2026/06/06 11:00"),
                Location = "カケハシルーム"
            },
            new() {
                Title = "エンディング",
                StartTime = ParseDateTime("2026/06/06 18:25"),
                EndTime = ParseDateTime("2026/06/06 18:45"),
                Location = "カケハシルーム"
            },
            new() {
                Title = "Ask the speaker",
                StartTime = ParseDateTime("2026/06/06 12:30"),
                EndTime = ParseDateTime("2026/06/06 12:45"),
                Location = "スポンサールーム"
            },
            new() {
                Title = "Ask the speaker",
                StartTime = ParseDateTime("2026/06/06 15:20"),
                EndTime = ParseDateTime("2026/06/06 15:35"),
                Location = "スポンサールーム"
            },
            new() {
                Title = "Ask the speaker",
                StartTime = ParseDateTime("2026/06/06 16:50"),
                EndTime = ParseDateTime("2026/06/06 17:05"),
                Location = "スポンサールーム"
            }
        ]);

        var authority = "https://fortee.jp";
        var httpClient = this._httpClientFactory.CreateClient();
        var parser = new HtmlParser();

        // Fetch the time table page
        var timetablePage = await httpClient.GetStringAsync("https://fortee.jp/frontend-phpcon-do-2026/timetable");
        var timetableDoc = await parser.ParseDocumentAsync(timetablePage);

        // Traverse each session detail page
        var linksToSession = timetableDoc.QuerySelectorAll(".proposal .title a").AsEnumerable();
        foreach (var link in linksToSession)
        {
            // Fetch the session detail page
            await Task.Delay(20);
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

            var durationText = sessionInfoBlock?.QuerySelector(".name")?.TextContent;
            var match = Regex.Match(durationText ?? "", @"(?<min>\d+)分");
            var duration = match.Success ? int.Parse(match.Groups["min"].Value) : 0;

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
        calendar.AddProperty("X-WR-CALNAME", "Frontend and PHP Conference Hokkaido");
        calendar.AddProperty("X-WR-CALDESC", "フロントエンド・PHP カンファレンス北海道");
        foreach (var session in sessionList)
        {
            var icalEvent = new CalendarEvent
            {
                Uid = session.GetHashForUID(),
                DtStart = new CalDateTime(session.StartTime),
                DtEnd = new CalDateTime(session.EndTime),
                Summary = session.Title,
                Description = $"<b>Speaker:</b>\r\n{session.Speaker}\r\n\r\n<b>Description:</b>\r\n{session.Description}",
                Location = session.Location,
            };
            calendar.Events.Add(icalEvent);
        }

        var serializer = new CalendarSerializer(new SerializationContext());
        return serializer.SerializeToString(calendar) ?? "";
    }
}