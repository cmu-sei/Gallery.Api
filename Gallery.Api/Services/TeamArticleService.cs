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
    public interface ITeamArticleService
    {
        Task<IEnumerable<ViewModels.TeamArticle>> GetAsync(CancellationToken ct);
        Task<IEnumerable<ViewModels.TeamArticle>> GetByExhibitAsync(Guid exhibitId, CancellationToken ct);
        Task<IEnumerable<ViewModels.TeamArticle>> GetByArticleAsync(Guid articleId, CancellationToken ct);
        Task<IEnumerable<ViewModels.TeamArticle>> GetByTeamAsync(Guid teamId, CancellationToken ct);
        Task<ViewModels.TeamArticle> GetAsync(Guid id, CancellationToken ct);
        Task<ViewModels.TeamArticle> CreateAsync(ViewModels.TeamArticle teamArticle, CancellationToken ct);
        Task<ViewModels.TeamArticle> UpdateAsync(Guid id, ViewModels.TeamArticle teamArticle, CancellationToken ct);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct);
        Task<bool> DeleteByIdsAsync(Guid teamId, Guid userId, CancellationToken ct);
    }

    public class TeamArticleService : ITeamArticleService
    {
        private readonly GalleryDbContext _context;
        private readonly IAuthorizationService _authorizationService;
        private readonly ClaimsPrincipal _user;
        private readonly IMapper _mapper;

        public TeamArticleService(GalleryDbContext context, IAuthorizationService authorizationService, IPrincipal user, IMapper mapper)
        {
            _context = context;
            _authorizationService = authorizationService;
            _user = user as ClaimsPrincipal;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ViewModels.TeamArticle>> GetAsync(CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var items = await _context.TeamArticles
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<TeamArticle>>(items);
        }

        public async Task<IEnumerable<ViewModels.TeamArticle>> GetByExhibitAsync(Guid exhibitId, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new BaseUserRequirement())).Succeeded)
                throw new ForbiddenException();
            var collectionId = (await _context.Exhibits.FirstOrDefaultAsync(e => e.Id == exhibitId, ct)).CollectionId;
            var teamIds = await _context.ExhibitTeams.Where(et => et.ExhibitId == exhibitId).Select(et => et.TeamId).ToListAsync(ct);
            var items = await _context.TeamArticles
                .Where(ta => ta.Article.CollectionId == collectionId && teamIds.Contains(ta.TeamId))
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<TeamArticle>>(items);
        }

        public async Task<IEnumerable<ViewModels.TeamArticle>> GetByArticleAsync(Guid articleId, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var items = await _context.TeamArticles
                .Where(tc => tc.ArticleId == articleId)
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<TeamArticle>>(items);
        }

        public async Task<IEnumerable<ViewModels.TeamArticle>> GetByTeamAsync(Guid teamId, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new TeamUserRequirement(teamId))).Succeeded)
                throw new ForbiddenException();

            var items = await _context.TeamArticles
                .Where(tc => tc.TeamId == teamId)
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<TeamArticle>>(items);
        }

        public async Task<ViewModels.TeamArticle> GetAsync(Guid id, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var item = await _context.TeamArticles
                .SingleOrDefaultAsync(o => o.Id == id, ct);

            return _mapper.Map<TeamArticle>(item);
        }

        public async Task<ViewModels.TeamArticle> CreateAsync(ViewModels.TeamArticle teamArticle, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            teamArticle.DateCreated = DateTime.UtcNow;
            teamArticle.CreatedBy = _user.GetId();
            teamArticle.DateModified = null;
            teamArticle.ModifiedBy = null;
            var teamArticleEntity = _mapper.Map<TeamArticleEntity>(teamArticle);
            teamArticleEntity.Id = teamArticleEntity.Id != Guid.Empty ? teamArticleEntity.Id : Guid.NewGuid();

            _context.TeamArticles.Add(teamArticleEntity);
            await _context.SaveChangesAsync(ct);

            return await GetAsync(teamArticleEntity.Id, ct);
        }

        public async Task<ViewModels.TeamArticle> UpdateAsync(Guid id, ViewModels.TeamArticle teamArticle, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new CanIncrementIncidentRequirement())).Succeeded)
                throw new ForbiddenException();

            var teamArticleToUpdate = await _context.TeamArticles.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (teamArticleToUpdate == null)
                throw new EntityNotFoundException<TeamArticle>();

            teamArticle.CreatedBy = teamArticleToUpdate.CreatedBy;
            teamArticle.DateCreated = teamArticleToUpdate.DateCreated;
            teamArticle.ModifiedBy = _user.GetId();
            teamArticle.DateModified = DateTime.UtcNow;
            _mapper.Map(teamArticle, teamArticleToUpdate);

            _context.TeamArticles.Update(teamArticleToUpdate);
            await _context.SaveChangesAsync(ct);

            teamArticle = await GetAsync(teamArticleToUpdate.Id, ct);

            return teamArticle;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var teamArticleToDelete = await _context.TeamArticles.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (teamArticleToDelete == null)
                throw new EntityNotFoundException<TeamArticle>();

            _context.TeamArticles.Remove(teamArticleToDelete);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        public async Task<bool> DeleteByIdsAsync(Guid teamId, Guid articleId, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var teamArticleToDelete = await _context.TeamArticles.SingleOrDefaultAsync(v => (v.ArticleId == articleId) && (v.TeamId == teamId), ct);

            if (teamArticleToDelete == null)
                throw new EntityNotFoundException<TeamArticle>();

            _context.TeamArticles.Remove(teamArticleToDelete);
            await _context.SaveChangesAsync(ct);

            return true;
        }

    }
}

