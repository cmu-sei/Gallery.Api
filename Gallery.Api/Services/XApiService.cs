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
using Gallery.Api.Data;
using Gallery.Api.Infrastructure.Extensions;
using Gallery.Api.Infrastructure.Authorization;
using Gallery.Api.Infrastructure.Exceptions;
using Gallery.Api.Infrastructure.Options;
using TinCan;

namespace Gallery.Api.Services
{
    public interface IXApiService
    {
        Boolean IsConfigured();
        Task<Boolean> CreateAsync(Uri verb, Dictionary<String,String> activityData, Dictionary<String,String> parentData, Dictionary<String,String> otherData, Guid teamId, CancellationToken ct);
    }

    public class XApiService : IXApiService
    {
        private readonly GalleryDbContext _context;
        private readonly ClaimsPrincipal _user;
        private readonly IAuthorizationService _authorizationService;
        private readonly XApiOptions _xApiOptions;
        private readonly RemoteLRS _lrs;
        private readonly Agent _agent;
        private readonly AgentAccount _account;
        private readonly Context _xApiContext;
        public XApiService(GalleryDbContext context, IPrincipal user, IAuthorizationService authorizationService, XApiOptions xApiOptions)
        {
            _context = context;
            _user = user as ClaimsPrincipal;
            _authorizationService = authorizationService;
            _xApiOptions = xApiOptions;

            if (IsConfigured()) {
                // configure LRS
                _lrs = new TinCan.RemoteLRS(_xApiOptions.Endpoint, _xApiOptions.Username, _xApiOptions.Password);

                // configure AgentAccount
                _account = new TinCan.AgentAccount();
                _account.name = _user.Identities.First().Claims.First(c => c.Type == "sub")?.Value;
                var iss = _user.Identities.First().Claims.First(c => c.Type == "iss")?.Value;
                if (_xApiOptions.IssuerUrl != "") {
                    _account.homePage = new Uri(_xApiOptions.IssuerUrl);
                } else if (iss.Contains("http")) {
                    _account.homePage = new Uri(iss);
                } else if (_xApiOptions.IssuerUrl == "") {
                    _account.homePage = new Uri("http://" + iss);
                }

                // configure Agent
                _agent = new TinCan.Agent();
                _agent.name = _context.Users.Find(_user.GetId()).Name;
                _agent.account = _account;

                // Initialize the Context
                _xApiContext = new Context();
                _xApiContext.platform = "Gallery";
                _xApiContext.language = "en-US";

            }
        }

        public Boolean IsConfigured()
        {
            return _xApiOptions.Username != null;
        }

        public async Task<Boolean> CreateAsync(Uri verbUri, Dictionary<String,String> activityData, Dictionary<String,String> parentData, Dictionary<String,String> otherData, Guid teamId, CancellationToken ct)
        {
            if (!IsConfigured())
            {
                return false;
            };

            if (!(await _authorizationService.AuthorizeAsync(_user, null, new BaseUserRequirement())).Succeeded)
                throw new ForbiddenException();

            var verb = new Verb();
            verb.id = verbUri;
            verb.display = new LanguageMap();
            verb.display.Add("en-US", verb.id.Segments.Last());

            var activity = new Activity();
            activity.id = _xApiOptions.ApiUrl + activityData["type"] + "/" + activityData["id"];
            activity.definition = new TinCan.ActivityDefinition();
            activity.definition.type = new Uri(activityData["activityType"]);
            if (activityData["moreInfo"] != "") {
                activity.definition.moreInfo = new Uri(_xApiOptions.UiUrl + activityData["moreInfo"]);
            } else {
                activity.definition.moreInfo = new Uri(_xApiOptions.UiUrl + "/?" + activityData["type"] + "=?" + activityData["id"]);
            }
            activity.definition.name = new LanguageMap();
            activity.definition.name.Add("en-US", activityData["name"]);
            activity.definition.description = new LanguageMap();
            activity.definition.description.Add("en-US", activityData["description"]);

            var context = _xApiContext;

            if (teamId.ToString() !=  "") {
                var team = _context.Teams.Find(teamId);
                var group = new TinCan.Group();
                group.name = team.Id.ToString();
                if (_xApiOptions.EmailDomain != "") {
                    // this is being set but not logged inside the lrs
                    group.mbox = "mailto:" + team.ShortName + "@" + _xApiOptions.EmailDomain;
                }
                group.account = new AgentAccount();
                group.account.homePage = new Uri(_xApiOptions.UiUrl);
                if (_xApiOptions.UiUrl != "") {
                    group.account.homePage = new Uri(_xApiOptions.UiUrl);
                }

                group.account.name = team.ShortName;
                group.member = new List<Agent> {};
                group.member.Add(_agent);

                context.team = group;

            }

            var parent = new Activity();
            parent.id = _xApiOptions.ApiUrl + parentData["type"] + "/" + parentData["id"];
            parent.definition = new ActivityDefinition();
            parent.definition.name = new LanguageMap();
            parent.definition.name.Add("en-US", parentData["name"]);
            parent.definition.description = new LanguageMap();
            parent.definition.description.Add("en-US", parentData["description"]);
            parent.definition.type = new Uri(parentData["activityType"]);
            if (parentData["moreInfo"] != "") {
                parent.definition.moreInfo = new Uri(_xApiOptions.UiUrl + parentData["moreInfo"]);
            } else {
                parent.definition.moreInfo = new Uri(_xApiOptions.UiUrl + "/?" + parentData["type"] + "=?" + activityData["id"]);
            }

            var contextActivities = new ContextActivities();
            contextActivities.parent = new List<Activity>();
            contextActivities.parent.Add(parent);
            context.contextActivities = contextActivities;

            var other = new TinCan.Activity();
            other.id = _xApiOptions.ApiUrl  + otherData["type"] + "/" + otherData["id"];
            other.definition = new ActivityDefinition();
            other.definition.name = new LanguageMap();
            other.definition.name.Add("en-US", otherData["name"]);
            other.definition.description = new LanguageMap();
            other.definition.description.Add("en-US", otherData["description"]);
            other.definition.type = new Uri(otherData["activityType"]);
            if (otherData["moreInfo"] != "") {
                other.definition.moreInfo = new Uri(_xApiOptions.UiUrl + otherData["moreInfo"]);
            } else {
                other.definition.moreInfo = new Uri(_xApiOptions.UiUrl + "/?" + otherData["type"] + "=?" + activityData["id"]);
            }
            context.contextActivities.other = new List<Activity>();
            context.contextActivities.other.Add(other);

            //var extensions = new Extensions();
            //context.extensions = new TinCan.Extensions();

            var statement = new Statement();
            statement.actor = _agent;
            statement.verb = verb;
            statement.target = activity;
            statement.context = context;

            // TODO pass in separately
            var result = new Result();
            result.response = activityData["result"];
            statement.result = result;

            TinCan.LRSResponses.StatementLRSResponse lrsStatementResponse = _lrs.SaveStatement(statement);
            if (lrsStatementResponse.success)
            {
                // List of statements available
                Console.WriteLine("LRS saved statesment from xAPI Service");
            } else {
                Console.WriteLine("ERROR FROM LRS VIA XAPI SERVICE: " + lrsStatementResponse.errMsg);
                return false;
            }

            return true;
        }


    }
}

