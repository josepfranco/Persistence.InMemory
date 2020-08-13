using System;
using System.Linq;
using System.Threading.Tasks;
using Persistence.InMemory.Tests.Fixtures.Models;
using Xunit;

namespace Persistence.InMemory.Tests.Integration.WriteRepository
{
    public class UpdateOperationTests
    {

        [Fact]
        public async Task UpdateOne_WhenExistingOne_ShouldUpdateOne()
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
            await repository.InsertAsync(person);

            var savedModifiedDate = person.ModifiedAt;
            var savedCreationDate = person.CreatedAt;

            // ACT
            person.Name = "Gordon Freeman 2";
            person.Age = 28;
            await repository.UpdateAsync(person);

            // ASSERT
            var allPeople = context.ReadAll<Person>().ToList();
            Assert.Single(allPeople);
            Assert.Equal("Gordon Freeman 2", allPeople[0].Name);
            Assert.Equal(28, allPeople[0].Age);
            Assert.True(savedModifiedDate < allPeople[0].ModifiedAt);
            Assert.Equal(savedCreationDate, allPeople[0].CreatedAt);
        }
        
        [Fact]
        public async Task UpdateOne_WhenDifferentInstance_ShouldReplaceOldInstance()
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
            await repository.InsertAsync(person);

            // ACT
            var newPerson = new Person
            {
                Id = person.Id,
                GlobalId = person.GlobalId,
                Name = "Kevin Bacon",
                Age = 50,
                CreatedAt = person.CreatedAt,
                CreatedBy = person.CreatedBy,
                ModifiedAt = person.ModifiedAt,
                ModifiedBy = person.ModifiedBy
            };
            await repository.UpdateAsync(newPerson);

            // ASSERT
            var allPeople = context.ReadAll<Person>().ToList();
            Assert.Single(allPeople);
            Assert.Equal("Kevin Bacon", allPeople[0].Name);
            Assert.Equal(50, allPeople[0].Age);
            Assert.True(person.ModifiedAt < allPeople[0].ModifiedAt);
            Assert.Equal(person.CreatedAt, allPeople[0].CreatedAt);
        }
    }
}