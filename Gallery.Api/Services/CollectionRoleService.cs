// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using STT = System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Gallery.Api.Data;
using Gallery.Api.Infrastructure.Exceptions;
using Gallery.Api.ViewModels;
using Gallery.Api.Infrastructure.Mappings;
using System.Linq;

namespace Gallery.Api.Services
{
    public interface ICollectionRoleService
    {
        STT.Task<IEnumerable<CollectionRole>> GetAsync(CancellationToken ct);
        STT.Task<CollectionRole> GetAsync(Guid id, CancellationToken ct);
    }

    public class CollectionRoleService : ICollectionRoleService
    {
        private readonly GalleryDbContext _context;
        private readonly ClaimsPrincipal _user;

        public CollectionRoleService(GalleryDbContext context, IPrincipal user)
        {
            _context = context;
            _user = user as ClaimsPrincipal;
        }

        public async STT.Task<IEnumerable<CollectionRole>> GetAsync(CancellationToken ct)
        {
            var items = await _context.CollectionRoles
                .ToListAsync(ct);

            return items.Select(x => x.ToCollectionRole()).ToList();
        }

        public async STT.Task<CollectionRole> GetAsync(Guid id, CancellationToken ct)
        {
            var item = await _context.CollectionRoles
                .SingleOrDefaultAsync(o => o.Id == id, ct);

            if (item == null)
                throw new EntityNotFoundException<CollectionRole>();

            return item.ToCollectionRole();
        }

    }
}
