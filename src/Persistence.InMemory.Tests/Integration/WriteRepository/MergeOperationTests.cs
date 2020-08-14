using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Persistence.InMemory.Tests.Fixtures.Models;
using Xunit;

namespace Persistence.InMemory.Tests.Integration.WriteRepository
{
    public class MergeOperationTests
    {
        [Fact]
        public async Task MergeOne_WhenNonExistingNestedEntity_ShouldCreateAll()
        {
            // ARRANGE
            var context = new Context();
            var repository = new WriteRepository<Vehicle>(context);

            var person = new Person
            {
                GlobalId = Guid.NewGuid(),
                Name = "Gordon Freeman",
                Age = 27
            };

            var vehicle = new Vehicle
            {
                GlobalId = Guid.NewGuid(),
                Model = "Pontiac",
                Driver = person,
                Passengers = new List<Person>
                {
                    person,
                    new Person
                    {
                        GlobalId = Guid.NewGuid(),
                        Name = "Gordon Freeman 2",
                        Age = 28
                    }
                }
            };

            // ACT
            await repository.MergeAsync(vehicle);

            // ASSERT
            var allVehicles = context.ReadAll<Vehicle>().ToList();
            Assert.Single(allVehicles);
            Assert.Contains(vehicle, allVehicles);
            Assert.NotEqual(default, allVehicles[0].CreatedAt);
            Assert.NotEqual(default, allVehicles[0].ModifiedAt);

            var allPeople = context.ReadAll<Person>().ToList();
            Assert.Equal(2, allPeople.Count);
        }
    }
}