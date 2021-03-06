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

        public UserArticleService(
            GalleryDbContext context,
            IAuthorizationService authorizationService,
            IPrincipal user,
            IMapper mapper,
            ClientOptions clientOptions,
            ISteamfitterService steamfitterService,
            ILogger<UserArticleService> logger)
        {
            _context = context;
            _authorizationService = authorizationService;
            _user = user as ClaimsPrincipal;
            _mapper = mapper;
            _clientOptions = clientOptions;
            _steamfitterService = steamfitterService;
            _logger = logger;
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
            return await GetUnreadCountAsync(exhibitId, _user.GetId(), ct);
        }

        public async Task<UnreadArticles> GetUnreadCountAsync(Guid exhibitId, Guid userId, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded &&
                !(((await _authorizationService.AuthorizeAsync(_user, null, new BaseUserRequirement())).Succeeded) && userId == _user.GetId()))
                throw new ForbiddenException();

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
            if (_user.GetId() != userArticleEntity.UserId)
                throw new ForbiddenException();
            var sharedUserArticle = _mapper.Map<UserArticle>(userArticleEntity);
            var existingUserIdList = await _context.UserArticles
                .Where(ua => ua.ArticleId == userArticleEntity.ArticleId && ua.ExhibitId == userArticleEntity.ExhibitId)
                .Select(ua => ua.UserId)
                .ToListAsync(ct);
            var exhibitTeamIdList = await _context.ExhibitTeams
                .Where(et => et.ExhibitId == shareDetails.ExhibitId)
                .Select(et => et.TeamId)
                .ToListAsync(ct);
            var ccTeamId = (await _context.TeamUsers
                .FirstOrDefaultAsync(tu => tu.UserId == _user.GetId() && exhibitTeamIdList.Contains(tu.TeamId))).TeamId;
            var teamUsers = _context.TeamUsers
                .Where(tu => shareDetails.ToTeamIdList.Contains(tu.TeamId) && exhibitTeamIdList.Contains(tu.TeamId));
            if (await teamUsers.AnyAsync(ct))
            {
                var addUserIds = teamUsers
                    .Where(u => !existingUserIdList.Contains(u.UserId))
                    .Select(u => u.UserId);
                if (await addUserIds.AnyAsync(ct))
                {
                    foreach (var userId in (await addUserIds.ToListAsync(ct)))
                    {
                        sharedUserArticle.Id = Guid.NewGuid();
                        sharedUserArticle.UserId = userId;
                        sharedUserArticle.DateCreated = DateTime.UtcNow;
                        sharedUserArticle.CreatedBy = _user.GetId();
                        sharedUserArticle.DateModified = null;
                        sharedUserArticle.ModifiedBy = null;
                        var sharedArticleEntity =  _mapper.Map<UserArticleEntity>(sharedUserArticle);
                        _context.UserArticles.Add(sharedArticleEntity);
                    }
                    await _context.SaveChangesAsync(ct);
                }
                if (_clientOptions.IsEmailActive)
                {
                    // get the email addresses
                    var emailFrom = (await _context.Users
                        .FirstOrDefaultAsync(u => u.Id == _user.GetId())).Email;
                    var emailTo = string.Join(",", (await teamUsers
                        .Where(tu => tu.User.Email.Contains("@"))
                        .Select(tu => tu.User.Email)
                        .ToListAsync(ct)));
                    if (emailFrom == "")
                    {
                        throw new ArgumentException("You do not have an email address in Gallery.");
                    }
                    else if (emailTo == "")
                    {
                        throw new ArgumentException("The selected teams have no users with email addresses in Gallery.");
                    }
                    var ccUsers = await _context.TeamUsers
                        .Where(tu => tu.TeamId == ccTeamId && tu.User.Email.Contains("@"))
                        .Select(tu => tu.User)
                        .ToListAsync(ct);
                    var emailCc = string.Join(",", ccUsers
                        .Select(u => u.Email)
                        .ToList());
                    var scenarioId = (await _context.Exhibits
                        .FirstOrDefaultAsync(e => e.Id == shareDetails.ExhibitId))
                        .ScenarioId;
                    if (scenarioId == null)
                        throw new ArgumentException("To share an article, the exhibit " + userArticleEntity.ExhibitId + " must have a Steamfitter Scenario ID defined.");
                    // send the message to the user via email from Steamfitter
                    await SendEmail(emailFrom, emailTo, emailCc, shareDetails.Subject, shareDetails.Message, (Guid)scenarioId, ct);
                }
            } else {
                throw new ArgumentException("There are no users on the selected teams to receive an email notification.");
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
            var teamList = await _context.ExhibitTeams
                .Include(et => et.Team)
                .ThenInclude(t => t.TeamUsers)
                .Where(et => et.ExhibitId == exhibit.Id)
                .Select(et => et.Team)
                .ToListAsync(ct);
            foreach (var team in teamList)
            {
                var cardIdList = await _context.TeamCards
                    .Where(tc => tc.TeamId == team.Id && tc.Card.CollectionId == exhibit.CollectionId)
                    .Select(tc => tc.CardId)
                    .ToListAsync(ct);
                var validArticleIdList = await _context.Articles
                    .Where(a => cardIdList.Contains((Guid)a.CardId) && (a.Move < currentMove || (a.Move == currentMove && a.Inject <= currentInject)))
                    .Select(a => a.Id)
                    .ToListAsync(ct);
                foreach (var articleId in validArticleIdList)
                {
                    foreach (var teamUser in team.TeamUsers)
                    {
                        var alreadyExists = _context.UserArticles.Any(ua =>
                                ua.ExhibitId == exhibit.Id &&
                                ua.ArticleId == articleId &&
                                ua.UserId == teamUser.UserId
                            );
                        if (!alreadyExists)
                        {
                            var newUserArticle = new UserArticleEntity() {
                                Id = Guid.NewGuid(),
                                ArticleId = articleId,
                                ExhibitId = exhibit.Id,
                                UserId = teamUser.UserId,
                                ActualDatePosted = actualTime,
                                IsRead = false
                            };
                            await _context.UserArticles.AddAsync(newUserArticle, ct);
                        }
                    }
                }

            }
            await _context.SaveChangesAsync(ct);

            return true;
        }

        private async Task<bool> LoadUserArticlesAsync(ExhibitEntity exhibit, Guid userId, CancellationToken ct)
        {
            var currentMove = exhibit.CurrentMove;
            var currentInject = exhibit.CurrentInject;
            var actualTime = DateTime.UtcNow;
            var exhibitTeamIdList = await _context.ExhibitTeams
                .Where(et => et.ExhibitId == exhibit.Id)
                .Select(et => et.TeamId)
                .ToListAsync(ct);
            var userTeamIdList = await _context.TeamUsers
                .Where(tu => tu.UserId == userId)
                .Select(tu => tu.TeamId)
                .ToListAsync(ct);
            var teamId = (await _context.Teams
                .FirstOrDefaultAsync(t => exhibitTeamIdList.Contains(t.Id) && userTeamIdList.Contains(t.Id)))
                .Id;
            var cardIdList = await _context.TeamCards
                .Where(tc => tc.TeamId == teamId && tc.Card.CollectionId == exhibit.CollectionId)
                .Select(tc => tc.CardId)
                .ToListAsync(ct);
            var validArticleIdList = await _context.Articles
                .Where(a => cardIdList.Contains((Guid)a.CardId) && (a.Move < currentMove || (a.Move == currentMove && a.Inject <= currentInject)))
                .Select(a => a.Id)
                .ToListAsync(ct);
            var existingArticleIdList = await _context.UserArticles
                .Where(ua => ua.UserId == userId && validArticleIdList.Contains(ua.ArticleId))
                .Select(ua => ua.ArticleId)
                .ToListAsync(ct);
            var neededArticleIdList = validArticleIdList.Where(id => !existingArticleIdList.Contains(id));
            foreach (var articleId in neededArticleIdList)
            {
                var newUserArticle = new UserArticleEntity() {
                    Id = Guid.NewGuid(),
                    ArticleId = articleId,
                    ExhibitId = exhibit.Id,
                    UserId = userId,
                    ActualDatePosted = actualTime,
                    IsRead = false
                };
                await _context.UserArticles.AddAsync(newUserArticle, ct);
            }
            await _context.SaveChangesAsync(ct);

            return true;
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

