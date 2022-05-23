// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Api.Infrastructure.Authorization
{
    public enum UserClaimTypes
    {
        SystemAdmin,
        ContentDeveloper,
        Operator,
        BaseUser,
        CanIncrementIncident,
        CanModify,
        CanSubmit,
        ExhibitUser,
        TeamUser
    }
}

