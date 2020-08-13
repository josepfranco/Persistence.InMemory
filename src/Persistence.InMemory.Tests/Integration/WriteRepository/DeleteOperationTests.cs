using System;
using System.Linq;
using System.Threading.Tasks;
using Persistence.InMemory.Tests.Fixtures.Models;
using Xunit;

namespace Persistence.InMemory.Tests.Integration.WriteRepository
{
    public class DeleteOperationTests
    {
        [Fact]
        public async Task DeleteOne_WhenExistingOne_ShouldDeleteOne()
        {
            // ARRANGE
            var context = new Context();
            var repository = new WriteRepository<Person>(context);
            var person1 = new Person
            {
                GlobalId = Guid.NewGuid(),
                Name = "Gordon Freeman",
                Age = 27
            };
            await repository.InsertAsync(person1);

            // ACT
            await repository.DeleteAsync(person1);

            // ASSERT
            var allPeople = context.ReadAll<Person>().ToList();
            Assert.Empty(allPeople);
        }
        
        [Fact]
        public async Task DeleteOne_WhenExistingOthers_ShouldDeleteOne()
        {
            // ARRANGE
            var context = new Context();
            var repository = new WriteRepository<Person>(context);
            var person1 = new Person
            {
                GlobalId = Guid.NewGuid(),
                Name = "Gordon Freeman",
                Age = 27
            };
            var person2 = new Person
            {
                GlobalId = Guid.NewGuid(),
                Name = "Gordon Freeman 2",
                Age = 28
            };
            await repository.InsertRangeAsync(new[]{ person1, person2 });

            // ACT
            await repository.DeleteAsync(person2);

            // ASSERT
            var allPeople = context.ReadAll<Person>().ToList();
            Assert.NotEmpty(allPeople);
            Assert.Contains(person1, allPeople);
        }
        
        [Fact]
        public async Task DeleteOne_WhenNonExistingEntity_ShouldThrowException()
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
            // ASSERT
            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.DeleteAsync(person));
        }

        [Fact]
        public async Task DeleteOne_WhenNoInternalIdEntity_ShouldThrowException()
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
            // ASSERT
            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.DeleteAsync(person));
        }
        
        [Fact]
        public async Task DeleteOne_WhenNoGuidEntity_ShouldThrowException()
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
            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.DeleteAsync(person));
        }
    }
}