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
    public interface ITeamUserService
    {
        Task<IEnumerable<ViewModels.TeamUser>> GetAsync(CancellationToken ct);
        Task<ViewModels.TeamUser> GetAsync(Guid id, CancellationToken ct);
        Task<IEnumerable<ViewModels.Team>> GetMineAsync(CancellationToken ct);
        Task<IEnumerable<ViewModels.Team>> GetByUserAsync(Guid userId, CancellationToken ct);
        Task<ViewModels.TeamUser> CreateAsync(ViewModels.TeamUser teamUser, CancellationToken ct);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct);
        Task<bool> DeleteByIdsAsync(Guid teamId, Guid userId, CancellationToken ct);
    }

    public class TeamUserService : ITeamUserService
    {
        private readonly ApiDbContext _context;
        private readonly IAuthorizationService _authorizationService;
        private readonly ClaimsPrincipal _user;
        private readonly IMapper _mapper;

        public TeamUserService(ApiDbContext context, IAuthorizationService authorizationService, IPrincipal user, IMapper mapper)
        {
            _context = context;
            _authorizationService = authorizationService;
            _user = user as ClaimsPrincipal;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ViewModels.TeamUser>> GetAsync(CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var items = await _context.TeamUsers
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<TeamUser>>(items);
        }

        public async Task<IEnumerable<ViewModels.Team>> GetMineAsync(CancellationToken ct)
        {
            if(!(await _authorizationService.AuthorizeAsync(_user, null, new BaseUserRequirement())).Succeeded)
                throw new ForbiddenException();

            var items = await _context.TeamUsers
                .Where(w => w.UserId == _user.GetId())
                .Select(x => x.Team)
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<Team>>(items);
        }

        public async Task<IEnumerable<ViewModels.Team>> GetByUserAsync(Guid userId, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var items = await _context.TeamUsers
                .Where(w => w.UserId == userId)
                .Select(x => x.Team)
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<Team>>(items);
        }

        public async Task<ViewModels.TeamUser> GetAsync(Guid id, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var item = await _context.TeamUsers
                .Include(tu => tu.User)
                .Include(tu => tu.Team)
                .SingleOrDefaultAsync(o => o.Id == id, ct);

            return _mapper.Map<TeamUser>(item);
        }

        public async Task<ViewModels.TeamUser> CreateAsync(ViewModels.TeamUser teamUser, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            teamUser.DateCreated = DateTime.UtcNow;
            teamUser.CreatedBy = _user.GetId();
            teamUser.DateModified = null;
            teamUser.ModifiedBy = null;
            var teamUserEntity = _mapper.Map<TeamUserEntity>(teamUser);
            teamUserEntity.Id = teamUserEntity.Id != Guid.Empty ? teamUserEntity.Id : Guid.NewGuid();

            _context.TeamUsers.Add(teamUserEntity);
            await _context.SaveChangesAsync(ct);

            return await GetAsync(teamUserEntity.Id, ct);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var teamUserToDelete = await _context.TeamUsers.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (teamUserToDelete == null)
                throw new EntityNotFoundException<TeamUser>();

            _context.TeamUsers.Remove(teamUserToDelete);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        public async Task<bool> DeleteByIdsAsync(Guid teamId, Guid userId, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var teamUserToDelete = await _context.TeamUsers.SingleOrDefaultAsync(v => (v.UserId == userId) && (v.TeamId == teamId), ct);

            if (teamUserToDelete == null)
                throw new EntityNotFoundException<TeamUser>();

            _context.TeamUsers.Remove(teamUserToDelete);
            await _context.SaveChangesAsync(ct);

            return true;
        }

    }
}

