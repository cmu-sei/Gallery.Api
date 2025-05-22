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
        private readonly IMapper _mapper;

        public CollectionRoleService(GalleryDbContext context, IPrincipal user, IMapper mapper)
        {
            _context = context;
            _user = user as ClaimsPrincipal;
            _mapper = mapper;
        }

        public async STT.Task<IEnumerable<CollectionRole>> GetAsync(CancellationToken ct)
        {
            var items = await _context.CollectionRoles
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<CollectionRole>>(items);
        }

        public async STT.Task<CollectionRole> GetAsync(Guid id, CancellationToken ct)
        {
            var item = await _context.CollectionRoles
                .SingleOrDefaultAsync(o => o.Id == id, ct);

            if (item == null)
                throw new EntityNotFoundException<CollectionRole>();

            return _mapper.Map<CollectionRole>(item);
        }

    }
}
