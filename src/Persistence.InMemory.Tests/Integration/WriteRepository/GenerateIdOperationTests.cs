using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Persistence.InMemory.Tests.Fixtures.Models;
using Xunit;

namespace Persistence.InMemory.Tests.Integration.WriteRepository
{
    public class GenerateIdOperationTests
    {
        [Fact]
        public async Task GenerateId_WhenOneValid_ShouldIncrementId()
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
            var testPerson = context.ReadAll<Person>().ToList()[0];
            
            Assert.NotNull(testPerson);
            Assert.True(testPerson.Id > 0);
        }
        
        [Theory]
        [InlineData(5)]
        [InlineData(100)]
        [InlineData(5000)]
        public async Task GenerateId_WhenMultipleValid_ShouldIncrementMultipleId(int repetitions)
        {
            // ARRANGE
            var context = new Context();
            var repository = new WriteRepository<Person>(context);
            var personList = new List<Person>();

            for (var i = 0; i < repetitions; i++)
            {
                personList.Add(new Person
                {
                    GlobalId = Guid.NewGuid(),
                    Name = "Gordon Freeman",
                    Age = 27
                });
            }

            // ACT
            await repository.InsertRangeAsync(personList);

            // ASSERT
            var testPersonList = context.ReadAll<Person>().ToList();
            Assert.NotEmpty(testPersonList);
            for (var i = 0; i < repetitions; i++)
            {
                var testPerson = testPersonList[i];
                Assert.Equal(i+1, testPerson.Id);
            }
        }
    }
}