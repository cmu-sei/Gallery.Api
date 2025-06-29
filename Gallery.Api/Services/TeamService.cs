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
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Gallery.Api.Data;
using Gallery.Api.Data.Models;
using Gallery.Api.Infrastructure.Extensions;
using Gallery.Api.Infrastructure.Authorization;
using Gallery.Api.Infrastructure.Exceptions;
using Gallery.Api.ViewModels;

namespace Gallery.Api.Services
{
    public interface ITeamService
    {
        Task<IEnumerable<ViewModels.Team>> GetAsync(CancellationToken ct);
        Task<ViewModels.Team> GetAsync(Guid id, CancellationToken ct);
        Task<IEnumerable<ViewModels.Team>> GetMineByExhibitAsync(Guid exhibitId, CancellationToken ct);
        Task<IEnumerable<ViewModels.Team>> GetByUserAsync(Guid userId, CancellationToken ct);
        Task<IEnumerable<ViewModels.Team>> GetByCardAsync(Guid cardId, CancellationToken ct);
        Task<IEnumerable<ViewModels.Team>> GetByExhibitAsync(Guid exhibitId, bool checkForTeamMembership, CancellationToken ct);
        Task<ViewModels.Team> CreateAsync(ViewModels.Team team, CancellationToken ct);
        Task<ViewModels.Team> UpdateAsync(Guid id, ViewModels.Team team, CancellationToken ct);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    }

    public class TeamService : ITeamService
    {
        private readonly GalleryDbContext _context;
        private readonly ClaimsPrincipal _user;
        private readonly IAuthorizationService _authorizationService;
        private readonly IMapper _mapper;
        private readonly ILogger<ITeamService> _logger;

        public TeamService(GalleryDbContext context, IPrincipal team, IAuthorizationService authorizationService, ILogger<ITeamService> logger, IMapper mapper)
        {
            _context = context;
            _user = team as ClaimsPrincipal;
            _authorizationService = authorizationService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<ViewModels.Team>> GetAsync(CancellationToken ct)
        {
            var items = await _context.Teams
                .ProjectTo<ViewModels.Team>(_mapper.ConfigurationProvider)
                .ToArrayAsync(ct);
            return items;
        }

        public async Task<ViewModels.Team> GetAsync(Guid id, CancellationToken ct)
        {
            var item = await _context.Teams
                .ProjectTo<ViewModels.Team>(_mapper.ConfigurationProvider, dest => dest.Users)
                .SingleOrDefaultAsync(o => o.Id == id, ct);
            return item;
        }

        public async Task<IEnumerable<ViewModels.Team>> GetMineByExhibitAsync(Guid exhibitId, CancellationToken ct)
        {
            var userId = _user.GetId();
            var items = await _context.Teams
                .Where(w => w.ExhibitId == exhibitId)
                .Include(t => t.TeamUsers)
                .ThenInclude(tu => tu.User)
                .ToListAsync(ct);

            var myTeamUser = await _context.TeamUsers
                .SingleOrDefaultAsync(tu => tu.UserId == userId && tu.Team.ExhibitId == exhibitId, ct);
            if (myTeamUser == null)
            {
                items = [];
            }
            else if (!myTeamUser.IsObserver)
            {
                items = items
                    .Where(team => team.Id == myTeamUser.Team.Id)
                    .ToList();
            }

            return _mapper.Map<IEnumerable<Team>>(items);
        }

        public async Task<IEnumerable<ViewModels.Team>> GetByUserAsync(Guid userId, CancellationToken ct)
        {
            var items = await _context.TeamUsers
                .Where(w => w.UserId == userId)
                .Select(x => x.Team)
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<Team>>(items);
        }

        public async Task<IEnumerable<ViewModels.Team>> GetByCardAsync(Guid cardId, CancellationToken ct)
        {
            var items = await _context.TeamCards
                .Where(tc => tc.CardId == cardId)
                .Select(x => x.Team)
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<Team>>(items);
        }

        public async Task<IEnumerable<ViewModels.Team>> GetByExhibitAsync(Guid exhibitId, bool checkForTeamMembership, CancellationToken ct)
        {
            if (checkForTeamMembership)
            {
                var userId = _user.GetId();
                var isMember = await _context.TeamUsers
                    .AnyAsync(tu => tu.UserId == userId && tu.Team.ExhibitId == exhibitId, ct);
                if(!isMember)
                    throw new ForbiddenException();
            }
            var items = await _context.Teams
                .Include(t => t.TeamUsers)
                .ThenInclude(tu => tu.User)
                .Where(t => t.ExhibitId == exhibitId)
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<Team>>(items);
        }

        public async Task<ViewModels.Team> CreateAsync(ViewModels.Team team, CancellationToken ct)
        {
            var teamEntity = _mapper.Map<TeamEntity>(team);
            teamEntity.Id = teamEntity.Id != Guid.Empty ? teamEntity.Id : Guid.NewGuid();

            _context.Teams.Add(teamEntity);
            await _context.SaveChangesAsync(ct);
             _logger.LogWarning($"Team {team.Name} ({teamEntity.Id}) in Exhibit {team.ExhibitId} created by {_user.GetId()}");
            return await GetAsync(teamEntity.Id, ct);
        }

        public async Task<ViewModels.Team> UpdateAsync(Guid id, ViewModels.Team team, CancellationToken ct)
        {
            // Don't allow changing your own Id
            if (id != team.Id)
            {
                throw new ForbiddenException("You cannot change the team Id");
            }

            var teamToUpdate = await _context.Teams.SingleOrDefaultAsync(v => v.Id == id, ct);
            if (teamToUpdate == null)
                throw new EntityNotFoundException<Team>();

            _mapper.Map(team, teamToUpdate);

            _context.Teams.Update(teamToUpdate);
            await _context.SaveChangesAsync(ct);
            _logger.LogWarning($"Team {teamToUpdate.Name} ({teamToUpdate.Id}) in Exhibit {team.ExhibitId} updated by {_user.GetId()}");
            return await GetAsync(id, ct);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            var teamToDelete = await _context.Teams.SingleOrDefaultAsync(v => v.Id == id, ct);
            if (teamToDelete == null)
                throw new EntityNotFoundException<Team>();

            _context.Teams.Remove(teamToDelete);
            await _context.SaveChangesAsync(ct);
            _logger.LogWarning($"Team {teamToDelete.Name} ({teamToDelete.Id}) in Evaluation {teamToDelete.ExhibitId} deleted by {_user.GetId()}");
            return true;
        }

    }
}
