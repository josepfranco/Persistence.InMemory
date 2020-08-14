using System;
using System.Collections.Generic;
using Abstractions.Persistence;

namespace Persistence.InMemory.Tests.Fixtures.Models
{
    public class Vehicle : IDomainEntity
    {
        public long Id { get; set; }
        public Guid GlobalId { get; set; }
        
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }

        public string Model { get; set; }
        public Person Driver { get; set; }
        public ICollection<Person> Passengers { get; set; } = new List<Person>();
    }
}