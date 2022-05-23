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
    public interface IExhibitTeamService
    {
        Task<IEnumerable<ViewModels.ExhibitTeam>> GetAsync(CancellationToken ct);
        Task<ViewModels.ExhibitTeam> GetAsync(Guid id, CancellationToken ct);
        Task<ViewModels.ExhibitTeam> CreateAsync(ViewModels.ExhibitTeam exhibitTeam, CancellationToken ct);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct);
        Task<bool> DeleteByIdsAsync(Guid exhibitId, Guid teamId, CancellationToken ct);
    }

    public class ExhibitTeamService : IExhibitTeamService
    {
        private readonly GalleryDbContext _context;
        private readonly IAuthorizationService _authorizationService;
        private readonly ClaimsPrincipal _user;
        private readonly IMapper _mapper;

        public ExhibitTeamService(GalleryDbContext context, IAuthorizationService authorizationService, IPrincipal team, IMapper mapper)
        {
            _context = context;
            _authorizationService = authorizationService;
            _user = team as ClaimsPrincipal;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ViewModels.ExhibitTeam>> GetAsync(CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var items = await _context.ExhibitTeams
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<ExhibitTeam>>(items);
        }

        public async Task<ViewModels.ExhibitTeam> GetAsync(Guid id, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var item = await _context.ExhibitTeams
                .SingleOrDefaultAsync(o => o.Id == id, ct);

            return _mapper.Map<ExhibitTeam>(item);
        }

        public async Task<ViewModels.ExhibitTeam> CreateAsync(ViewModels.ExhibitTeam exhibitTeam, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            exhibitTeam.DateCreated = DateTime.UtcNow;
            exhibitTeam.CreatedBy = _user.GetId();
            exhibitTeam.DateModified = null;
            exhibitTeam.ModifiedBy = null;
            var exhibitTeamEntity = _mapper.Map<ExhibitTeamEntity>(exhibitTeam);
            exhibitTeamEntity.Id = exhibitTeamEntity.Id != Guid.Empty ? exhibitTeamEntity.Id : Guid.NewGuid();

            _context.ExhibitTeams.Add(exhibitTeamEntity);
            await _context.SaveChangesAsync(ct);

            return await GetAsync(exhibitTeamEntity.Id, ct);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var exhibitTeamToDelete = await _context.ExhibitTeams.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (exhibitTeamToDelete == null)
                throw new EntityNotFoundException<ExhibitTeam>();

            _context.ExhibitTeams.Remove(exhibitTeamToDelete);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        public async Task<bool> DeleteByIdsAsync(Guid exhibitId, Guid teamId, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var exhibitTeamToDelete = await _context.ExhibitTeams.SingleOrDefaultAsync(v => (v.TeamId == teamId) && (v.ExhibitId == exhibitId), ct);

            if (exhibitTeamToDelete == null)
                throw new EntityNotFoundException<ExhibitTeam>();

            _context.ExhibitTeams.Remove(exhibitTeamToDelete);
            await _context.SaveChangesAsync(ct);

            return true;
        }

    }
}

