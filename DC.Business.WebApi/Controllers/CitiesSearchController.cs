﻿using DC.Business.Application.Contracts.Dtos;
using DC.Business.Application.Contracts.Interfaces;
using DC.Business.Domain.ElasticEnteties;
using Microsoft.AspNetCore.Mvc;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DC.Business.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class CitiesSearchController : ControllerBase
    {
        private readonly ICitiesElasticService _citiesElasticService;

        public CitiesSearchController(ICitiesElasticService citiesElasticService)
        {
            _citiesElasticService = citiesElasticService ?? throw new ArgumentNullException(nameof(citiesElasticService));
        }

        //[HttpGet]
        //[Route("/api/search")]
        //public async Task<IActionResult> Query([FromQuery(Name = "q")] string query, CancellationToken cancellationToken)
        //{
        //    var searchResponse = await _citiesElasticService.SearchAsync(query, cancellationToken);
        //    var searchResult = ConvertToSearchResults(query, searchResponse);

        //    return Ok(searchResult);
        //}

        [HttpGet]
        [Route("suggest")]
        public async Task<IActionResult> Suggest([FromQuery(Name = "q")] string query, CancellationToken cancellationToken)
        {
            var searchResponse = await _citiesElasticService.SuggestAsync(query, cancellationToken);
            var searchSuggestions = ConvertToSearchSuggestions(query, searchResponse);

            return Ok(searchSuggestions);
        }

        private SearchSuggestionsDto ConvertToSearchSuggestions(string query, ISearchResponse<ElasticCitiesSuggestionsDto> searchResponse)
        {
            return new SearchSuggestionsDto
            {
                Query = query,
                Results = GetSuggestions(searchResponse)
            };
        }

        private SearchSuggestionDto[] GetSuggestions(ISearchResponse<ElasticCitiesSuggestionsDto> searchResponse)
        {
            if (searchResponse == null)
            {
                return null;
            }

            var suggest = searchResponse.Suggest;

            if (suggest == null)
            {
                return null;
            }

            if (!suggest.ContainsKey("suggest"))
            {
                return null;
            }

            var suggestions = suggest["suggest"];

            if (suggestions == null)
            {
                return null;
            }

            var result = new List<SearchSuggestionDto>();

            foreach (var suggestion in suggestions)
            {
                var offset = suggestion.Offset;
                var length = suggestion.Length;

                foreach (var option in suggestion.Options)
                {
                    var text = option.Text;
                    var prefix = option.Text.Substring(offset, Math.Min(length, text.Length));
                    var highlight = ReplaceAt(option.Text, offset, length, $"<strong>{prefix}</strong>");

                    result.Add(new SearchSuggestionDto { Text = text, Highlight = highlight });
                }
            }

            return result.ToArray();
        }

        public static string ReplaceAt(string str, int index, int length, string replace)
        {
            return str
                .Remove(index, Math.Min(length, str.Length - index))
                .Insert(index, replace);
        }

        //private SearchResultsDto ConvertToSearchResults(string query, ISearchResponse<ElasticCitiesSuggestionsDto> searchResponse)
        //{
        //    var searchResults = searchResponse
        //        // Get the Hits:
        //        .Hits
        //        // Convert the Hit into a SearchResultDto:
        //        .Select(x => new SearchResultDto
        //        {
        //            Identifier = x.Source.Id,
        //            Title = x.Source.Title,
        //            Keywords = x.Source.Keywords,
        //            Matches = GetMatches(x.Highlight)
        //        })
        //        // And convert to array:
        //        .ToArray();

        //    return new SearchResultsDto
        //    {
        //        Query = query,
        //        Results = searchResults
        //    };

        //}

        //private string[] GetMatches(IReadOnlyDictionary<string, IReadOnlyCollection<string>> highlight)
        //{
        //    var matchesForContent = GetMatchesForField(highlight, "attachment.content");

        //    return matchesForContent.ToArray();
        //}

        //private string[] GetMatchesForField(IReadOnlyDictionary<string, IReadOnlyCollection<string>> highlight, string field)
        //{
        //    if (highlight == null)
        //    {
        //        return new string[] { };
        //    }

        //    if (highlight.TryGetValue(field, out var matches))
        //    {
        //        return matches.ToArray();
        //    }

        //    return new string[] { };
        //}
    }

}