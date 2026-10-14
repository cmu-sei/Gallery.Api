// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gallery.Api.Data.Enumerations
{
    // named for the color the wall shows; a lower value is more severe.
    // declared best to worst; swagger and the generated clients list them by value instead
    // the values are stored, so they must not change
    public enum ItemStatus
    {
        Gray = 0,      // was Unused
        Green = 50,    // was Open
        Yellow = 40,   // was Affected
        Orange = 20,   // was Critical
        Red = 10       // was Closed
    }

    public enum SourceType
    {
        News = 10,
        Social = 20,
        Email = 30,
        Phone = 40,
        Intel = 50,
        Reporting = 60,
        Orders = 70
    }

    public enum SystemPermission
    {
        CreateCollections,
        ViewCollections,
        EditCollections,
        ManageCollections,
        CreateExhibits,
        ViewExhibits,
        EditExhibits,
        ManageExhibits,
        ViewUsers,
        ManageUsers,
        ViewRoles,
        ManageRoles,
        ViewGroups,
        ManageGroups
    }

    public enum ExhibitPermission
    {
        ViewExhibit,
        EditExhibit,
        ManageExhibit,
        ParticipateExhibit
    }

    public enum CollectionPermission
    {
        ViewCollection,
        EditCollection,
        ManageCollection,
        ParticipateCollection
    }

    public enum TeamPermission
    {
        ViewTeam,
        EditTeam,
        ManageTeam,
        ParticipateTeam
    }

}
