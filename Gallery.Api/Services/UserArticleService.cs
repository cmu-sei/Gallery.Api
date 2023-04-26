// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Gallery.Api.Data;
using Gallery.Api.Data.Models;
using Gallery.Api.Infrastructure.Authorization;
using Gallery.Api.Infrastructure.Exceptions;
using Gallery.Api.Infrastructure.Extensions;
using Gallery.Api.Infrastructure.Options;
using Gallery.Api.ViewModels;
using SAC = Steamfitter.Api.Client;
namespace Gallery.Api.Services
{
    public interface IUserArticleService
    {
        Task<IEnumerable<ViewModels.UserArticle>> GetByExhibitAsync(Guid Exhibit, CancellationToken ct);
        Task<IEnumerable<ViewModels.UserArticle>> GetMineByExhibitAsync(Guid Exhibit, CancellationToken ct);
        Task<UnreadArticles> GetMyUnreadCountAsync(Guid exhibitId, CancellationToken ct);
        Task<UnreadArticles> GetUnreadCountAsync(Guid exhibitId, Guid userId, CancellationToken ct);
        Task<ViewModels.UserArticle> CreateAsync(ViewModels.UserArticle userArticle, CancellationToken ct);
        Task<ViewModels.UserArticle> ShareAsync(Guid id, ShareDetails shareDetails, CancellationToken ct);
        Task<ViewModels.UserArticle> SetIsReadAsync(Guid id, bool isRead, CancellationToken ct);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct);
        Task<bool> LoadUserArticlesAsync(ExhibitEntity exhibit, CancellationToken ct);
        Task<bool> LoadUserArticlesAsync(Guid teamArticleId, CancellationToken ct);
        Task<bool> LogXApiAsync(Uri verb, Article article, Guid exhibitId, CancellationToken ct);

    }

    public class UserArticleService : IUserArticleService
    {
        private readonly GalleryDbContext _context;
        private readonly IAuthorizationService _authorizationService;
        private readonly ClaimsPrincipal _user;
        private readonly IMapper _mapper;
        private readonly ClientOptions _clientOptions;
        private readonly ISteamfitterService _steamfitterService;
        private readonly ILogger<UserArticleService> _logger;
        private readonly IXApiService _xApiService;

        public UserArticleService(
            GalleryDbContext context,
            IAuthorizationService authorizationService,
            IPrincipal user,
            IMapper mapper,
            ClientOptions clientOptions,
            ISteamfitterService steamfitterService,
            IXApiService xApiService,
            ILogger<UserArticleService> logger)
        {
            _context = context;
            _authorizationService = authorizationService;
            _user = user as ClaimsPrincipal;
            _mapper = mapper;
            _clientOptions = clientOptions;
            _steamfitterService = steamfitterService;
            _logger = logger;
            _xApiService = xApiService;
        }

        public async Task<IEnumerable<ViewModels.UserArticle>> GetByExhibitAsync(Guid exhibitId, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var exhibit = await _context.Exhibits.FirstAsync(e => e.Id == exhibitId);
            IQueryable<UserArticleEntity> articles = _context.UserArticles
                .Where(ua =>
                        ua.ExhibitId == exhibitId &&
                        (ua.Article.Move < exhibit.CurrentMove) ||
                        (ua.Article.Move == exhibit.CurrentMove && ua.Article.Inject <= exhibit.CurrentInject)
                )
                .OrderByDescending(ua => ua.Article.Move)
                .ThenByDescending(ua => ua.Article.Inject);

            return _mapper.Map<IEnumerable<UserArticle>>(await articles.ToListAsync());
        }

        public async Task<IEnumerable<ViewModels.UserArticle>> GetMineByExhibitAsync(Guid exhibitId, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ExhibitUserRequirement(exhibitId))).Succeeded)
                throw new ForbiddenException();

            var userId = _user.GetId();
            var exhibit = await _context.Exhibits.FirstAsync(e => e.Id == exhibitId);
            await LoadUserArticlesAsync(exhibit, userId, ct);
            var userArticleEntityList =  await _context.UserArticles
                .Where(ua =>
                    ua.UserId == userId &&
                    ua.ExhibitId == exhibitId &&
                    (
                        (ua.Article.Move < exhibit.CurrentMove) ||
                        (ua.Article.Move == exhibit.CurrentMove && ua.Article.Inject <= exhibit.CurrentInject)
                    )
                )
                .Include(ua => ua.Article)
                .OrderByDescending(ua => ua.Article.Move)
                .ThenByDescending(ua => ua.Article.Inject)
                .ToListAsync();

            return _mapper.Map<IEnumerable<UserArticle>>(userArticleEntityList);
        }

        public async Task<UnreadArticles> GetMyUnreadCountAsync(Guid exhibitId, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ExhibitUserRequirement(exhibitId))).Succeeded)
                throw new ForbiddenException();

            return await GetUnreadArticlesAsync(exhibitId, _user.GetId(), ct);
        }

        public async Task<UnreadArticles> GetUnreadCountAsync(Guid exhibitId, Guid userId, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            return await GetUnreadArticlesAsync(exhibitId, userId, ct);
        }

        public async Task<ViewModels.UserArticle> CreateAsync(ViewModels.UserArticle userArticle, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            userArticle.DateCreated = DateTime.UtcNow;
            userArticle.CreatedBy = _user.GetId();
            userArticle.DateModified = null;
            userArticle.ModifiedBy = null;
            var userArticleEntity = _mapper.Map<UserArticleEntity>(userArticle);
            userArticleEntity.Id = userArticleEntity.Id != Guid.Empty ? userArticleEntity.Id : Guid.NewGuid();

            _context.UserArticles.Add(userArticleEntity);
            await _context.SaveChangesAsync(ct);

            return _mapper.Map<UserArticle>(userArticleEntity);
        }

        public async Task<ViewModels.UserArticle> ShareAsync(Guid id, ShareDetails shareDetails, CancellationToken ct)
        {
            var userArticleEntity = await _context.UserArticles
                .SingleOrDefaultAsync(ua => ua.Id == id, ct);
            if (userArticleEntity == null)
                throw new EntityNotFoundException<UserArticle>();

            var fromUserId = _user.GetId();
            if (fromUserId != userArticleEntity.UserId)
                throw new ForbiddenException();

            var sharedUserArticle = _mapper.Map<UserArticle>(userArticleEntity);
            var existingUserIdList = await _context.UserArticles
                .Where(ua => ua.ArticleId == userArticleEntity.ArticleId && ua.ExhibitId == userArticleEntity.ExhibitId)
                .Select(ua => ua.UserId)
                .ToListAsync(ct);
            var exhibitTeamList = _context.Teams
                .Where(t => t.ExhibitId == shareDetails.ExhibitId)
                .Include(t => t.TeamUsers)
                .ThenInclude(tu => tu.User)
                .AsSingleQuery();
            var exhibitTeamIdList = exhibitTeamList
                .Select(t => t.Id);
            var fromTeamId = (await _context.TeamUsers
                .Where(tu => tu.UserId == fromUserId && exhibitTeamIdList.Contains(tu.TeamId))
                .SingleOrDefaultAsync()).TeamId;
            var toTeamUsers = _context.TeamUsers
                .Where(tu => shareDetails.ToTeamIdList.Contains(tu.TeamId))
                .Include(tu => tu.Team)
                .Include(tu => tu.User)
                .AsNoTracking()
                .Distinct();
            if (await toTeamUsers.AnyAsync(ct))
            {
                var addUserIds = toTeamUsers
                    .Where(u => !existingUserIdList.Contains(u.UserId))
                    .Select(u => u.UserId);
                if (await addUserIds.AnyAsync(ct))
                {
                    foreach (var userId in (await addUserIds.ToListAsync(ct)))
                    {
                        sharedUserArticle.Id = Guid.NewGuid();
                        sharedUserArticle.UserId = userId;
                        sharedUserArticle.DateCreated = DateTime.UtcNow;
                        sharedUserArticle.CreatedBy = fromUserId;
                        sharedUserArticle.DateModified = null;
                        sharedUserArticle.ModifiedBy = null;
                        var sharedArticleEntity =  _mapper.Map<UserArticleEntity>(sharedUserArticle);
                        _context.UserArticles.Add(sharedArticleEntity);
                    }
                    await _context.SaveChangesAsync(ct);
                }

                var article = await _context.Articles.Where(a => a.Id == sharedUserArticle.ArticleId).FirstAsync();
                var verb = new Uri("https://w3id.org/xapi/dod-isd/verbs/shared");
                await LogXApiAsync(verb, _mapper.Map<Article>(article), sharedUserArticle.ExhibitId, ct);
                // if email is active, send the article sharing email
                if (_clientOptions.IsEmailActive)
                {
                    // determine the emailFrom address
                    var emailFrom = GetUserEmail(fromUserId, exhibitTeamList.SingleOrDefault(t => t.Id == fromTeamId));
                    if (emailFrom == "")
                    {
                        throw new ArgumentException("You nor your team have a valid email address in Gallery.");
                    }
                    // get the emailCc and emailTo addresses
                    var emailCc = GetTeamEmail(exhibitTeamList.SingleOrDefault(t => t.Id == fromTeamId));
                    var toTeamEmailList = new List<string>();
                    foreach (var teamId in shareDetails.ToTeamIdList)
                    {
                        var teamEmail = GetTeamEmail(exhibitTeamList.SingleOrDefault(t => t.Id == teamId));
                        if (!string.IsNullOrEmpty(teamEmail))
                        {
                            toTeamEmailList.Add(teamEmail);
                        }
                    }
                    var emailTo = string.Join(",", toTeamEmailList);
                    if (emailTo == "")
                    {
                        var toTeamNames = string.Join(",", exhibitTeamList.Where(t => shareDetails.ToTeamIdList.Contains(t.Id)).Select(t => t.ShortName).ToList());
                        throw new ArgumentException($"The selected teams ({toTeamNames}) have no valid email addresses in Gallery.");
                    }
                    var scenarioId = (await _context.Exhibits
                        .FirstOrDefaultAsync(e => e.Id == shareDetails.ExhibitId))
                        .ScenarioId;
                    if (scenarioId == null)
                        throw new ArgumentException("To share an article, the exhibit " + userArticleEntity.ExhibitId + " must have a Steamfitter Scenario ID defined.");
                    // send the message to the user via email from Steamfitter
                    await SendEmail(emailFrom, emailTo, emailCc, shareDetails.Subject, shareDetails.Message, (Guid)scenarioId, ct);
                }
            } else {
                throw new ArgumentException("There are no users on the selected teams to receive a shared article.");
            }

            return _mapper.Map<UserArticle>(userArticleEntity);
        }

        public async Task<ViewModels.UserArticle> SetIsReadAsync(Guid id, bool isRead, CancellationToken ct)
        {
            var userArticleEntity = await _context.UserArticles
                .Include(ua => ua.Article)
                .SingleOrDefaultAsync(ua => ua.UserId == _user.GetId() && ua.Id == id, ct);

            if (userArticleEntity == null)
                throw new EntityNotFoundException<UserArticleEntity>("No user article was found for article and user.");

            userArticleEntity.ModifiedBy = _user.GetId();
            userArticleEntity.DateModified = DateTime.UtcNow;
            userArticleEntity.IsRead = isRead;

            await _context.SaveChangesAsync(ct);

            var article = await _context.Articles.Where(a => a.Id == userArticleEntity.ArticleId).FirstAsync();
            var verb = new Uri("https://w3id.org/xapi/dod-isd/verbs/read");
            if (!isRead) {
                verb = new Uri("https://w3id.org/xapi/dod-isd/verbs/reset");
            }
            await LogXApiAsync(verb, _mapper.Map<Article>(article), userArticleEntity.ExhibitId, ct);

            return _mapper.Map<UserArticle>(userArticleEntity);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var userArticleToDelete = await _context.UserArticles.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (userArticleToDelete == null)
                throw new EntityNotFoundException<Article>();

            _context.UserArticles.Remove(userArticleToDelete);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        public async Task<bool> LoadUserArticlesAsync(ExhibitEntity exhibit, CancellationToken ct)
        {
            var currentMove = exhibit.CurrentMove;
            var currentInject = exhibit.CurrentInject;
            var actualTime = DateTime.UtcNow;
            var teamArticleList = await _context.TeamArticles
                .Include(ta => ta.Team)
                .ThenInclude(t => t.TeamUsers)
                .Where(ta => ta.ExhibitId == exhibit.Id &&
                    (ta.Article.Move < currentMove || (ta.Article.Move == currentMove && ta.Article.Inject <= currentInject))
                )
                .AsSplitQuery()
                .ToListAsync(ct);
            foreach (var teamArticle in teamArticleList)
            {
                foreach (var teamUser in teamArticle.Team.TeamUsers)
                {
                    try
                    {
                        var newUserArticle = new UserArticleEntity() {
                            Id = Guid.NewGuid(),
                            ArticleId = teamArticle.ArticleId,
                            ExhibitId = exhibit.Id,
                            UserId = teamUser.UserId,
                            ActualDatePosted = actualTime,
                            IsRead = false
                        };
                        await _context.UserArticles.AddAsync(newUserArticle, ct);
                        await _context.SaveChangesAsync(ct);
                    }
                    catch (Exception ex)
                    {
                        if (ex.InnerException == null
                            || !ex.InnerException.Message.Contains("IX_user_articles_exhibit_id_user_id_article_id"))
                        {
                            throw ex;
                        }
                    }
                }

            }

            return true;
        }

        public async Task<bool> LoadUserArticlesAsync(Guid teamArticleId, CancellationToken ct)
        {
            var teamArticle = await _context.TeamArticles
                .Include(ta => ta.Team)
                .ThenInclude(t => t.TeamUsers)
                .SingleOrDefaultAsync(ta => ta.Id == teamArticleId);
            var exhibit = await _context.Exhibits
                .SingleOrDefaultAsync(e => e.Id == teamArticle.ExhibitId, ct);
            var currentMove = exhibit.CurrentMove;
            var currentInject = exhibit.CurrentInject;
            var actualTime = DateTime.UtcNow;
            foreach (var teamUser in teamArticle.Team.TeamUsers)
            {
                try
                {
                    var newUserArticle = new UserArticleEntity() {
                        Id = Guid.NewGuid(),
                        ArticleId = teamArticle.ArticleId,
                        ExhibitId = exhibit.Id,
                        UserId = teamUser.UserId,
                        ActualDatePosted = actualTime,
                        IsRead = false
                    };
                    await _context.UserArticles.AddAsync(newUserArticle, ct);
                    await _context.SaveChangesAsync(ct);
                }
                catch (Exception ex)
                {
                    if (ex.InnerException == null
                        || !ex.InnerException.Message.Contains("IX_user_articles_exhibit_id_user_id_article_id"))
                    {
                        throw ex;
                    }
                }
            }

            return true;
        }

        private async Task<bool> LoadUserArticlesAsync(ExhibitEntity exhibit, Guid userId, CancellationToken ct)
        {
            var currentMove = exhibit.CurrentMove;
            var currentInject = exhibit.CurrentInject;
            var actualTime = DateTime.UtcNow;
            var teamId = await _context.TeamUsers
                .Where(tu => tu.UserId == userId && tu.Team.ExhibitId == exhibit.Id)
                .Select(tu => tu.TeamId)
                .SingleOrDefaultAsync(ct);
            var teamArticleList = await _context.TeamArticles
                .Include(ta => ta.Team)
                .ThenInclude(t => t.TeamUsers)
                .Where(ta => ta.ExhibitId == exhibit.Id &&
                    (ta.Article.Move < currentMove || (ta.Article.Move == currentMove && ta.Article.Inject <= currentInject)) &&
                    ta.TeamId == teamId
                )
                .AsSplitQuery()
                .ToListAsync(ct);
            var newUserArticles = new List<UserArticleEntity>();
            foreach (var teamArticle in teamArticleList)
            {
                var alreadyExists = await _context.UserArticles.AnyAsync(ua =>
                    ua.ExhibitId == exhibit.Id &&
                    ua.ArticleId == teamArticle.ArticleId &&
                    ua.UserId == userId);
                var alreadyAdded = newUserArticles.Any(ua =>
                    ua.ExhibitId == exhibit.Id &&
                    ua.ArticleId == teamArticle.ArticleId &&
                    ua.UserId == userId);
                if (!alreadyExists && !alreadyAdded)
                {
                    var newUserArticle = new UserArticleEntity() {
                        Id = Guid.NewGuid(),
                        ArticleId = teamArticle.ArticleId,
                        ExhibitId = exhibit.Id,
                        UserId = userId,
                        ActualDatePosted = actualTime,
                        IsRead = false
                    };
                    newUserArticles.Add(newUserArticle);
                }
            }
            await _context.UserArticles.AddRangeAsync(newUserArticles, ct);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        private string GetUserEmail(Guid userId, TeamEntity team)
        {
            var userEmail = "";
            var user = team.TeamUsers.SingleOrDefault(tu => tu.UserId == userId).User;
            if (user.Email != null && user.Email.Contains("@"))
            {
                userEmail = user.Email;
            }
            else if (team.Email != null && team.Email.Contains("@"))
            {
                userEmail = team.Email;
            }
            return userEmail;
        }

        private string GetTeamEmail(TeamEntity team)
        {
            var teamEmail = "";
            if (team.Email != null && team.Email.Contains("@"))
            {
                teamEmail = team.Email;
            }
            else 
            {
                teamEmail = string.Join(",", (team.TeamUsers
                    .Where(tu => tu.User.Email.Contains("@"))
                    .Select(tu => tu.User.Email)
                    .ToList()));
            }
            return teamEmail;
        }

        private async Task<string> SendEmail(
            string emailFrom,
            string emailTo,
            string emailCc,
            string subject,
            string message,
            Guid scenarioId,
            CancellationToken ct)
        {
            var task = new SAC.TaskForm()
            {
                Repeatable = false,
                UserExecutable = true,
                Score = 0,
                TriggerCondition = SAC.TaskTrigger.Manual,
                TriggerTaskId = null,
                CurrentIteration = 0,
                IterationTermination = SAC.TaskIterationTermination.IterationCount,
                Iterations = 1,
                IntervalSeconds = 0,
                DelaySeconds = 0,
                Executable = true,
                ExpirationSeconds = 0,
                ActionParameters = new Dictionary<string, string>()
                {
                    {"EmailFrom", emailFrom},
                    {"EmailTo", emailTo},
                    {"EmailCC", emailCc},
                    {"Subject", subject},
                    {"Message", message},
                    {"Account", "stackstorm"},
                    {"Mime", "html"}
                },
                ApiUrl = "stackstorm",
                VmList = null,
                VmMask = "",
                Action = SAC.TaskAction.Send_email,
                UserId = _user.GetId(),
                ScenarioId = scenarioId,
                ScenarioTemplateId = null,
                Description = "Send email regarding shared Gallery Article",
                Name = "Send Email from Gallery User " + _user.FindFirst("name")?.Value,
                ExpectedOutput = "True",
                AdditionalProperties = new Dictionary<string, Object>(){}
            };
            try
            {
                // create and execute the send email task in steamfitter
                var results = await _steamfitterService.CreateAndExecuteTaskAsnyc(task, ct);
            }
            catch (System.Exception ex)
            {
                if (!ex.Message.Contains("Status: 200"))
                {
                    throw new Exception("Error sending email via Steamfitter API, which returned the following error:  " + ex.Message);
                }
            }

            return "ok";
        }

        private async Task<UnreadArticles> GetUnreadArticlesAsync(Guid exhibitId, Guid userId, CancellationToken ct)
        {
            var exhibit = await _context.Exhibits.FirstAsync(e => e.Id == exhibitId);
            if (exhibit == null)
                throw new EntityNotFoundException<ExhibitEntity>("Exhibit " + exhibitId + " was not found.");

            await LoadUserArticlesAsync(exhibit, userId, ct);
            var count =  await _context.UserArticles
                .Where(ua =>
                    ua.UserId == userId &&
                    ua.ExhibitId == exhibitId &&
                    (
                        (ua.Article.Move < exhibit.CurrentMove) ||
                        (ua.Article.Move == exhibit.CurrentMove && ua.Article.Inject <= exhibit.CurrentInject)
                    ) &&
                    !ua.IsRead
                )
                .CountAsync();

            return new UnreadArticles(){
                ExhibitId = exhibitId,
                UserId = userId,
                Count = count.ToString()
            };

        }

        public async Task<bool> LogXApiAsync(Uri verb, Article article, Guid exhibitId, CancellationToken ct)
        {
            if (_xApiService.IsConfigured())
            {

                var collection = await _context.Collections.Where(c => c.Id == article.CollectionId).FirstAsync();
                var card = await _context.Cards.Where(c => c.Id == article.CardId).FirstAsync();

                var teamId = (await _context.TeamUsers
                    .SingleOrDefaultAsync(tu => tu.UserId == _user.GetId() && tu.Team.ExhibitId == exhibitId)).TeamId;

                // create and send xapi statement
                var activity = new Dictionary<String,String>();
                activity.Add("id", article.Id.ToString());
                activity.Add("name", article.Name);
                activity.Add("description", article.Summary);
                activity.Add("type", "article");
                activity.Add("activityType", "http://id.tincanapi.com/activitytype/resource");
                activity.Add("moreInfo", "/article/" + article.Id.ToString());

                var parent = new Dictionary<String,String>();
                parent.Add("id", exhibitId.ToString());
                parent.Add("name", "Exhibit");
                parent.Add("description", collection.Name);
                parent.Add("type", "exhibit");
                parent.Add("activityType", "http://adlnet.gov/expapi/activities/simulation");
                parent.Add("moreInfo", "/?exhibit=" + exhibitId.ToString());

                var category = new Dictionary<String,String>();
                category.Add("id", article.SourceType.ToString());
                category.Add("name", article.SourceType.ToString());
                category.Add("description", "The source type for the article.");
                category.Add("type", "sourceType");
                category.Add("activityType", "http://id.tincanapi.com/activitytype/category");
                //category.Add("moreInfo", "");

                var grouping = new Dictionary<String,String>();
                grouping.Add("id", card.Id.ToString());
                grouping.Add("name", card.Name);
                grouping.Add("description", card.Description);
                grouping.Add("type", "card");
                grouping.Add("activityType", "http://id.tincanapi.com/activitytype/collection-simple");
                grouping.Add("moreInfo", "/?section=archive&exhibit=" + exhibitId.ToString() + "&card=" + card.Id.ToString());

                var other = new Dictionary<String,String>();

                // TODO determine if we should log exhibit as registration
                return await _xApiService.CreateAsync(
                    verb, activity, parent, category, grouping, other, teamId, ct);

            }
            return false;
        }

    }

    public class ShareDetails
    {
        public Guid[] ToTeamIdList { get; set; }
        public Guid ExhibitId { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
    }

    public class UnreadArticles
    {
        public Guid ExhibitId { get; set; }
        public Guid UserId { get; set; }
        public string Count { get; set; }
    }

}

