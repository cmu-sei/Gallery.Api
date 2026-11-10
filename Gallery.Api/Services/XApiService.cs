// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license, please see LICENSE.md in the project root for license information or contact permission@sei.cmu.edu for full terms.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Gallery.Api.Data;
using Gallery.Api.ViewModels;
using Gallery.Api.Infrastructure.Extensions;
using Gallery.Api.Infrastructure.Authorization;
using Gallery.Api.Infrastructure.Exceptions;
using Gallery.Api.Infrastructure.Options;
using TinCan;
using Gallery.Api.Data.Models;

namespace Gallery.Api.Services
{
    public interface IXApiService
    {
        Boolean IsConfigured();
        Task<Boolean> ArticleViewedAsync(Exhibit exhibit, Article article, Card card, Collection collection, CancellationToken ct);
        Task<Boolean> ArticlePreviewedAsync(Exhibit exhibit, Article article, Card card, Collection collection, CancellationToken ct);
        Task<Boolean> CardViewedAsync(Card card, Exhibit exhibit, Collection collection, CancellationToken ct);
        Task<Boolean> ExhibitArchiveViewedAsync(Exhibit exhibit, Collection collection, CancellationToken ct);
        Task<Boolean> ExhibitWallViewedAsync(Exhibit exhibit, Collection collection, CancellationToken ct);
        Task<Boolean> ExhibitArchiveObservedAsync(Exhibit exhibit, Collection collection, Team team, CancellationToken ct);
        Task<Boolean> ExhibitWallObservedAsync(Exhibit exhibit, Collection collection, Team team, CancellationToken ct);
        Task<Boolean> CreateAsync(
            Uri verb,
            Dictionary<String,String> activityData,
            Dictionary<String,String> categoryData,
            List<Dictionary<String,String>> groupingData,
            Dictionary<String,String> parentData,
            Dictionary<String,String> otherData,
            Guid teamId,
            CancellationToken ct);
    }

    public class XApiService : IXApiService
    {
        private readonly GalleryDbContext _context;
        private readonly ClaimsPrincipal _user;
        private readonly IAuthorizationService _authorizationService;
        private readonly XApiOptions _xApiOptions;
        private readonly IXApiQueueService _queueService;
        private Agent _agent;
        private AgentAccount _account;
        private Context _xApiContext;
        private readonly ILogger<XApiService> _logger;

        public XApiService(
            GalleryDbContext context,
            IPrincipal user,
            IAuthorizationService authorizationService,
            XApiOptions xApiOptions,
            IXApiQueueService queueService,
            ILogger<XApiService> logger)
        {
            _context = context;
            _user = user as ClaimsPrincipal;
            _authorizationService = authorizationService;
            _xApiOptions = xApiOptions;
            _queueService = queueService;
            _logger = logger;
        }

        private void EnsureAgentInitialized()
        {
            if (_agent != null || !IsConfigured())
                return;

            // configure AgentAccount
            _account = new TinCan.AgentAccount();
            _account.Name = _user.Identities.First().Claims.First(c => c.Type == "sub")?.Value;
            var iss = _user.Identities.First().Claims.First(c => c.Type == "iss")?.Value;
            if (_xApiOptions.IssuerUrl != "")
            {
                _account.HomePage = new Uri(_xApiOptions.IssuerUrl);
            }
            else if (iss.Contains("http"))
            {
                _account.HomePage = new Uri(iss);
            }
            else if (_xApiOptions.IssuerUrl == "")
            {
                _account.HomePage = new Uri("http://" + iss);
            }

            // configure Agent
            _agent = new TinCan.Agent();
            _agent.Name = _context.Users.Find(_user.GetId()).Name;
            _agent.Account = _account;

            // Initialize the Context
            _xApiContext = new Context();
            _xApiContext.Platform = _xApiOptions.Platform;
            _xApiContext.Language = "en-US";
        }

        public Boolean IsConfigured()
        {
            return !string.IsNullOrWhiteSpace(_xApiOptions.Username);
        }

        public async Task<Boolean> ArticleViewedAsync(Exhibit exhibit, Article article, Card card, Collection collection, CancellationToken ct)
        {
            var verb = new Uri("http://id.tincanapi.com/verb/viewed");

            var teamId = (_context.TeamUsers
                .SingleOrDefault(tu => tu.UserId == _user.GetId() && tu.Team.ExhibitId == exhibit.Id)).TeamId;

            var activity = new Dictionary<String,String>();
            activity.Add("id", article.Id.ToString());
            activity.Add("name", article.Name);
            activity.Add("description", article.Summary);
            activity.Add("type", "article");
            activity.Add("activityType", "http://id.tincanapi.com/activitytype/resource");
            activity.Add("moreInfo", "/exhibit/" + exhibit.Id.ToString() + "/article/" + article.Id.ToString());

            var parent = new Dictionary<String,String>();
            parent.Add("id", exhibit.Id.ToString());
            parent.Add("name", "Exhibit");
            parent.Add("description", collection.Name);
            parent.Add("type", "exhibit");
            parent.Add("activityType", "http://adlnet.gov/expapi/activities/simulation");
            parent.Add("moreInfo", "/?exhibit=" + exhibit.Id.ToString());

            var category = new Dictionary<String,String>();
            category.Add("id", article.SourceType.ToString());
            category.Add("name", article.SourceType.ToString());
            category.Add("description", "The source type for the article.");
            category.Add("type", "sourceType");
            category.Add("activityType", "http://id.tincanapi.com/activitytype/category");
            category.Add("moreInfo", "");

            // Use grouping for move/inject context (scenario phase) - separate entries for each
            var grouping = new List<Dictionary<String,String>>();

            // Move grouping entry
            var moveGrouping = new Dictionary<String,String>();
            moveGrouping.Add("id", article.Move.ToString());
            moveGrouping.Add("name", $"Move {article.Move}");
            moveGrouping.Add("description", "");
            moveGrouping.Add("type", $"exhibit/{exhibit.Id}/move");
            moveGrouping.Add("activityType", "http://id.tincanapi.com/activitytype/collection-simple");
            moveGrouping.Add("moreInfo", "");
            grouping.Add(moveGrouping);

            // Inject grouping entry
            var injectGrouping = new Dictionary<String,String>();
            injectGrouping.Add("id", article.Inject.ToString());
            injectGrouping.Add("name", $"Inject {article.Inject}");
            injectGrouping.Add("description", "");
            injectGrouping.Add("type", $"exhibit/{exhibit.Id}/inject");
            injectGrouping.Add("activityType", "http://id.tincanapi.com/activitytype/step");
            injectGrouping.Add("moreInfo", "");
            grouping.Add(injectGrouping);

            // Move card to other context
            var other = new Dictionary<String,String>();
            other.Add("id", card.Id.ToString());
            other.Add("name", card.Name);
            other.Add("description", card.Description);
            other.Add("type", "card");
            other.Add("activityType", "http://id.tincanapi.com/activitytype/collection-simple");
            other.Add("moreInfo", "/?section=archive&exhibit=" + exhibit.Id.ToString() + "&card=" + card.Id.ToString());

            return await CreateAsync(
                verb, activity, category, grouping, parent, other, teamId, ct);

        }

        public async Task<Boolean> ArticlePreviewedAsync(Exhibit exhibit, Article article, Card card, Collection collection, CancellationToken ct)
        {
            var verb = new Uri("http://id.tincanapi.com/verb/previewed");

            var teamId = (_context.TeamUsers
                .SingleOrDefault(tu => tu.UserId == _user.GetId() && tu.Team.ExhibitId == exhibit.Id)).TeamId;

            var activity = new Dictionary<String,String>();
            activity.Add("id", article.Id.ToString());
            activity.Add("name", article.Name);
            activity.Add("description", article.Summary);
            activity.Add("type", "article");
            activity.Add("activityType", "http://id.tincanapi.com/activitytype/resource");
            activity.Add("moreInfo", "/exhibit/" + exhibit.Id.ToString() + "/article/" + article.Id.ToString());

            var parent = new Dictionary<String,String>();
            parent.Add("id", exhibit.Id.ToString());
            parent.Add("name", "Exhibit");
            parent.Add("description", collection.Name);
            parent.Add("type", "exhibit");
            parent.Add("activityType", "http://adlnet.gov/expapi/activities/simulation");
            parent.Add("moreInfo", "/?exhibit=" + exhibit.Id.ToString());

            var category = new Dictionary<String,String>();
            category.Add("id", article.SourceType.ToString());
            category.Add("name", article.SourceType.ToString());
            category.Add("description", "The source type for the article.");
            category.Add("type", "sourceType");
            category.Add("activityType", "http://id.tincanapi.com/activitytype/category");
            category.Add("moreInfo", "");

            // Use grouping for move/inject context (scenario phase) - separate entries for each
            var grouping = new List<Dictionary<String,String>>();

            // Move grouping entry
            var moveGrouping = new Dictionary<String,String>();
            moveGrouping.Add("id", article.Move.ToString());
            moveGrouping.Add("name", $"Move {article.Move}");
            moveGrouping.Add("description", "");
            moveGrouping.Add("type", $"exhibit/{exhibit.Id}/move");
            moveGrouping.Add("activityType", "http://id.tincanapi.com/activitytype/collection-simple");
            moveGrouping.Add("moreInfo", "");
            grouping.Add(moveGrouping);

            // Inject grouping entry
            var injectGrouping = new Dictionary<String,String>();
            injectGrouping.Add("id", article.Inject.ToString());
            injectGrouping.Add("name", $"Inject {article.Inject}");
            injectGrouping.Add("description", "");
            injectGrouping.Add("type", $"exhibit/{exhibit.Id}/inject");
            injectGrouping.Add("activityType", "http://id.tincanapi.com/activitytype/step");
            injectGrouping.Add("moreInfo", "");
            grouping.Add(injectGrouping);

            // Move card to other context
            var other = new Dictionary<String,String>();
            other.Add("id", card.Id.ToString());
            other.Add("name", card.Name);
            other.Add("description", card.Description);
            other.Add("type", "card");
            other.Add("activityType", "http://id.tincanapi.com/activitytype/collection-simple");
            other.Add("moreInfo", "/?section=archive&exhibit=" + exhibit.Id.ToString() + "&card=" + card.Id.ToString());

            return await CreateAsync(
                verb, activity, category, grouping, parent, other, teamId, ct);

        }

        public async Task<Boolean> CardViewedAsync(Card card, Exhibit exhibit, Collection collection, CancellationToken ct)
        {
            var verb = new Uri("http://id.tincanapi.com/verb/viewed");
            //var teamId = new Guid();

            var teamUser = await _context.TeamUsers
                .Include(tu => tu.Team)
                .SingleOrDefaultAsync(tu => tu.UserId == _user.GetId() && tu.Team.ExhibitId == exhibit.Id, ct);
            var teamId = teamUser?.TeamId ?? Guid.Empty;

            var activity = new Dictionary<String,String>();

            activity.Add("id", card.Id.ToString());
            activity.Add("name", card.Name);
            activity.Add("description", card.Description);
            activity.Add("type", "card");
            activity.Add("activityType", "http://id.tincanapi.com/activitytype/resource");
            activity.Add("moreInfo", "/?exhbit=" + exhibit.Id.ToString() + "&card=" + card.Id.ToString());

            var parent = new Dictionary<String,String>();
            parent.Add("id", exhibit.Id.ToString());
            parent.Add("name", "Exhibit");
            parent.Add("description", collection.Name);
            parent.Add("type", "exhibit");
            parent.Add("activityType", "http://adlnet.gov/expapi/activities/simulation");
            parent.Add("moreInfo", "/?exhibit=" + exhibit.Id.ToString());

            var category = new Dictionary<String,String>();
            var grouping = new List<Dictionary<String,String>>();
            var other = new Dictionary<String,String>();

            return await CreateAsync(
                verb, activity, category, grouping, parent, other, teamId, ct);

        }

        public async Task<Boolean> ExhibitArchiveViewedAsync(Exhibit exhibit,Collection collection, CancellationToken ct)
        {
            var verb = new Uri("http://id.tincanapi.com/verb/viewed");

            var teamUser = await _context.TeamUsers
                .Include(tu => tu.Team)
                .SingleOrDefaultAsync(tu => tu.UserId == _user.GetId() && tu.Team.ExhibitId == exhibit.Id, ct);
            var teamId = teamUser?.TeamId ?? Guid.Empty;

            var activity = new Dictionary<String,String>();

            activity.Add("id", exhibit.Id.ToString());
            activity.Add("name", "Archive");
            activity.Add("description", "The Gallery Archive is a collection of information that contains relevant reporting, intelligence, news, and social media data sources.");
            activity.Add("type", "exhibit");
            activity.Add("activityType", "http://id.tincanapi.com/activitytype/resource");
            activity.Add("moreInfo", "?section=archive&exhibit=" + exhibit.Id.ToString());

            var parent = new Dictionary<String,String>();

            parent.Add("id", exhibit.Id.ToString());
            parent.Add("name", collection.Name);
            parent.Add("description", collection.Description);
            parent.Add("type", "exhibit");
            parent.Add("activityType", "http://id.tincanapi.com/activitytype/resource");
            parent.Add("moreInfo", "?exhibit=" + exhibit.Id.ToString());

            var category = new Dictionary<String,String>();
            var grouping = new List<Dictionary<String,String>>();
            var other = new Dictionary<String,String>();

            if (exhibit.CurrentMove >= 0 || exhibit.CurrentInject >= 0)
            {
                var moveGrouping = new Dictionary<String,String>();
                moveGrouping.Add("id", exhibit.CurrentMove.ToString());
                moveGrouping.Add("name", $"Move {exhibit.CurrentMove}");
                moveGrouping.Add("description", "");
                moveGrouping.Add("type", $"exhibit/{exhibit.Id}/move");
                moveGrouping.Add("activityType", "http://id.tincanapi.com/activitytype/collection-simple");
                moveGrouping.Add("moreInfo", "");
                grouping.Add(moveGrouping);

                var injectGrouping = new Dictionary<String,String>();
                injectGrouping.Add("id", exhibit.CurrentInject.ToString());
                injectGrouping.Add("name", $"Inject {exhibit.CurrentInject}");
                injectGrouping.Add("description", "");
                injectGrouping.Add("type", $"exhibit/{exhibit.Id}/inject");
                injectGrouping.Add("activityType", "http://id.tincanapi.com/activitytype/step");
                injectGrouping.Add("moreInfo", "");
                grouping.Add(injectGrouping);
            }

            return await CreateAsync(
                verb, activity, category, grouping, parent, other, teamId, ct);

        }

        public async Task<Boolean> ExhibitWallViewedAsync(Exhibit exhibit, Collection collection, CancellationToken ct)
        {
            var verb = new Uri("http://id.tincanapi.com/verb/viewed");

            var teamUser = await _context.TeamUsers
                .Include(tu => tu.Team)
                .SingleOrDefaultAsync(tu => tu.UserId == _user.GetId() && tu.Team.ExhibitId == exhibit.Id, ct);
            var teamId = teamUser?.TeamId ?? Guid.Empty;

            var activity = new Dictionary<String,String>();

            activity.Add("id", exhibit.Id.ToString());
            activity.Add("name", "Wall");
            activity.Add("description", "The Gallery Wall is a dashboard with red, orange, yellow, and green status indicators.");
            activity.Add("type", "exhibit");
            activity.Add("activityType", "http://id.tincanapi.com/activitytype/resource");
            activity.Add("moreInfo", "?section=wall&exhibit=" + exhibit.Id.ToString());

            var parent = new Dictionary<String,String>();

            parent.Add("id", exhibit.Id.ToString());
            parent.Add("name", collection.Name);
            parent.Add("description", collection.Description);
            parent.Add("type", "exhibit");
            parent.Add("activityType", "http://id.tincanapi.com/activitytype/resource");
            parent.Add("moreInfo", "?exhibit=" + exhibit.Id.ToString());

            var category = new Dictionary<String,String>();
            var grouping = new List<Dictionary<String,String>>();
            var other = new Dictionary<String,String>();

            if (exhibit.CurrentMove >= 0 || exhibit.CurrentInject >= 0)
            {
                var moveGrouping = new Dictionary<String,String>();
                moveGrouping.Add("id", exhibit.CurrentMove.ToString());
                moveGrouping.Add("name", $"Move {exhibit.CurrentMove}");
                moveGrouping.Add("description", "");
                moveGrouping.Add("type", $"exhibit/{exhibit.Id}/move");
                moveGrouping.Add("activityType", "http://id.tincanapi.com/activitytype/collection-simple");
                moveGrouping.Add("moreInfo", "");
                grouping.Add(moveGrouping);

                var injectGrouping = new Dictionary<String,String>();
                injectGrouping.Add("id", exhibit.CurrentInject.ToString());
                injectGrouping.Add("name", $"Inject {exhibit.CurrentInject}");
                injectGrouping.Add("description", "");
                injectGrouping.Add("type", $"exhibit/{exhibit.Id}/inject");
                injectGrouping.Add("activityType", "http://id.tincanapi.com/activitytype/step");
                injectGrouping.Add("moreInfo", "");
                grouping.Add(injectGrouping);
            }

            return await CreateAsync(
                verb, activity, category, grouping, parent, other, teamId, ct);

        }
        public async Task<Boolean> ExhibitArchiveObservedAsync(Exhibit exhibit,Collection collection, Team team, CancellationToken ct)
        {
            var verb = new Uri("https://w3id.org/xapi/dod-isd/verbs/observed");

            var teamId = team.Id;

            var activity = new Dictionary<String,String>();

            activity.Add("id", exhibit.Id.ToString());
            activity.Add("name", "Archive");
            activity.Add("description", "The Gallery Archive is a collection of information that contains relevant reporting, intelligence, news, and social media data sources.");
            activity.Add("type", "exhibit");
            activity.Add("activityType", "http://id.tincanapi.com/activitytype/resource");
            activity.Add("moreInfo", "?section=archive&exhibit=" + exhibit.Id.ToString());

            var parent = new Dictionary<String,String>();

            parent.Add("id", exhibit.Id.ToString());
            parent.Add("name", collection.Name);
            parent.Add("description", collection.Description);
            parent.Add("type", "exhibit");
            parent.Add("activityType", "http://id.tincanapi.com/activitytype/resource");
            parent.Add("moreInfo", "?exhibit=" + exhibit.Id.ToString());

            var category = new Dictionary<String,String>();
            var grouping = new List<Dictionary<String,String>>();
            var other = new Dictionary<String,String>();

            if (exhibit.CurrentMove >= 0 || exhibit.CurrentInject >= 0)
            {
                var moveGrouping = new Dictionary<String,String>();
                moveGrouping.Add("id", exhibit.CurrentMove.ToString());
                moveGrouping.Add("name", $"Move {exhibit.CurrentMove}");
                moveGrouping.Add("description", "");
                moveGrouping.Add("type", $"exhibit/{exhibit.Id}/move");
                moveGrouping.Add("activityType", "http://id.tincanapi.com/activitytype/collection-simple");
                moveGrouping.Add("moreInfo", "");
                grouping.Add(moveGrouping);

                var injectGrouping = new Dictionary<String,String>();
                injectGrouping.Add("id", exhibit.CurrentInject.ToString());
                injectGrouping.Add("name", $"Inject {exhibit.CurrentInject}");
                injectGrouping.Add("description", "");
                injectGrouping.Add("type", $"exhibit/{exhibit.Id}/inject");
                injectGrouping.Add("activityType", "http://id.tincanapi.com/activitytype/step");
                injectGrouping.Add("moreInfo", "");
                grouping.Add(injectGrouping);
            }

            return await CreateAsync(
                verb, activity, category, grouping, parent, other, teamId, ct);

        }

        public async Task<Boolean> ExhibitWallObservedAsync(Exhibit exhibit, Collection collection, Team team, CancellationToken ct)
        {
            var verb = new Uri("https://w3id.org/xapi/dod-isd/verbs/observed");

            var teamId = team.Id;

            var activity = new Dictionary<String,String>();

            activity.Add("id", exhibit.Id.ToString());
            activity.Add("name", "Wall");
            activity.Add("description", "The Gallery Wall is a dashboard with red, orange, yellow, and green status indicators.");
            activity.Add("type", "exhibit");
            activity.Add("activityType", "http://id.tincanapi.com/activitytype/resource");
            activity.Add("moreInfo", "?section=wall&exhibit=" + exhibit.Id.ToString());

            var parent = new Dictionary<String,String>();

            parent.Add("id", exhibit.Id.ToString());
            parent.Add("name", collection.Name);
            parent.Add("description", collection.Description);
            parent.Add("type", "exhibit");
            parent.Add("activityType", "http://id.tincanapi.com/activitytype/resource");
            parent.Add("moreInfo", "?exhibit=" + exhibit.Id.ToString());

            var category = new Dictionary<String,String>();
            var grouping = new List<Dictionary<String,String>>();
            var other = new Dictionary<String,String>();

            if (exhibit.CurrentMove >= 0 || exhibit.CurrentInject >= 0)
            {
                var moveGrouping = new Dictionary<String,String>();
                moveGrouping.Add("id", exhibit.CurrentMove.ToString());
                moveGrouping.Add("name", $"Move {exhibit.CurrentMove}");
                moveGrouping.Add("description", "");
                moveGrouping.Add("type", $"exhibit/{exhibit.Id}/move");
                moveGrouping.Add("activityType", "http://id.tincanapi.com/activitytype/collection-simple");
                moveGrouping.Add("moreInfo", "");
                grouping.Add(moveGrouping);

                var injectGrouping = new Dictionary<String,String>();
                injectGrouping.Add("id", exhibit.CurrentInject.ToString());
                injectGrouping.Add("name", $"Inject {exhibit.CurrentInject}");
                injectGrouping.Add("description", "");
                injectGrouping.Add("type", $"exhibit/{exhibit.Id}/inject");
                injectGrouping.Add("activityType", "http://id.tincanapi.com/activitytype/step");
                injectGrouping.Add("moreInfo", "");
                grouping.Add(injectGrouping);
            }

            return await CreateAsync(
                verb, activity, category, grouping, parent, other, teamId, ct);

        }



        public async Task<Boolean> CreateAsync(
            Uri verbUri,
            Dictionary<String,String> activityData,
            Dictionary<String,String> categoryData,
            List<Dictionary<String,String>> groupingData,
            Dictionary<String,String> parentData,
            Dictionary<String,String> otherData,
            Guid teamId,
            CancellationToken ct)
        {
            if (!IsConfigured())
            {
                _logger.LogInformation("xAPI Service not configured");
                return true;
            }

            // Initialize agent lazily (only when needed, not in constructor)
            EnsureAgentInitialized();

            var verb = new Verb();
            verb.Id = verbUri;
            verb.Display = new LanguageMap();
            verb.Display.Add("en-US", verb.Id.Segments.Last());

            var activity = new Activity();
            activity.Id = _xApiOptions.ApiUrl + activityData["type"] + "/" + activityData["id"];
            activity.Definition = new TinCan.ActivityDefinition();
            activity.Definition.Type = new Uri(activityData["activityType"]);
            if (activityData.ContainsKey("moreInfo")) {
                activity.Definition.MoreInfo = new Uri(_xApiOptions.UiUrl.TrimEnd('/') + activityData["moreInfo"]);
            }
            activity.Definition.Name = new LanguageMap();
            activity.Definition.Name.Add("en-US", activityData["name"]);
            activity.Definition.Description = new LanguageMap();
            if (!string.IsNullOrEmpty(activityData["description"])) {
                activity.Definition.Description.Add("en-US", activityData["description"]);
            }

            var context = new Context();
            context.Platform = _xApiContext.Platform;
            context.Language = _xApiContext.Language;

            // Set registration to exhibit ID if available (groups statements by exhibit session)
            if (parentData.Count() > 0 && parentData.ContainsKey("type") && parentData.ContainsKey("id")) {
                var parentType = parentData["type"].ToLower();
                if (parentType == "exhibit" || parentType == "exhibits") {
                    context.Registration = Guid.Parse(parentData["id"]);
                }
            }

            if (teamId != Guid.Empty) {
                var team = _context.Teams.Find(teamId);
                if (team == null) return false;
                var group = new TinCan.Group();
                group.Name = team.ShortName;
                if (_xApiOptions.EmailDomain != "") {
                    // this is being set but not logged inside the lrs
                    group.Mbox = "mailto:" + team.ShortName + "@" + _xApiOptions.EmailDomain;
                }
                group.Account = new AgentAccount();
                group.Account.HomePage = new Uri(_xApiOptions.UiUrl);
                group.Account.Name = team.Id.ToString();;
                group.Member = new List<Agent> {};
                if (verb.Id.Segments.Last() != "observed") {
                    group.Member.Add(_agent);
                }
                context.Team = group;
            }

            var contextActivities = new ContextActivities();
            context.ContextActivities = contextActivities;

            if (parentData.Count() > 0) {
                var parent = new Activity();
                parent.Id = _xApiOptions.ApiUrl + parentData["type"] + "/" + parentData["id"];
                parent.Definition = new ActivityDefinition();
                parent.Definition.Name = new LanguageMap();
                parent.Definition.Name.Add("en-US", parentData["name"]);
                parent.Definition.Description = new LanguageMap();
                if (!string.IsNullOrEmpty(parentData["description"])) {
                    parent.Definition.Description.Add("en-US", parentData["description"]);
                }
                parent.Definition.Type = new Uri(parentData["activityType"]);
                if (parentData.ContainsKey("moreInfo")) {
                    parent.Definition.MoreInfo = new Uri(_xApiOptions.UiUrl.TrimEnd('/') + parentData["moreInfo"]);
                }
                contextActivities.Parent = new List<Activity>();
                contextActivities.Parent.Add(parent);
            }

            if (otherData.Count() > 0) {
                var other = new TinCan.Activity();
                other.Id = _xApiOptions.ApiUrl  + otherData["type"] + "/" + otherData["id"];
                other.Definition = new ActivityDefinition();
                other.Definition.Name = new LanguageMap();
                other.Definition.Name.Add("en-US", otherData["name"]);
                other.Definition.Description = new LanguageMap();
                if (!string.IsNullOrEmpty(otherData["description"])) {
                    other.Definition.Description.Add("en-US", otherData["description"]);
                }
                other.Definition.Type = new Uri(otherData["activityType"]);
                if (otherData.ContainsKey("moreInfo")) {
                    other.Definition.MoreInfo = new Uri(_xApiOptions.UiUrl.TrimEnd('/') + otherData["moreInfo"]);
                }
                contextActivities.Other = new List<Activity>();
                context.ContextActivities.Other.Add(other);
            }
            //var extensions = new Extensions();
            //context.Extensions = new TinCan.Extensions();

            if (groupingData != null && groupingData.Count() > 0) {
                contextActivities.Grouping = new List<Activity>();
                foreach (var groupingItem in groupingData) {
                    if (groupingItem.Count() > 0) {
                        var grouping = new TinCan.Activity();
                        grouping.Id = _xApiOptions.ApiUrl  + groupingItem["type"] + "/" + groupingItem["id"];
                        grouping.Definition = new ActivityDefinition();
                        grouping.Definition.Name = new LanguageMap();
                        grouping.Definition.Name.Add("en-US", groupingItem["name"]);
                        grouping.Definition.Description = new LanguageMap();
                        if (!string.IsNullOrEmpty(groupingItem["description"])) {
                            grouping.Definition.Description.Add("en-US", groupingItem["description"]);
                        }
                        grouping.Definition.Type = new Uri(groupingItem["activityType"]);
                        if (groupingItem.ContainsKey("moreInfo") && !string.IsNullOrEmpty(groupingItem["moreInfo"])) {
                            grouping.Definition.MoreInfo = new Uri(_xApiOptions.UiUrl.TrimEnd('/') + groupingItem["moreInfo"]);
                        }
                        context.ContextActivities.Grouping.Add(grouping);
                    }
                }
            }

            if (categoryData.Count() > 0) {
                var category = new TinCan.Activity();
                category.Id = _xApiOptions.ApiUrl  + categoryData["type"] + "/" + categoryData["id"];
                category.Definition = new ActivityDefinition();
                category.Definition.Name = new LanguageMap();
                category.Definition.Name.Add("en-US", categoryData["name"]);
                category.Definition.Description = new LanguageMap();
                if (!string.IsNullOrEmpty(categoryData["description"])) {
                    category.Definition.Description.Add("en-US", categoryData["description"]);
                }
                category.Definition.Type = new Uri(categoryData["activityType"]);
                if (categoryData.ContainsKey("moreInfo")) {
                    category.Definition.MoreInfo = new Uri(_xApiOptions.UiUrl.TrimEnd('/') + categoryData["moreInfo"]);
                }
                contextActivities.Category = new List<Activity>();
                context.ContextActivities.Category.Add(category);
            }

            var statement = new Statement();
            statement.Actor = _agent;
            statement.Verb = verb;
            statement.Target = activity;
            statement.Context = context;

            // TODO pass in separately
            if (activityData.ContainsKey("result"))
            {
                var result = new Result();
                result.Response = activityData["result"];
                statement.Result = result;
            }

            // Serialize statement to JSON and enqueue for background processing
            var statementJson = statement.ToJson();

            var queuedStatement = new Data.Models.XApiQueuedStatementEntity
            {
                Id = Guid.NewGuid(),
                StatementJson = statementJson,
                Verb = verb.Id.Segments.Last(),
                ActivityId = activity.Id,
                TeamId = teamId != Guid.Empty ? teamId : null
            };

            await _queueService.EnqueueAsync(queuedStatement, ct);
            _logger.LogInformation("Enqueued xAPI statement for verb {Verb}", verb.Id.Segments.Last());

            return true;
        }


    }
}
