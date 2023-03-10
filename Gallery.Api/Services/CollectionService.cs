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
    public interface ICollectionService
    {
        Task<IEnumerable<ViewModels.Collection>> GetAsync(CancellationToken ct);
        Task<IEnumerable<ViewModels.Collection>> GetMineAsync(CancellationToken ct);
        Task<ViewModels.Collection> GetAsync(Guid id, CancellationToken ct);
        Task<ViewModels.Collection> CreateAsync(ViewModels.Collection collection, CancellationToken ct);
        Task<ViewModels.Collection> UpdateAsync(Guid id, ViewModels.Collection collection, CancellationToken ct);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    }

    public class CollectionService : ICollectionService
    {
        private readonly GalleryDbContext _context;
        private readonly IAuthorizationService _authorizationService;
        private readonly ClaimsPrincipal _user;
        private readonly IMapper _mapper;

        public CollectionService(
            GalleryDbContext context,
            IAuthorizationService authorizationService,
            IPrincipal user,
            IMapper mapper)
        {
            _context = context;
            _authorizationService = authorizationService;
            _user = user as ClaimsPrincipal;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ViewModels.Collection>> GetAsync(CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new BaseUserRequirement())).Succeeded)
                throw new ForbiddenException();

            IQueryable<CollectionEntity> collections = _context.Collections;

            return _mapper.Map<IEnumerable<Collection>>(await collections.ToListAsync());
        }

        public async Task<IEnumerable<ViewModels.Collection>> GetMineAsync(CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new BaseUserRequirement())).Succeeded)
                throw new ForbiddenException();

            var userId = _user.GetId();
            IQueryable<CollectionEntity> collections = _context.Teams
                .Where(t => t.TeamUsers.Any(tu => tu.UserId == userId) && t.Exhibit.CollectionId != null)
                .Select(t => t.Exhibit.Collection)
                .Distinct()
                .OrderBy(c => c.Name);

            return _mapper.Map<IEnumerable<Collection>>(await collections.ToListAsync());
        }

        public async Task<ViewModels.Collection> GetAsync(Guid id, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new BaseUserRequirement())).Succeeded)
                throw new ForbiddenException();

            var item = await _context.Collections.SingleOrDefaultAsync(sm => sm.Id == id, ct);

            return _mapper.Map<Collection>(item);
        }

        public async Task<ViewModels.Collection> CreateAsync(ViewModels.Collection collection, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            collection.DateCreated = DateTime.UtcNow;
            collection.CreatedBy = _user.GetId();
            collection.DateModified = null;
            collection.ModifiedBy = null;
            var collectionEntity = _mapper.Map<CollectionEntity>(collection);
            collectionEntity.Id = collectionEntity.Id != Guid.Empty ? collectionEntity.Id : Guid.NewGuid();

            _context.Collections.Add(collectionEntity);
            await _context.SaveChangesAsync(ct);
            collection = await GetAsync(collectionEntity.Id, ct);

            return collection;
        }

        public async Task<ViewModels.Collection> UpdateAsync(Guid id, ViewModels.Collection collection, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new CanIncrementIncidentRequirement())).Succeeded)
                throw new ForbiddenException();

            var collectionToUpdate = await _context.Collections.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (collectionToUpdate == null)
                throw new EntityNotFoundException<Collection>();

            collection.CreatedBy = collectionToUpdate.CreatedBy;
            collection.DateCreated = collectionToUpdate.DateCreated;
            collection.ModifiedBy = _user.GetId();
            collection.DateModified = DateTime.UtcNow;
            _mapper.Map(collection, collectionToUpdate);

            _context.Collections.Update(collectionToUpdate);
            await _context.SaveChangesAsync(ct);

            collection = await GetAsync(collectionToUpdate.Id, ct);

            return collection;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var collectionToDelete = await _context.Collections.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (collectionToDelete == null)
                throw new EntityNotFoundException<Collection>();

            _context.Collections.Remove(collectionToDelete);
            await _context.SaveChangesAsync(ct);

            return true;
        }

    }
}

