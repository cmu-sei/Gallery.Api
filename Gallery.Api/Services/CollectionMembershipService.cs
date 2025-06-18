// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using STT = System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Gallery.Api.Data;
using Gallery.Api.Infrastructure.Exceptions;
using Gallery.Api.ViewModels;
using System.Linq;
using Gallery.Api.Data.Models;

namespace Gallery.Api.Services
{
    public interface ICollectionMembershipService
    {
        STT.Task<CollectionMembership> GetAsync(Guid id, CancellationToken ct);
        STT.Task<IEnumerable<CollectionMembership>> GetByCollectionAsync(Guid collectionId, CancellationToken ct);
        STT.Task<CollectionMembership> CreateAsync(CollectionMembership collectionMembership, CancellationToken ct);
        STT.Task<CollectionMembership> UpdateAsync(Guid id, CollectionMembership collectionMembership, CancellationToken ct);
        STT.Task DeleteAsync(Guid id, CancellationToken ct);
    }

    public class CollectionMembershipService : ICollectionMembershipService
    {
        private readonly GalleryDbContext _context;
        private readonly ClaimsPrincipal _user;
        private readonly IMapper _mapper;

        public CollectionMembershipService(GalleryDbContext context, IPrincipal user, IMapper mapper)
        {
            _context = context;
            _user = user as ClaimsPrincipal;
            _mapper = mapper;
        }

        public async STT.Task<CollectionMembership> GetAsync(Guid id, CancellationToken ct)
        {
            var item = await _context.CollectionMemberships
                .SingleOrDefaultAsync(o => o.Id == id, ct);

            if (item == null)
                throw new EntityNotFoundException<CollectionMembership>();

            return _mapper.Map<CollectionMembership>(item);
        }

        public async STT.Task<IEnumerable<CollectionMembership>> GetByCollectionAsync(Guid collectionId, CancellationToken ct)
        {
            var items = await _context.CollectionMemberships
                .Where(m => m.CollectionId == collectionId)
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<CollectionMembership>>(items);
        }

        public async STT.Task<CollectionMembership> CreateAsync(CollectionMembership collectionMembership, CancellationToken ct)
        {
            var collectionMembershipEntity = _mapper.Map<CollectionMembershipEntity>(collectionMembership);

            _context.CollectionMemberships.Add(collectionMembershipEntity);
            await _context.SaveChangesAsync(ct);
            var createdExhibit = await GetAsync(collectionMembershipEntity.Id, ct);

            return createdExhibit;
        }
        public async STT.Task<CollectionMembership> UpdateAsync(Guid id, CollectionMembership collectionMembership, CancellationToken ct)
        {
            var collectionMembershipToUpdate = await _context.CollectionMemberships.SingleOrDefaultAsync(v => v.Id == id, ct);
            if (collectionMembershipToUpdate == null)
                throw new EntityNotFoundException<Exhibit>();

            collectionMembershipToUpdate.RoleId = collectionMembership.RoleId;
            await _context.SaveChangesAsync(ct);

            return _mapper.Map<CollectionMembership>(collectionMembershipToUpdate);
        }
        public async STT.Task DeleteAsync(Guid id, CancellationToken ct)
        {
            var collectionMembershipToDelete = await _context.CollectionMemberships.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (collectionMembershipToDelete == null)
                throw new EntityNotFoundException<CollectionMembership>();

            _context.CollectionMemberships.Remove(collectionMembershipToDelete);
            await _context.SaveChangesAsync(ct);

            return;
        }

    }
}
