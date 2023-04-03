// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

/*
    Articles have an optional CardID and an optional ExhibitId
    The optional CardId associates an article to a Card, so that the article is sorted by that Card and contributes to that Card's status
    The optional ExhibitId inicates a user created an Article during an active Exhibit.   That Article is only used by that Exhibit.
*/
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
using Gallery.Api.Data;
using Gallery.Api.Data.Models;
using Gallery.Api.Infrastructure.Authorization;
using Gallery.Api.Infrastructure.Exceptions;
using Gallery.Api.Infrastructure.Extensions;
using Gallery.Api.ViewModels;

namespace Gallery.Api.Services
{
    public interface IArticleService
    {
        Task<ViewModels.Article> GetAsync(Guid id, CancellationToken ct);
        Task<IEnumerable<ViewModels.Article>> GetByCardAsync(Guid cardId, CancellationToken ct);
        Task<IEnumerable<ViewModels.Article>> GetByCollectionAsync(Guid collectionId, CancellationToken ct);
        Task<IEnumerable<ViewModels.Article>> GetByExhibitAsync(Guid exhibitId, CancellationToken ct);
        Task<ViewModels.Article> CreateAsync(ViewModels.Article article, CancellationToken ct);
        Task<ViewModels.Article> UpdateAsync(Guid id, ViewModels.Article article, CancellationToken ct);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    }

    public class ArticleService : IArticleService
    {
        private readonly GalleryDbContext _context;
        private readonly IAuthorizationService _authorizationService;
        private readonly ClaimsPrincipal _user;
        private readonly IMapper _mapper;
        private readonly IUserArticleService _userArticleService;

        public ArticleService(
            GalleryDbContext context,
            IAuthorizationService authorizationService,
            IPrincipal user,
            IMapper mapper,
            IUserArticleService userArticleService)
        {
            _context = context;
            _authorizationService = authorizationService;
            _user = user as ClaimsPrincipal;
            _mapper = mapper;
            _userArticleService = userArticleService;
        }

        public async Task<ViewModels.Article> GetAsync(Guid id, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new BaseUserRequirement())).Succeeded)
                throw new ForbiddenException();

            var item = await _context.Articles.SingleOrDefaultAsync(sm => sm.Id == id, ct);

            return _mapper.Map<Article>(item);
        }

        public async Task<IEnumerable<ViewModels.Article>> GetByCardAsync(Guid cardId, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            IQueryable<ArticleEntity> articles = _context.Articles
                .Where(a => a.CardId == cardId && a.ExhibitId == null)
                .OrderBy(a => a.Move)
                .ThenBy(a => a.Inject);

            return _mapper.Map<IEnumerable<Article>>(await articles.ToListAsync());
        }

        public async Task<IEnumerable<ViewModels.Article>> GetByCollectionAsync(Guid collectionId, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            IQueryable<ArticleEntity> articles = _context.Articles
                .Where(a => a.CollectionId == collectionId && a.ExhibitId == null)
                .OrderBy(a => a.Move)
                .ThenBy(a => a.Inject);

            return _mapper.Map<IEnumerable<Article>>(await articles.ToListAsync());
        }

        public async Task<IEnumerable<ViewModels.Article>> GetByExhibitAsync(Guid exhibitId, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var exhibit = (await _context.Exhibits.FirstAsync(e => e.Id == exhibitId));
            IQueryable<ArticleEntity> articles = _context.Articles
                .Where(a => a.CollectionId == exhibit.CollectionId
                     && (a.ExhibitId == null || a.ExhibitId == exhibitId)
                    && (a.Move < exhibit.CurrentMove
                        || (a.Move == exhibit.CurrentMove && a.Inject <= exhibit.CurrentInject))
                )
                .OrderByDescending(a => a.Move)
                .ThenByDescending(a => a.Inject);

            return _mapper.Map<IEnumerable<Article>>(await articles.ToListAsync());
        }

        public async Task<ViewModels.Article> CreateAsync(ViewModels.Article article, CancellationToken ct)
        {
            if (article.ExhibitId == null)
            {
                // must be a content developer to add an article to a collection
                if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                    throw new ForbiddenException();
            }
            else
            {
                var userId = _user.GetId();
                var teamId = (await _context.TeamUsers
                    .SingleOrDefaultAsync(tu => tu.UserId == userId && tu.Team.ExhibitId == article.ExhibitId)).TeamId;
                var canPostArticles = await _context.TeamCards
                    .AnyAsync(tc => tc.TeamId == teamId && tc.CardId == article.CardId && tc.CanPostArticles, ct);
                if (!canPostArticles)
                    throw new ForbiddenException();
            }

            article.DateCreated = DateTime.UtcNow;
            article.CreatedBy = _user.GetId();
            article.DateModified = null;
            article.ModifiedBy = null;
            var articleEntity = _mapper.Map<ArticleEntity>(article);
            articleEntity.Id = articleEntity.Id != Guid.Empty ? articleEntity.Id : Guid.NewGuid();
            articleEntity.DatePosted = articleEntity.DatePosted.Kind == DateTimeKind.Utc ? articleEntity.DatePosted : DateTime.SpecifyKind(articleEntity.DatePosted, DateTimeKind.Utc);

            _context.Articles.Add(articleEntity);
            await _context.SaveChangesAsync(ct);
            article = await GetAsync(articleEntity.Id, ct);

            // add ArticleTeams and UserArticles, if this is a user article for an Exhibit
            if (article.ExhibitId != null)
            {
                // ArticleTeams
                var teamCards = await _context.TeamCards
                    .Where(tc => tc.CardId == article.CardId && tc.Team.ExhibitId == article.ExhibitId)
                    .ToListAsync(ct);
                foreach (var teamCard in teamCards)
                {
                    var teamArticle = new TeamArticleEntity() {
                        TeamId = teamCard.TeamId,
                        ArticleId = article.Id,
                        ExhibitId = (Guid)article.ExhibitId
                    };
                    _context.TeamArticles.Add(teamArticle);
                    await _context.SaveChangesAsync(ct);
                    // UserArticles
                    await _userArticleService.LoadUserArticlesAsync(teamArticle.Id, ct);
                }
            }

            return article;
        }

        public async Task<ViewModels.Article> UpdateAsync(Guid id, ViewModels.Article article, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new CanIncrementIncidentRequirement())).Succeeded)
                throw new ForbiddenException();

            var articleToUpdate = await _context.Articles.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (articleToUpdate == null)
                throw new EntityNotFoundException<Article>();

            article.CreatedBy = articleToUpdate.CreatedBy;
            article.DateCreated = articleToUpdate.DateCreated;
            article.ModifiedBy = _user.GetId();
            article.DateModified = DateTime.UtcNow;
            _mapper.Map(article, articleToUpdate);

            _context.Articles.Update(articleToUpdate);
            await _context.SaveChangesAsync(ct);

            article = await GetAsync(articleToUpdate.Id, ct);

            return article;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var articleToDelete = await _context.Articles.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (articleToDelete == null)
                throw new EntityNotFoundException<Article>();

            _context.Articles.Remove(articleToDelete);
            await _context.SaveChangesAsync(ct);

            return true;
        }

    }
}

