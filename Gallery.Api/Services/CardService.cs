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
using Gallery.Api.Data;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Data.Models;
using Gallery.Api.Infrastructure.Authorization;
using Gallery.Api.Infrastructure.Exceptions;
using Gallery.Api.Infrastructure.Extensions;
using Gallery.Api.ViewModels;

namespace Gallery.Api.Services
{
    public interface ICardService
    {
        Task<IEnumerable<ViewModels.Card>> GetAsync(CancellationToken ct);
        Task<ViewModels.Card> GetAsync(Guid id, CancellationToken ct);
        Task<IEnumerable<ViewModels.Card>> GetByCollectionAsync(Guid collectionId, CancellationToken ct);
        Task<IEnumerable<ViewModels.Card>> GetByExhibitAsync(Guid exhibitId, CancellationToken ct);
        Task<IEnumerable<ViewModels.Card>> GetByExhibitTeamAsync(Guid exhibitId, Guid teamId, CancellationToken ct);
        Task<ViewModels.Card> CreateAsync(ViewModels.Card card, CancellationToken ct);
        Task<ViewModels.Card> UpdateAsync(Guid id, ViewModels.Card card, CancellationToken ct);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    }

    public class CardService : ICardService
    {
        private readonly GalleryDbContext _context;
        private readonly IAuthorizationService _authorizationService;
        private readonly ClaimsPrincipal _user;
        private readonly IMapper _mapper;

        public CardService(
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

        public async Task<IEnumerable<ViewModels.Card>> GetAsync(CancellationToken ct)
        {
            IQueryable<CardEntity> cards = _context.Cards;

            return _mapper.Map<IEnumerable<Card>>(await cards.ToListAsync());
        }

        public async Task<ViewModels.Card> GetAsync(Guid id, CancellationToken ct)
        {
            var item = await _context.Cards.SingleOrDefaultAsync(sm => sm.Id == id, ct);

            return _mapper.Map<Card>(item);
        }

        public async Task<IEnumerable<ViewModels.Card>> GetByCollectionAsync(Guid collectionId, CancellationToken ct)
        {
            var cards = await _context.Cards
                .Where(c => c.CollectionId == collectionId)
                .OrderBy(c => c.Move)
                .ThenBy(c => c.Inject)
                .ProjectTo<ViewModels.Card>(_mapper.ConfigurationProvider)
                .ToListAsync(ct);

            return cards;
        }

        public async Task<IEnumerable<ViewModels.Card>> GetByExhibitAsync(Guid exhibitId, CancellationToken ct)
        {
            var exhibit = await _context.Exhibits.FirstAsync(e => e.Id == exhibitId);
            var articles = _context.Articles
                .Where(a => a.CollectionId == exhibit.CollectionId
                            && (a.Move < exhibit.CurrentMove
                                || (a.Move == exhibit.CurrentMove && a.Inject <= exhibit.CurrentInject)))
                .OrderByDescending(a => a.Move)
                .ThenByDescending(a => a.Inject);
            var cards = await _context.Cards
                .Where(c => c.CollectionId == exhibit.CollectionId)
                .OrderBy(c => c.Inject)
                .ProjectTo<ViewModels.Card>(_mapper.ConfigurationProvider)
                .ToListAsync(ct);

            return cards;
        }

        public async Task<IEnumerable<ViewModels.Card>> GetByExhibitTeamAsync(Guid exhibitId, Guid teamId, CancellationToken ct)
        {
            var exhibit = await _context.Exhibits.FindAsync(exhibitId);
            var cards = await _context.TeamCards
                .Where(tc => tc.Card.CollectionId == exhibit.CollectionId && tc.TeamId == teamId)
                .Include(tc => tc.Card)
                .Select(tc => tc.Card)
                .ProjectTo<ViewModels.Card>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return cards;
        }

        public async Task<ViewModels.Card> CreateAsync(ViewModels.Card card, CancellationToken ct)
        {
            var cardEntity = _mapper.Map<CardEntity>(card);
            cardEntity.Id = cardEntity.Id != Guid.Empty ? cardEntity.Id : Guid.NewGuid();

            _context.Cards.Add(cardEntity);
            await _context.SaveChangesAsync(ct);
            card = await GetAsync(cardEntity.Id, ct);

            return card;
        }

        public async Task<ViewModels.Card> UpdateAsync(Guid id, ViewModels.Card card, CancellationToken ct)
        {
            var cardToUpdate = await _context.Cards.SingleOrDefaultAsync(v => v.Id == id, ct);
            if (cardToUpdate == null)
                throw new EntityNotFoundException<Card>();

            _mapper.Map(card, cardToUpdate);

            _context.Cards.Update(cardToUpdate);
            await _context.SaveChangesAsync(ct);

            card = await GetAsync(cardToUpdate.Id, ct);

            return card;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            var cardToDelete = await _context.Cards.SingleOrDefaultAsync(v => v.Id == id, ct);
            _context.Cards.Remove(cardToDelete);
            await _context.SaveChangesAsync(ct);

            return true;
        }

    }
}
