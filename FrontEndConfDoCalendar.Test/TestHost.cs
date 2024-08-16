using System.Text;
using FrontendConfDoCalendar.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FrontendConfDoCalendar.Test;

internal class TestHost
{
    private class AgendaPageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var fileName = request.RequestUri?.AbsoluteUri switch
            {
                "https://fortee.jp/frontend-conf-hokkaido-2024/timetable" => "timetable.html",
                "https://fortee.jp/frontend-conf-hokkaido-2024/proposal/3b8fed13-5990-4981-92c8-1f5666e3363e" => "proposal-3b8fed13-5990-4981-92c8-1f5666e3363e.html",
                "https://fortee.jp/frontend-conf-hokkaido-2024/proposal/44d4167c-2b7f-4bc4-bc4b-319b1ca39c82" => "proposal-44d4167c-2b7f-4bc4-bc4b-319b1ca39c82.html",
                "https://fortee.jp/frontend-conf-hokkaido-2024/proposal/812dd186-d237-4956-9a2a-3ce475cb056a" => "proposal-812dd186-d237-4956-9a2a-3ce475cb056a.html",
                "https://fortee.jp/frontend-conf-hokkaido-2024/proposal/f5bdebf8-0a87-4b66-bb0b-76a1749d71a1" => "proposal-f5bdebf8-0a87-4b66-bb0b-76a1749d71a1.html",
                _ => throw new NotImplementedException()
            };

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var content = File.ReadAllText(Path.Combine(baseDir, "Assets", fileName));
            return Task.FromResult(new HttpResponseMessage()
            {
                Content = new StringContent(content, Encoding.UTF8, "text/html")
            });
        }
    }

    public static ServiceProvider GetServiceProvider()
    {
        return new ServiceCollection()
            .AddHttpClient()
            .ConfigureHttpClientDefaults(builder =>
            {
                builder.ConfigurePrimaryHttpMessageHandler((_) => new AgendaPageHandler());
            })
            .AddSingleton<Agenda>()
            .BuildServiceProvider();
    }
}
