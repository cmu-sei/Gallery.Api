// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Api.Data.Enumerations;

namespace Api.ViewModels
{
    public class Card : Base
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Move { get; set; }
        public int Inject { get; set; }
        public Guid CollectionId { get; set; }
   }
}

