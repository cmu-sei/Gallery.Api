// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Gallery.Api.Data;
using Gallery.Api.Data.Models;
using Gallery.Api.Infrastructure.Extensions;
using Gallery.Api.Infrastructure.Exceptions;
using Gallery.Api.ViewModels;
using Gallery.Api.Infrastructure.Mappings;

namespace Gallery.Api.Services
{
    public interface IUserService
    {
        Task<IEnumerable<ViewModels.User>> GetAsync(bool includePermissions, CancellationToken ct);
        Task<ViewModels.User> GetAsync(Guid id, CancellationToken ct);
        Task<IEnumerable<ViewModels.User>> GetByTeamAsync(Guid TeamId, CancellationToken ct);
        Task<ViewModels.User> CreateAsync(ViewModels.User user, CancellationToken ct);
        Task<ViewModels.User> UpdateAsync(Guid id, ViewModels.User user, CancellationToken ct);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    }

    public class UserService : IUserService
    {
        private readonly GalleryDbContext _context;
        private readonly ClaimsPrincipal _user;
        private readonly IAuthorizationService _authorizationService;
        private readonly ILogger<ITeamUserService> _logger;

        public UserService(GalleryDbContext context, IPrincipal user, IAuthorizationService authorizationService, ILogger<ITeamUserService> logger)
        {
            _context = context;
            _user = user as ClaimsPrincipal;
            _authorizationService = authorizationService;
            _logger = logger;
        }

        public async Task<IEnumerable<ViewModels.User>> GetAsync(bool includePermissions, CancellationToken ct)
        {
            // Nothing fills User.Permissions, so both callers get the same projection; the AutoMapper
            // expansion this flag asked for named a member no map filled.
            return await _context.Users
                .ProjectToUser()
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<ViewModels.User>> GetByTeamAsync(Guid teamId, CancellationToken ct)
        {
            var items = await _context.TeamUsers
                .Where(tu => tu.TeamId == teamId)
                .Select(tu => tu.User)
                .ToListAsync(ct);

            return items.Select(x => x.ToUser()).ToList();
        }

        public async Task<ViewModels.User> GetAsync(Guid id, CancellationToken ct)
        {
            var item = await _context.Users
                .ProjectToUser()
                .SingleOrDefaultAsync(o => o.Id == id, ct);
            return item;
        }

        public async Task<ViewModels.User> CreateAsync(ViewModels.User user, CancellationToken ct)
        {
            var userEntity = user.ToEntity();
            userEntity.Id = userEntity.Id != Guid.Empty ? userEntity.Id : Guid.NewGuid();
            _context.Users.Add(userEntity);
            await _context.SaveChangesAsync(ct);
            _logger.LogWarning($"User {user.Name} ({userEntity.Id}) created by {_user.GetId()}");
            return await GetAsync(user.Id, ct);
        }

        public async Task<ViewModels.User> UpdateAsync(Guid id, ViewModels.User user, CancellationToken ct)
        {
            // Don't allow changing the Id
            if (id != user.Id)
                throw new ForbiddenException("You cannot change the UserId");

            var userToUpdate = await _context.Users.SingleOrDefaultAsync(v => v.Id == id, ct);
            if (userToUpdate == null)
                throw new EntityNotFoundException<User>();

            user.ApplyTo(userToUpdate);
            _context.Users.Update(userToUpdate);
            await _context.SaveChangesAsync(ct);
            _logger.LogWarning($"User {userToUpdate.Name} ({userToUpdate.Id}) updated by {_user.GetId()}");
            return await GetAsync(id, ct);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            if (id == _user.GetId())
                throw new ForbiddenException("You cannot delete your own account");

            var userToDelete = await _context.Users
                .Include(u => u.CollectionMemberships)
                .SingleOrDefaultAsync(v => v.Id == id, ct);
            if (userToDelete == null)
                throw new EntityNotFoundException<User>();

            // Remove collection memberships first to avoid FK constraint violations
            if (userToDelete.CollectionMemberships != null && userToDelete.CollectionMemberships.Any())
            {
                _context.CollectionMemberships.RemoveRange(userToDelete.CollectionMemberships);
            }

            _context.Users.Remove(userToDelete);
            await _context.SaveChangesAsync(ct);
            _logger.LogWarning($"User ({userToDelete.Id}) deleted by {_user.GetId()}");
            return true;
        }

    }
}
