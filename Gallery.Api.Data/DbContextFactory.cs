// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.
using System;
using Microsoft.EntityFrameworkCore;
namespace Gallery.Api.Data;
public class GalleryDbContextFactory : IDbContextFactory<GalleryDbContext>
{
    private readonly IDbContextFactory<GalleryDbContext> _pooledFactory;
    private readonly IServiceProvider _serviceProvider;
    public GalleryDbContextFactory(
        IDbContextFactory<GalleryDbContext> pooledFactory,
        IServiceProvider serviceProvider)
    {
        _pooledFactory = pooledFactory;
        _serviceProvider = serviceProvider;
    }
    public GalleryDbContext CreateDbContext()
    {
        var context = _pooledFactory.CreateDbContext();
        // Inject the current scope's ServiceProvider
        context.ServiceProvider = _serviceProvider;
        return context;
    }
}