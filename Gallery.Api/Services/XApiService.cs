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
        Task<Boolean> CreateAsync(String verb, String description, Guid exhibitId, Guid teamId, CancellationToken ct);
    }

    public class XApiService : IXApiService
    {
        private readonly GalleryDbContext _context;
        private readonly ClaimsPrincipal _user;
        private readonly IAuthorizationService _authorizationService;
        private readonly XApiOptions _xApiOptions;
        private readonly RemoteLRS _lrs;
        private readonly Verb _verb;
        private readonly AgentAccount _account;
        private readonly Activity _activity;
        private readonly Agent _agent;
        private readonly Statement _statement;
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

                // Initalilze Verb and Activity
                _verb = new TinCan.Verb();
                _verb.display = new LanguageMap();
                _activity = new TinCan.Activity();

                // Initialize the Context
                _xApiContext = new Context();
                _xApiContext.platform = "Gallery";
                _xApiContext.language = "en-US";

                // Initalize Statement
                _statement = new TinCan.Statement();
            }
        }

        public Boolean IsConfigured()
        {
            return _xApiOptions.Username != null;
        }

        public async Task<Boolean> CreateAsync(String verb, String description, Guid exhibitId, Guid teamId, CancellationToken ct)
        {
            if (!IsConfigured())
            {
                return false;
            };

            if (!(await _authorizationService.AuthorizeAsync(_user, null, new BaseUserRequirement())).Succeeded)
                throw new ForbiddenException();

            _verb.id = new Uri ("http://adlnet.gov/expapi/verbs/" + verb);
            _verb.display.Add("en-US", verb);

            _activity.id = _xApiOptions.SiteUrl + "/api/exhibit/" + exhibitId;
            _activity.definition = new TinCan.ActivityDefinition();
            _activity.definition.type = new Uri("http://adlnet.gov/expapi/activities/simulation");
            _activity.definition.moreInfo = new Uri(_xApiOptions.SiteUrl + "/?exhibit=?" + exhibitId);
            _activity.definition.name = new LanguageMap();
            _activity.definition.name.Add("en-US", description);
            _activity.definition.description = new LanguageMap();
            _activity.definition.description.Add("en-US", description);


            if (teamId.ToString() !=  "") {
                var team = _context.Teams.Find(teamId);
                var group = new TinCan.Group();
                group.name = team.ShortName;
                if (_xApiOptions.EmailDomain != "") {
                    // this is being set but not logged inside the lrs
                    group.mbox = "mailto:" + team.ShortName + "@" + _xApiOptions.EmailDomain;
                }
                group.account = new AgentAccount();
                group.account.homePage = new Uri(_xApiOptions.SiteUrl);
                group.account.name = team.ShortName;
                //group.account.name = teamId.ToString();
                group.member = new List<Agent> {_agent};
                //group.member.Add(_agent);

                _xApiContext.team = group;
            }
            //context.extensions = new TinCan.Extensions();
            //var ext = new TinCan.Extensions();
            //context.extensions.


            _statement.actor = _agent;
            _statement.verb = _verb;
            _statement.target = _activity;
            _statement.context = _xApiContext;

            TinCan.LRSResponses.StatementLRSResponse lrsStatementResponse = _lrs.SaveStatement(_statement);
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

