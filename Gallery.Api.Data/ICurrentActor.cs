// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Gallery.Api.Data
{
    /// <summary>
    /// The user a change is saved for. <see cref="GalleryDbContext"/> stamps CreatedBy and ModifiedBy with it,
    /// so a request body cannot set either. Id is null outside a request, such as in background work or
    /// seeding; the context then keeps the values the code set.
    /// </summary>
    public interface ICurrentActor
    {
        Guid? Id { get; }
    }
}
