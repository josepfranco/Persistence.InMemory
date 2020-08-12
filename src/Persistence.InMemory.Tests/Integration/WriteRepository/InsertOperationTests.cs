using System;
using System.Linq;
using System.Threading.Tasks;
using Persistence.InMemory.Tests.Fixtures.Models;
using Xunit;

namespace Persistence.InMemory.Tests.Integration.WriteRepository
{
    public class InsertOperationTests
    {
        [Fact]
        public async Task InsertOne_WhenOne_ShouldAddOne()
        {
            // ARRANGE
            var context = new Context();
            var repository = new WriteRepository<Person>(context);
            var person = new Person
            {
                GlobalId = Guid.NewGuid(),
                Name = "Gordon Freeman",
                Age = 27
            };

            // ACT
            await repository.InsertAsync(person);

            // ASSERT
            var allPeople = context.ReadAll<Person>().ToList();
            Assert.NotEmpty(allPeople);
            Assert.Contains(person, allPeople);
        }

        [Fact]
        public async Task InsertOne_WhenOneWithId_ShouldAddOne()
        {
            // ARRANGE
            var context = new Context();
            var repository = new WriteRepository<Person>(context);
            var person = new Person
            {
                Id = 10,
                GlobalId = Guid.NewGuid(),
                Name = "Gordon Freeman",
                Age = 27
            };

            // ACT
            await repository.InsertAsync(person);

            // ASSERT
            var allPeople = context.ReadAll<Person>().ToList();
            Assert.NotEmpty(allPeople);
            Assert.Contains(person, allPeople);
        }
        
        [Fact]
        public async Task InsertOne_WhenOneWithNegativeId_ShouldThrowException()
        {
            // ARRANGE
            var context = new Context();
            var repository = new WriteRepository<Person>(context);
            var person = new Person
            {
                Id = -1,
                GlobalId = Guid.NewGuid(),
                Name = "Gordon Freeman",
                Age = 27
            };

            // ACT
            // ASSERT
            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.InsertAsync(person));
        }
        
        [Fact]
        public async Task InsertOne_WhenOneWithNoGuid_ShouldThrowException()
        {
            // ARRANGE
            var context = new Context();
            var repository = new WriteRepository<Person>(context);
            var person = new Person
            {
                Name = "Gordon Freeman",
                Age = 27
            };

            // ACT
            // ASSERT
            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.InsertAsync(person));
        }
        
        [Fact]
        public async Task InsertDifferent_WhenDifferent_ShouldAddDifferent()
        {
            // ARRANGE
            var context = new Context();
            var personRepository = new WriteRepository<Person>(context);
            var vehicleRepository = new WriteRepository<Vehicle>(context);
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
                Driver = person
            };

            // ACT
            await personRepository.InsertAsync(person);
            await vehicleRepository.InsertAsync(vehicle);

            // ASSERT
            var allPeople = context.ReadAll<Person>().ToList();
            Assert.NotEmpty(allPeople);
            Assert.Contains(person, allPeople);
            
            var allVehicles = context.ReadAll<Vehicle>().ToList();
            Assert.NotEmpty(allVehicles);
            Assert.Contains(vehicle, allVehicles);
        }
    }
}