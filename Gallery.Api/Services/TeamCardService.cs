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
using Gallery.Api.Infrastructure.Exceptions;
using Gallery.Api.Infrastructure.Extensions;
using Gallery.Api.ViewModels;

namespace Gallery.Api.Services
{
    public interface ITeamCardService
    {
        Task<IEnumerable<ViewModels.TeamCard>> GetAsync(CancellationToken ct);
        Task<IEnumerable<ViewModels.TeamCard>> GetByExhibitAsync(Guid exhibitId, CancellationToken ct);
        Task<IEnumerable<ViewModels.TeamCard>> GetByCardAsync(Guid cardId, CancellationToken ct);
        Task<IEnumerable<ViewModels.TeamCard>> GetByExhibitTeamAsync(Guid exhibitId, Guid teamId, CancellationToken ct);
        Task<ViewModels.TeamCard> GetAsync(Guid id, CancellationToken ct);
        Task<ViewModels.TeamCard> CreateAsync(ViewModels.TeamCard teamCard, CancellationToken ct);
        Task<ViewModels.TeamCard> UpdateAsync(Guid id, ViewModels.TeamCard teamCard, CancellationToken ct);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct);
        Task<bool> DeleteByIdsAsync(Guid teamId, Guid userId, CancellationToken ct);
    }

    public class TeamCardService : ITeamCardService
    {
        private readonly GalleryDbContext _context;
        private readonly IAuthorizationService _authorizationService;
        private readonly ClaimsPrincipal _user;
        private readonly IMapper _mapper;

        public TeamCardService(GalleryDbContext context, IAuthorizationService authorizationService, IPrincipal user, IMapper mapper)
        {
            _context = context;
            _authorizationService = authorizationService;
            _user = user as ClaimsPrincipal;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ViewModels.TeamCard>> GetAsync(CancellationToken ct)
        {
            var items = await _context.TeamCards
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<TeamCard>>(items);
        }

        public async Task<IEnumerable<ViewModels.TeamCard>> GetByExhibitAsync(Guid exhibitId, CancellationToken ct)
        {
            var items = await _context.TeamCards
                .Where(tc => tc.Team.ExhibitId == exhibitId)
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<TeamCard>>(items);
        }

        public async Task<IEnumerable<ViewModels.TeamCard>> GetByExhibitTeamAsync(Guid exhibitId, Guid teamId, CancellationToken ct)
        {
            var items = await _context.TeamCards
                .Where(tc => tc.TeamId == teamId)
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<TeamCard>>(items);
        }

        public async Task<IEnumerable<ViewModels.TeamCard>> GetByCardAsync(Guid cardId, CancellationToken ct)
        {
            var items = await _context.TeamCards
                .Where(tc => tc.CardId == cardId)
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<TeamCard>>(items);
        }

        public async Task<ViewModels.TeamCard> GetAsync(Guid id, CancellationToken ct)
        {
            var item = await _context.TeamCards
                .SingleOrDefaultAsync(o => o.Id == id, ct);

            return _mapper.Map<TeamCard>(item);
        }

        public async Task<ViewModels.TeamCard> CreateAsync(ViewModels.TeamCard teamCard, CancellationToken ct)
        {
            var teamCardEntity = _mapper.Map<TeamCardEntity>(teamCard);
            teamCardEntity.Id = teamCardEntity.Id != Guid.Empty ? teamCardEntity.Id : Guid.NewGuid();

            _context.TeamCards.Add(teamCardEntity);
            await _context.SaveChangesAsync(ct);

            return await GetAsync(teamCardEntity.Id, ct);
        }

        public async Task<ViewModels.TeamCard> UpdateAsync(Guid id, ViewModels.TeamCard teamCard, CancellationToken ct)
        {
            var teamCardToUpdate = await _context.TeamCards.SingleOrDefaultAsync(v => v.Id == id, ct);
            if (teamCardToUpdate == null)
                throw new EntityNotFoundException<TeamCard>();

            _mapper.Map(teamCard, teamCardToUpdate);

            _context.TeamCards.Update(teamCardToUpdate);
            await _context.SaveChangesAsync(ct);

            teamCard = await GetAsync(teamCardToUpdate.Id, ct);

            return teamCard;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            var teamCardToDelete = await _context.TeamCards.SingleOrDefaultAsync(v => v.Id == id, ct);
            if (teamCardToDelete == null)
                throw new EntityNotFoundException<TeamCard>();

            _context.TeamCards.Remove(teamCardToDelete);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        public async Task<bool> DeleteByIdsAsync(Guid teamId, Guid cardId, CancellationToken ct)
        {
            var teamCardToDelete = await _context.TeamCards.SingleOrDefaultAsync(v => (v.CardId == cardId) && (v.TeamId == teamId), ct);
            if (teamCardToDelete == null)
                throw new EntityNotFoundException<TeamCard>();

            _context.TeamCards.Remove(teamCardToDelete);
            await _context.SaveChangesAsync(ct);

            return true;
        }

    }
}
