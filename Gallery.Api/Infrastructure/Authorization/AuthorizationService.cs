// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gallery.Api.Data;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.ViewModels;
using Gallery.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Gallery.Api.Infrastructure.Authorization;

public interface IGalleryAuthorizationService
{
    Task<bool> AuthorizeAsync(
        SystemPermission[] requiredSystemPermissions,
        CancellationToken cancellationToken);

    Task<bool> AuthorizeAsync<T>(
        Guid? resourceId,
        SystemPermission[] requiredSystemPermissions,
        ExhibitPermission[] requiredExhibitPermissions,
        CancellationToken cancellationToken) where T : IAuthorizationType;

    Task<bool> AuthorizeAsync<T>(
        Guid? resourceId,
        SystemPermission[] requiredSystemPermissions,
        CollectionPermission[] requiredCollectionPermissions,
        CancellationToken cancellationToken) where T : IAuthorizationType;

    IEnumerable<Guid> GetAuthorizedExhibitIds();
    IEnumerable<SystemPermission> GetSystemPermissions();
    IEnumerable<ExhibitPermissionClaim> GetExhibitPermissions(Guid? exhibitId = null);
    IEnumerable<CollectionPermissionClaim> GetCollectionPermissions(Guid? collectionId = null);
}

public class AuthorizationService(
    IAuthorizationService authService,
    IIdentityResolver identityResolver,
    GalleryDbContext dbContext) : IGalleryAuthorizationService
{
    public async Task<bool> AuthorizeAsync(
        SystemPermission[] requiredSystemPermissions,
        CancellationToken cancellationToken)
    {
        return await HasSystemPermission<IAuthorizationType>(requiredSystemPermissions);
    }

    public async Task<bool> AuthorizeAsync<T>(
        Guid? resourceId,
        SystemPermission[] requiredSystemPermissions,
        ExhibitPermission[] requiredExhibitPermissions,
        CancellationToken cancellationToken) where T : IAuthorizationType
    {
        var claimsPrincipal = identityResolver.GetClaimsPrincipal();
        bool succeeded = await HasSystemPermission<IAuthorizationType>(requiredSystemPermissions);

        if (!succeeded && resourceId.HasValue)
        {
            var exhibitId = await GetExhibitId<T>(resourceId.Value, cancellationToken);

            if (exhibitId != null)
            {
                var exhibitPermissionRequirement = new ExhibitPermissionRequirement(requiredExhibitPermissions, exhibitId.Value);
                var exhibitPermissionResult = await authService.AuthorizeAsync(claimsPrincipal, null, exhibitPermissionRequirement);

                succeeded = exhibitPermissionResult.Succeeded;
            }

        }

        return succeeded;
    }

    public async Task<bool> AuthorizeAsync<T>(
        Guid? resourceId,
        SystemPermission[] requiredSystemPermissions,
        CollectionPermission[] requiredCollectionPermissions,
        CancellationToken cancellationToken) where T : IAuthorizationType
    {
        var claimsPrincipal = identityResolver.GetClaimsPrincipal();
        bool succeeded = await HasSystemPermission<IAuthorizationType>(requiredSystemPermissions);

        if (!succeeded && resourceId.HasValue)
        {
            var collectionId = await GetCollectionId<T>(resourceId.Value, cancellationToken);

            if (collectionId != null)
            {
                var collectionPermissionRequirement = new CollectionPermissionRequirement(requiredCollectionPermissions, collectionId.Value);
                var collectionPermissionResult = await authService.AuthorizeAsync(claimsPrincipal, null, collectionPermissionRequirement);

                succeeded = collectionPermissionResult.Succeeded;
            }

        }

        return succeeded;
    }

    public IEnumerable<Guid> GetAuthorizedExhibitIds()
    {
        return identityResolver.GetClaimsPrincipal().Claims
            .Where(x => x.Type == AuthorizationConstants.ExhibitPermissionClaimType)
            .Select(x => ExhibitPermissionClaim.FromString(x.Value).ExhibitId)
            .ToList();
    }

    public IEnumerable<SystemPermission> GetSystemPermissions()
    {
        var principal = identityResolver.GetClaimsPrincipal();
        var claims = principal.Claims;
        var permissions = claims
           .Where(x => x.Type == AuthorizationConstants.PermissionClaimType)
           .Select(x =>
           {
               if (Enum.TryParse<SystemPermission>(x.Value, out var permission))
                   return permission;

               return (SystemPermission?)null;
           })
           .Where(x => x.HasValue)
           .Select(x => x.Value)
           .ToList();
        return permissions;
    }

    public IEnumerable<ExhibitPermissionClaim> GetExhibitPermissions(Guid? exhibitId = null)
    {
        var permissions = identityResolver.GetClaimsPrincipal().Claims
           .Where(x => x.Type == AuthorizationConstants.ExhibitPermissionClaimType)
           .Select(x => ExhibitPermissionClaim.FromString(x.Value));

        if (exhibitId.HasValue)
        {
            permissions = permissions.Where(x => x.ExhibitId == exhibitId.Value);
        }

        return permissions;
    }

    public IEnumerable<CollectionPermissionClaim> GetCollectionPermissions(Guid? collectionId = null)
    {
        var permissions = identityResolver.GetClaimsPrincipal().Claims
           .Where(x => x.Type == AuthorizationConstants.CollectionPermissionClaimType)
           .Select(x => CollectionPermissionClaim.FromString(x.Value));

        if (collectionId.HasValue)
        {
            permissions = permissions.Where(x => x.CollectionId == collectionId.Value);
        }

        return permissions;
    }

    public IEnumerable<TeamPermissionClaim> GetTeamPermissions(Guid? teamId = null)
    {
        var permissions = identityResolver.GetClaimsPrincipal().Claims
           .Where(x => x.Type == AuthorizationConstants.TeamPermissionClaimType)
           .Select(x => TeamPermissionClaim.FromString(x.Value));

        if (teamId.HasValue)
        {
            permissions = permissions.Where(x => x.TeamId == teamId.Value);
        }

        return permissions;
    }

    private async Task<bool> HasSystemPermission<T>(
        SystemPermission[] requiredSystemPermissions) where T : IAuthorizationType
    {
        var claimsPrincipal = identityResolver.GetClaimsPrincipal();
        var permissionRequirement = new SystemPermissionRequirement(requiredSystemPermissions);
        var permissionResult = await authService.AuthorizeAsync(claimsPrincipal, null, permissionRequirement);

        return permissionResult.Succeeded;
    }

    private async Task<Guid?> GetExhibitId<T>(Guid resourceId, CancellationToken cancellationToken)
    {
        return typeof(T) switch
        {
            var t when t == typeof(Exhibit) => resourceId,
            var t when t == typeof(ExhibitMembership) => await GetExhibitIdFromExhibitMembership(resourceId, cancellationToken),
            _ => throw new NotImplementedException($"Handler for type {typeof(T).Name} is not implemented.")
        };
    }

    private async Task<Guid?> GetCollectionId<T>(Guid resourceId, CancellationToken cancellationToken)
    {
        return typeof(T) switch
        {
            var t when t == typeof(Collection) => resourceId,
            var t when t == typeof(Exhibit) => await GetCollectionIdFromExhibit(resourceId, cancellationToken),
            var t when t == typeof(ExhibitMembership) => await GetCollectionIdFromCollectionMembership(resourceId, cancellationToken),
            _ => throw new NotImplementedException($"Handler for type {typeof(T).Name} is not implemented.")
        };
    }

    private async Task<Guid> GetExhibitIdFromExhibitMembership(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.ExhibitMemberships
            .Where(x => x.Id == id)
            .Select(x => x.ExhibitId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Guid> GetCollectionIdFromExhibit(Guid id, CancellationToken cancellationToken)
    {
        return (Guid)await dbContext.Exhibits
            .Where(x => x.Id == id)
            .Select(x => x.CollectionId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Guid> GetCollectionIdFromCollectionMembership(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.CollectionMemberships
            .Where(x => x.Id == id)
            .Select(x => x.CollectionId)
            .FirstOrDefaultAsync(cancellationToken);
    }

}
