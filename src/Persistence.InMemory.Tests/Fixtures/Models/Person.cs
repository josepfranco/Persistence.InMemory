using System;
using Abstractions.Persistence;

namespace Persistence.InMemory.Tests.Fixtures.Models
{
    public class Person : IDomainEntity
    {
        public long Id { get; set; }
        public Guid GlobalId { get; set; }
        
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        
        public string Name { get; set; }
        public int Age { get; set; }
    }
}