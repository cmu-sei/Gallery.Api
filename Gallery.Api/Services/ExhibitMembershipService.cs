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
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Infrastructure.Exceptions;
using SAVM = Gallery.Api.ViewModels;
using Gallery.Api.ViewModels;
using System.Linq;
using Gallery.Api.Data.Models;

namespace Gallery.Api.Services
{
    public interface IExhibitMembershipService
    {
        STT.Task<ExhibitMembership> GetAsync(Guid id, CancellationToken ct);
        STT.Task<IEnumerable<ExhibitMembership>> GetByExhibitAsync(Guid exhibitId, CancellationToken ct);
        STT.Task<ExhibitMembership> CreateAsync(ExhibitMembership exhibitMembership, CancellationToken ct);
        STT.Task<ExhibitMembership> UpdateAsync(Guid id, ExhibitMembership exhibitMembership, CancellationToken ct);
        STT.Task DeleteAsync(Guid id, CancellationToken ct);
    }

    public class ExhibitMembershipService : IExhibitMembershipService
    {
        private readonly GalleryDbContext _context;
        private readonly ClaimsPrincipal _user;
        private readonly IMapper _mapper;

        public ExhibitMembershipService(GalleryDbContext context, IPrincipal user, IMapper mapper)
        {
            _context = context;
            _user = user as ClaimsPrincipal;
            _mapper = mapper;
        }

        public async STT.Task<ExhibitMembership> GetAsync(Guid id, CancellationToken ct)
        {
            var item = await _context.ExhibitMemberships
                .SingleOrDefaultAsync(o => o.Id == id, ct);

            if (item == null)
                throw new EntityNotFoundException<ExhibitMembership>();

            return _mapper.Map<SAVM.ExhibitMembership>(item);
        }

        public async STT.Task<IEnumerable<ExhibitMembership>> GetByExhibitAsync(Guid exhibitId, CancellationToken ct)
        {
            var items = await _context.ExhibitMemberships
                .Where(m => m.ExhibitId == exhibitId)
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<SAVM.ExhibitMembership>>(items);
        }

        public async STT.Task<ExhibitMembership> CreateAsync(ExhibitMembership exhibitMembership, CancellationToken ct)
        {
            var exhibitMembershipEntity = _mapper.Map<ExhibitMembershipEntity>(exhibitMembership);

            _context.ExhibitMemberships.Add(exhibitMembershipEntity);
            await _context.SaveChangesAsync(ct);
            var createdExhibit = await GetAsync(exhibitMembershipEntity.Id, ct);

            return createdExhibit;
        }
        public async STT.Task<ExhibitMembership> UpdateAsync(Guid id, ExhibitMembership exhibitMembership, CancellationToken ct)
        {
            var exhibitMembershipToUpdate = await _context.ExhibitMemberships.SingleOrDefaultAsync(v => v.Id == id, ct);
            if (exhibitMembershipToUpdate == null)
                throw new EntityNotFoundException<SAVM.Exhibit>();

            exhibitMembershipToUpdate.RoleId = exhibitMembership.RoleId;
            await _context.SaveChangesAsync(ct);

            return _mapper.Map<SAVM.ExhibitMembership>(exhibitMembershipToUpdate);
        }
        public async STT.Task DeleteAsync(Guid id, CancellationToken ct)
        {
            var exhibitMembershipToDelete = await _context.ExhibitMemberships.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (exhibitMembershipToDelete == null)
                throw new EntityNotFoundException<SAVM.ExhibitMembership>();

            _context.ExhibitMemberships.Remove(exhibitMembershipToDelete);
            await _context.SaveChangesAsync(ct);

            return;
        }

    }
}
