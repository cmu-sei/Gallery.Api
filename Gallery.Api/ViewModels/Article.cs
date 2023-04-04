// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using Gallery.Api.Data.Enumerations;

namespace Gallery.Api.ViewModels
{
    public class Article : Base
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid CollectionId { get; set; }
        public Guid? ExhibitId { get; set; }
        public Guid? CardId { get; set; }
        public int Move { get; set; }
        public int Inject { get; set; }
        public ItemStatus Status  { get; set; }
        public SourceType SourceType  { get; set; }
        public string SourceName { get; set; }
        public string Url { get; set; }
        public DateTime DatePosted { get; set; }
        public bool OpenInNewTab { get; set; }
        public ICollection<Team> Teams { get; set; } = new List<Team>();
   }
}

