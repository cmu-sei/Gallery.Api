// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Gallery.Api.Data.Enumerations;

namespace Gallery.Api.ViewModels
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

    /// <summary>
    /// The fields a Card's create and edit requests share. The Collection is not one of them: only
    /// <see cref="CardCreate"/> names it, so an edit cannot move a Card to another Collection.
    /// </summary>
    public abstract class CardFields
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Move { get; set; }
        public int Inject { get; set; }
    }

    public class CardCreate : CardFields
    {
        /// <summary>The Id to create the Card with. A new one is made when it is empty.</summary>
        public Guid Id { get; set; }
        public Guid CollectionId { get; set; }
    }

    public class CardEdit : CardFields
    {
    }
}
