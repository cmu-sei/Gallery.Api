// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Gallery.Api.ViewModels
{
    public class Exhibit : Base, IAuthorizationType
    {
        public Guid Id { get; set; }
        public int CurrentMove { get; set; }
        public int CurrentInject { get; set; }
        public Guid CollectionId { get; set; }
        public Guid? ScenarioId { get; set; }
        public bool ShowAdvanceButton { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public IEnumerable<string> ExhibitPermissions { get; set; }
    }

    /// <summary>
    /// The fields an Exhibit's create and edit requests share. The Collection is not one of them: only
    /// <see cref="ExhibitCreate"/> names it, so an edit cannot point an Exhibit at another Collection.
    /// </summary>
    public abstract class ExhibitFields
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int CurrentMove { get; set; }
        public int CurrentInject { get; set; }
        public bool ShowAdvanceButton { get; set; }
        /// <summary>
        /// The Steamfitter Scenario that sends share email. Setting or changing it on an edit needs the caller to
        /// be able to read the Scenario in Steamfitter.
        /// </summary>
        public Guid? ScenarioId { get; set; }
    }

    public class ExhibitCreate : ExhibitFields
    {
        /// <summary>The Id to create the Exhibit with. A new one is made when it is empty.</summary>
        public Guid Id { get; set; }
        public Guid CollectionId { get; set; }
    }

    public class ExhibitEdit : ExhibitFields
    {
    }
}
