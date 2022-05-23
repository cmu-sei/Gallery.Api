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
using Api.Data;
using Api.Data.Models;
using Api.Infrastructure.Authorization;
using Api.Infrastructure.Exceptions;
using Api.Infrastructure.Extensions;
using Api.ViewModels;

namespace Api.Services
{
    public interface IArticleService
    {
        Task<IEnumerable<ViewModels.Article>> GetAsync(CancellationToken ct);
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
        private readonly ApiDbContext _context;
        private readonly IAuthorizationService _authorizationService;
        private readonly ClaimsPrincipal _user;
        private readonly IMapper _mapper;

        public ArticleService(
            ApiDbContext context,
            IAuthorizationService authorizationService,
            IPrincipal user,
            IMapper mapper)
        {
            _context = context;
            _authorizationService = authorizationService;
            _user = user as ClaimsPrincipal;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ViewModels.Article>> GetAsync(CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new FullRightsRequirement())).Succeeded)
                throw new ForbiddenException();

            IQueryable<ArticleEntity> articles = _context.Articles;

            return _mapper.Map<IEnumerable<Article>>(await articles.ToListAsync());
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
                .Where(a => a.CardId == cardId)
                .OrderBy(a => a.Move)
                .ThenBy(a => a.Inject);

            return _mapper.Map<IEnumerable<Article>>(await articles.ToListAsync());
        }

        public async Task<IEnumerable<ViewModels.Article>> GetByCollectionAsync(Guid collectionId, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            IQueryable<ArticleEntity> articles = _context.Articles
                .Where(a => a.CollectionId == collectionId)
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
                    && (a.Move < exhibit.CurrentMove
                        || (a.Move == exhibit.CurrentMove && a.Inject <= exhibit.CurrentInject))
                )
                .OrderByDescending(a => a.Move)
                .ThenByDescending(a => a.Inject);

            return _mapper.Map<IEnumerable<Article>>(await articles.ToListAsync());
        }

        public async Task<ViewModels.Article> CreateAsync(ViewModels.Article article, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            article.DateCreated = DateTime.UtcNow;
            article.CreatedBy = _user.GetId();
            article.DateModified = null;
            article.ModifiedBy = null;
            var articleEntity = _mapper.Map<ArticleEntity>(article);
            articleEntity.Id = articleEntity.Id != Guid.Empty ? articleEntity.Id : Guid.NewGuid();

            _context.Articles.Add(articleEntity);
            await _context.SaveChangesAsync(ct);
            article = await GetAsync(articleEntity.Id, ct);

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

