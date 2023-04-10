// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gallery.Api.Data.Enumerations
{
    public enum ItemStatus
    {
        Unused = 0,
        Closed = 10,
        Critical = 20,
        Affected = 40,
        Open = 50
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

}

