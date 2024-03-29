// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Gallery.Api.Data;
using Gallery.Api.Data.Models;
using Gallery.Api.Infrastructure.Authorization;
using Gallery.Api.Infrastructure.Exceptions;
using Gallery.Api.Infrastructure.Extensions;
using Gallery.Api.ViewModels;

namespace Gallery.Api.Services
{
    public interface IUserPermissionService
    {
        Task<IEnumerable<ViewModels.UserPermission>> GetAsync(CancellationToken ct);
        Task<ViewModels.UserPermission> GetAsync(Guid id, CancellationToken ct);
        Task<ViewModels.UserPermission> CreateAsync(ViewModels.UserPermission userPermission, CancellationToken ct);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct);
        Task<bool> DeleteByIdsAsync(Guid userId, Guid permissionId, CancellationToken ct);
    }

    public class UserPermissionService : IUserPermissionService
    {
        private readonly GalleryDbContext _context;
        private readonly IAuthorizationService _authorizationService;
        private readonly ClaimsPrincipal _user;
        private readonly IMapper _mapper;
        private readonly ILogger<ITeamUserService> _logger;

        public UserPermissionService(GalleryDbContext context, IAuthorizationService authorizationService, IPrincipal user, ILogger<ITeamUserService> logger, IMapper mapper)
        {
            _context = context;
            _authorizationService = authorizationService;
            _user = user as ClaimsPrincipal;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<ViewModels.UserPermission>> GetAsync(CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new FullRightsRequirement())).Succeeded)
                throw new ForbiddenException();

            var items = await _context.UserPermissions
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<UserPermission>>(items);
        }

        public async Task<ViewModels.UserPermission> GetAsync(Guid id, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new FullRightsRequirement())).Succeeded)
                throw new ForbiddenException();

            var item = await _context.UserPermissions
                .SingleOrDefaultAsync(o => o.Id == id, ct);

            return _mapper.Map<UserPermission>(item);
        }

        public async Task<ViewModels.UserPermission> CreateAsync(ViewModels.UserPermission userPermission, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new FullRightsRequirement())).Succeeded)
                throw new ForbiddenException();

            userPermission.DateCreated = DateTime.UtcNow;
            userPermission.CreatedBy = _user.GetId();
            userPermission.DateModified = null;
            userPermission.ModifiedBy = null;
            var userPermissionEntity = _mapper.Map<UserPermissionEntity>(userPermission);
            userPermissionEntity.Id = userPermissionEntity.Id != Guid.Empty ? userPermissionEntity.Id : Guid.NewGuid();

            _context.UserPermissions.Add(userPermissionEntity);
            await _context.SaveChangesAsync(ct);
            _logger.LogWarning($"UserPermission created by {_user.GetId()} = user: {userPermission.UserId} and permission: {userPermission.PermissionId}");
            return await GetAsync(userPermissionEntity.Id, ct);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new FullRightsRequirement())).Succeeded)
                throw new ForbiddenException();

            var userPermissionToDelete = await _context.UserPermissions.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (userPermissionToDelete == null)
                throw new EntityNotFoundException<UserPermission>();

            _context.UserPermissions.Remove(userPermissionToDelete);
            await _context.SaveChangesAsync(ct);
            _logger.LogWarning($"UserPermission deleted by {_user.GetId()} = user: {userPermissionToDelete.UserId} and permission: {userPermissionToDelete.PermissionId}");
            return true;
        }

        public async Task<bool> DeleteByIdsAsync(Guid userId, Guid permissionId, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new FullRightsRequirement())).Succeeded)
                throw new ForbiddenException();

            var userPermissionToDelete = await _context.UserPermissions.SingleOrDefaultAsync(v => v.UserId == userId && v.PermissionId == permissionId, ct);

            if (userPermissionToDelete == null)
                throw new EntityNotFoundException<UserPermission>();

            _context.UserPermissions.Remove(userPermissionToDelete);
            await _context.SaveChangesAsync(ct);
            _logger.LogWarning($"UserPermission deleted by {_user.GetId()} = user: {userPermissionToDelete.UserId} and permission: {userPermissionToDelete.PermissionId}");
            return true;
        }

    }
}

