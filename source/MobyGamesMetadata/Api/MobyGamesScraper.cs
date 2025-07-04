﻿using Barnite.Scrapers;
using HtmlAgilityPack;
using MobyGamesMetadata.Api.V2;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MobyGamesMetadata.Api;

public class MobyGamesScraper
{
    public MobyGamesScraper(IPlatformUtility platformUtility, IWebDownloader downloader)
    {
        PlatformUtility = platformUtility;
        Downloader = downloader;
    }

    public IPlatformUtility PlatformUtility { get; }
    public IWebDownloader Downloader { get; }

    public static string GetSearchUrl(string query, string objectType)
    {
        query = Uri.EscapeDataString(query);
        return $"https://www.mobygames.com/search/?q={query}&type={objectType}&adult=true";
    }

    public static string GetGameDetailsUrl(int id)
    {
        return $"https://www.mobygames.com/game/{id}";
    }

    public IEnumerable<GameSearchResult> GetGameSearchResults(string query)
    {
        var url = GetSearchUrl(query, "game");
        var response = Downloader.DownloadString(url);
        return ParseGameSearchResultHtml(response.ResponseContent);
    }

    public IEnumerable<GroupSearchResult> GetGroupSearchResults(string query)
    {
        var url = GetSearchUrl(query, "group");
        var response = Downloader.DownloadString(url);
        return ParseGroupSearchResultHtml(response.ResponseContent);
    }

    public GameDetails GetGameDetails(int id)
    {
        var url = GetGameDetailsUrl(id);
        return GetGameDetails(url);
    }

    public GameDetails GetGameDetails(string url)
    {
        var response = Downloader.DownloadString(url);
        return ParseGameDetailsHtml(response.ResponseContent);
    }

    private IEnumerable<GameSearchResult> ParseGameSearchResultHtml(string html)
    {
        var page = new HtmlDocument();
        page.LoadHtml(html);

        var cells = page.DocumentNode.SelectNodes("//table[@class='table mb']/tr/td[last()]");
        if (cells == null)
            yield break;

        foreach (var td in cells)
        {
            var a = td.SelectSingleNode(".//a[@href]");
            if (a == null)
                continue;

            var releaseDateString = td.SelectSingleNode(".//span[starts-with(text(), '(')]")?.InnerText.HtmlDecode().Trim('(', ')');
            var platforms = td.SelectSingleNode(".//small[last()]")?.ChildNodes
                            .Where(n => n.NodeType == HtmlNodeType.Text)
                            .Select(n => n.InnerText.HtmlDecode())
                            .Where(n => !string.IsNullOrWhiteSpace(n))
                            .ToList();

            var alternateNames = td.ChildNodes.FirstOrDefault(n => n.InnerText.StartsWith("AKA: "))
                ?.InnerText.TrimStart("AKA: ")
                .Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim());

            var sr = new GameSearchResult
            {
                PlatformNames = platforms,
                ReleaseDate = MobyGamesHelper.ParseReleaseDate(releaseDateString),
            };
            sr.SetUrlAndId(a.Attributes["href"].Value);
            sr.SetName(a.InnerText.HtmlDecode(), alternateNames);
            sr.SetDescription(releaseDateString, platforms);
            yield return sr;
        }
    }

    private IEnumerable<GroupSearchResult> ParseGroupSearchResultHtml(string html)
    {
        var page = new HtmlDocument();
        page.LoadHtml(html);

        var cells = page.DocumentNode.SelectNodes("//table[@class='table mb']/tr/td[last()]");
        if (cells == null)
            yield break;

        foreach (var td in cells)
        {
            var a = td.SelectSingleNode(".//a[@href]");
            if (a == null)
                continue;

            var description = td.SelectSingleNode("./span[last()]")?.InnerText.HtmlDecode();

            var sr = new GroupSearchResult { Name = a.InnerText.HtmlDecode(), Description = description };
            sr.SetUrlAndId(a.Attributes["href"].Value);
            yield return sr;
        }
    }

    private GameDetails ParseGameDetailsHtml(string html)
    {
        return new MobyGamesHelper(PlatformUtility).ParseGameDetailsHtml(html, parseGenres: false);
    }
}

public class SearchResult : Playnite.SDK.GenericItemOption, IHasName
{
    public string Url { get; set; }
    public int Id { get; set; }

    public void SetUrlAndId(string url)
    {
        Url = url;
        if (url == null) return;
        var urlSegment = url.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Where(x => x.All(char.IsNumber)).FirstOrDefault();
        if (urlSegment != null)
            Id = int.Parse(urlSegment);
    }
}

public class GroupSearchResult : SearchResult { }

public class GameSearchResult : SearchResult, IGameSearchResult
{
    public List<string> PlatformNames { get; set; } = new List<string>();
    public List<string> AlternateTitles { get; set; } = new List<string>();
    public string Title { get; set; }
    public ReleaseDate? ReleaseDate { get; set; }

    public MobyGame ApiGameResult { get; private set; }

    public IEnumerable<string> AlternateNames => AlternateTitles;

    IEnumerable<string> IGameSearchResult.Platforms => PlatformNames;

    public void SetName(string title, IEnumerable<string> alternateTitles)
    {
        Title = title;
        AlternateTitles = alternateTitles?.ToList() ?? new List<string>();
        if (AlternateTitles.Any())
            Name = $"{Title} (AKA {string.Join("/", AlternateTitles)})";
        else
            Name = Title;
    }

    public void SetDescription(string releaseDate, List<string> platforms)
    {
        var descriptionElements = new List<string>();
        if (!string.IsNullOrWhiteSpace(releaseDate))
            descriptionElements.Add(releaseDate);

        if (platforms != null && platforms.Any())
            descriptionElements.Add(string.Join(", ", platforms));

        Description = string.Join(" | ", descriptionElements);
    }

    public GameSearchResult() { }

    public GameSearchResult(MobyGame game)
    {
        ApiGameResult = game;
        Id = game.game_id;
        Url = game.moby_url;
        SetName(game.title, game.highlights);
        PlatformNames.AddRange(game.platforms.Select(p => p.name));
        ReleaseDate = game.release_date.ParseReleaseDate();
        SetDescription(game.release_date, PlatformNames);
    }
}
