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
using Gallery.Api.Data;
using Gallery.Api.Data.Models;
using Gallery.Api.Infrastructure.Authorization;
using Gallery.Api.Infrastructure.Exceptions;
using Gallery.Api.Infrastructure.Extensions;
using Gallery.Api.ViewModels;

namespace Gallery.Api.Services
{
    public interface IArticleCardService
    {
        Task<IEnumerable<ViewModels.ArticleCard>> GetAsync(CancellationToken ct);
        Task<IEnumerable<ViewModels.ArticleCard>> GetByCollectionAsync(Guid collectionId, CancellationToken ct);
        Task<IEnumerable<ViewModels.ArticleCard>> GetByCardAsync(Guid cardId, CancellationToken ct);
        Task<IEnumerable<ViewModels.ArticleCard>> GetByArticleAsync(Guid articleId, CancellationToken ct);
        Task<ViewModels.ArticleCard> GetAsync(Guid id, CancellationToken ct);
        Task<ViewModels.ArticleCard> CreateAsync(ViewModels.ArticleCard articleCard, CancellationToken ct);
        Task<ViewModels.ArticleCard> UpdateAsync(Guid id, ViewModels.ArticleCard articleCard, CancellationToken ct);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct);
        Task<bool> DeleteByIdsAsync(Guid articleId, Guid userId, CancellationToken ct);
    }

    public class ArticleCardService : IArticleCardService
    {
        private readonly GalleryDbContext _context;
        private readonly IAuthorizationService _authorizationService;
        private readonly ClaimsPrincipal _user;
        private readonly IMapper _mapper;

        public ArticleCardService(GalleryDbContext context, IAuthorizationService authorizationService, IPrincipal user, IMapper mapper)
        {
            _context = context;
            _authorizationService = authorizationService;
            _user = user as ClaimsPrincipal;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ViewModels.ArticleCard>> GetAsync(CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var items = await _context.ArticleCards
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<ArticleCard>>(items);
        }

        public async Task<IEnumerable<ViewModels.ArticleCard>> GetByCollectionAsync(Guid collectionId, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new BaseUserRequirement())).Succeeded)
                throw new ForbiddenException();

            var items = await _context.ArticleCards
                .Where(tc => tc.Card.CollectionId == collectionId)
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<ArticleCard>>(items);
        }

        public async Task<IEnumerable<ViewModels.ArticleCard>> GetByCardAsync(Guid cardId, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var items = await _context.ArticleCards
                .Where(tc => tc.CardId == cardId)
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<ArticleCard>>(items);
        }

        public async Task<IEnumerable<ViewModels.ArticleCard>> GetByArticleAsync(Guid articleId, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var items = await _context.ArticleCards
                .Where(tc => tc.ArticleId == articleId)
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<ArticleCard>>(items);
        }

        public async Task<ViewModels.ArticleCard> GetAsync(Guid id, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var item = await _context.ArticleCards
                .SingleOrDefaultAsync(o => o.Id == id, ct);

            return _mapper.Map<ArticleCard>(item);
        }

        public async Task<ViewModels.ArticleCard> CreateAsync(ViewModels.ArticleCard articleCard, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            articleCard.DateCreated = DateTime.UtcNow;
            articleCard.CreatedBy = _user.GetId();
            articleCard.DateModified = null;
            articleCard.ModifiedBy = null;
            var articleCardEntity = _mapper.Map<ArticleCardEntity>(articleCard);
            articleCardEntity.Id = articleCardEntity.Id != Guid.Empty ? articleCardEntity.Id : Guid.NewGuid();

            _context.ArticleCards.Add(articleCardEntity);
            await _context.SaveChangesAsync(ct);

            return await GetAsync(articleCardEntity.Id, ct);
        }

        public async Task<ViewModels.ArticleCard> UpdateAsync(Guid id, ViewModels.ArticleCard articleCard, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new CanIncrementIncidentRequirement())).Succeeded)
                throw new ForbiddenException();

            var articleCardToUpdate = await _context.ArticleCards.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (articleCardToUpdate == null)
                throw new EntityNotFoundException<ArticleCard>();

            articleCard.CreatedBy = articleCardToUpdate.CreatedBy;
            articleCard.DateCreated = articleCardToUpdate.DateCreated;
            articleCard.ModifiedBy = _user.GetId();
            articleCard.DateModified = DateTime.UtcNow;
            _mapper.Map(articleCard, articleCardToUpdate);

            _context.ArticleCards.Update(articleCardToUpdate);
            await _context.SaveChangesAsync(ct);

            articleCard = await GetAsync(articleCardToUpdate.Id, ct);

            return articleCard;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var articleCardToDelete = await _context.ArticleCards.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (articleCardToDelete == null)
                throw new EntityNotFoundException<ArticleCard>();

            _context.ArticleCards.Remove(articleCardToDelete);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        public async Task<bool> DeleteByIdsAsync(Guid articleId, Guid cardId, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var articleCardToDelete = await _context.ArticleCards.SingleOrDefaultAsync(v => (v.CardId == cardId) && (v.ArticleId == articleId), ct);

            if (articleCardToDelete == null)
                throw new EntityNotFoundException<ArticleCard>();

            _context.ArticleCards.Remove(articleCardToDelete);
            await _context.SaveChangesAsync(ct);

            return true;
        }

    }
}

