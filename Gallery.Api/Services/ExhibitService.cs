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
    public interface IExhibitService
    {
        Task<IEnumerable<ViewModels.Exhibit>> GetAsync(CancellationToken ct);
        Task<IEnumerable<ViewModels.Exhibit>> GetMineAsync(CancellationToken ct);
        Task<IEnumerable<ViewModels.Exhibit>> GetByCollectionAsync(Guid collectionId, CancellationToken ct);
        Task<IEnumerable<ViewModels.Exhibit>> GetMineByCollectionAsync(Guid collectionId, CancellationToken ct);
        Task<ViewModels.Exhibit> GetAsync(Guid id, CancellationToken ct);
        Task<ViewModels.Exhibit> CreateAsync(ViewModels.Exhibit exhibit, CancellationToken ct);
        Task<ViewModels.Exhibit> UpdateAsync(Guid id, ViewModels.Exhibit exhibit, CancellationToken ct);
        Task<ViewModels.Exhibit> SetMoveAndInjectAsync(Guid id, int move, int inject, CancellationToken ct);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    }

    public class ExhibitService : IExhibitService
    {
        private readonly GalleryDbContext _context;
        private readonly IAuthorizationService _authorizationService;
        private readonly ClaimsPrincipal _user;
        private readonly IMapper _mapper;
        private readonly IUserArticleService _userArticleService;

        public ExhibitService(
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

        public async Task<IEnumerable<ViewModels.Exhibit>> GetAsync(CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            IQueryable<ExhibitEntity> exhibits = _context.Exhibits;

            return _mapper.Map<IEnumerable<Exhibit>>(await exhibits.ToListAsync());
        }

        public async Task<IEnumerable<ViewModels.Exhibit>> GetMineAsync(CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new BaseUserRequirement())).Succeeded)
                throw new ForbiddenException();

            var userId = _user.GetId();
            var exhibits = await _context.Teams
                .Where(t => t.TeamUsers.Any(tu => tu.UserId == userId) && t.ExhibitId != null)
                .Select(et => et.Exhibit)
                .Distinct()
                .ToListAsync();

            return _mapper.Map<IEnumerable<Exhibit>>(exhibits);
        }

        public async Task<ViewModels.Exhibit> GetAsync(Guid id, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new BaseUserRequirement())).Succeeded)
                throw new ForbiddenException();

            var item = await _context.Exhibits.SingleOrDefaultAsync(sm => sm.Id == id, ct);

            return _mapper.Map<Exhibit>(item);
        }

        public async Task<IEnumerable<ViewModels.Exhibit>> GetByCollectionAsync(Guid collectionId, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            IQueryable<ExhibitEntity> exhibits = _context.Exhibits
                .Where(a => a.CollectionId == collectionId)
                .OrderByDescending(a => a.DateCreated);

            return _mapper.Map<IEnumerable<Exhibit>>(await exhibits.ToListAsync());
        }

        public async Task<IEnumerable<ViewModels.Exhibit>> GetMineByCollectionAsync(Guid collectionId, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new BaseUserRequirement())).Succeeded)
                throw new ForbiddenException();

            var userId = _user.GetId();
            IQueryable<ExhibitEntity> exhibits = _context.Teams
                .Where(t => t.TeamUsers.Any(tu => tu.UserId == userId) && t.Exhibit.CollectionId == collectionId)
                .Select(t => t.Exhibit)
                .OrderByDescending(a => a.DateCreated);

            return _mapper.Map<IEnumerable<Exhibit>>(await exhibits.ToListAsync());
        }

        public async Task<ViewModels.Exhibit> CreateAsync(ViewModels.Exhibit exhibit, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            exhibit.DateCreated = DateTime.UtcNow;
            exhibit.CreatedBy = _user.GetId();
            exhibit.DateModified = null;
            exhibit.ModifiedBy = null;
            var exhibitEntity = _mapper.Map<ExhibitEntity>(exhibit);
            exhibitEntity.Id = exhibitEntity.Id != Guid.Empty ? exhibitEntity.Id : Guid.NewGuid();

            _context.Exhibits.Add(exhibitEntity);
            await _context.SaveChangesAsync(ct);
            exhibit = await GetAsync(exhibitEntity.Id, ct);

            return exhibit;
        }

        public async Task<ViewModels.Exhibit> UpdateAsync(Guid id, ViewModels.Exhibit exhibit, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var exhibitToUpdate = await _context.Exhibits.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (exhibitToUpdate == null)
                throw new EntityNotFoundException<Exhibit>();

            exhibit.CreatedBy = exhibitToUpdate.CreatedBy;
            exhibit.DateCreated = exhibitToUpdate.DateCreated;
            exhibit.ModifiedBy = _user.GetId();
            exhibit.DateModified = DateTime.UtcNow;
            _mapper.Map(exhibit, exhibitToUpdate);
            _context.Exhibits.Update(exhibitToUpdate);
            await _context.SaveChangesAsync(ct);
            await _userArticleService.LoadUserArticlesAsync(exhibitToUpdate, ct);

            exhibit = await GetAsync(exhibitToUpdate.Id, ct);

            return exhibit;
        }

        public async Task<ViewModels.Exhibit> SetMoveAndInjectAsync(Guid id, int move, int inject, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var exhibitToUpdate = await _context.Exhibits.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (exhibitToUpdate == null)
                throw new EntityNotFoundException<Exhibit>();

            exhibitToUpdate.ModifiedBy = _user.GetId();
            exhibitToUpdate.DateModified = DateTime.UtcNow;
            exhibitToUpdate.CurrentMove = move;
            exhibitToUpdate.CurrentInject = inject;
            await _context.SaveChangesAsync(ct);
            await _userArticleService.LoadUserArticlesAsync(exhibitToUpdate, ct);

            var updatedExhibit = await GetAsync(exhibitToUpdate.Id, ct);

            return updatedExhibit;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var exhibitToDelete = await _context.Exhibits.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (exhibitToDelete == null)
                throw new EntityNotFoundException<Exhibit>();

            _context.Exhibits.Remove(exhibitToDelete);
            await _context.SaveChangesAsync(ct);

            return true;
        }

    }
}

