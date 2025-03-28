// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Gallery.Api.ViewModels
{
    public class Exhibit : Base
    {
        public Guid Id { get; set; }
        public int CurrentMove { get; set; }
        public int CurrentInject { get; set; }
        public Guid CollectionId { get; set; }
        public Guid? ScenarioId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
