using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AnimalAPI.Database;
using AnimalAPI.Models;
using AnimalAPI.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Newtonsoft.Json;
using NUnit.Framework;

namespace AnimalAPI.Tests
{
    public class AnimalControllerIntegrationTests
    {
        private WebApplicationFactory<Startup> _factory;
        private AnimalService _animalService;

        [OneTimeSetUp]
        public void TestOneTimeSetup()
        {
            _factory = new WebApplicationFactory<Startup>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureAppConfiguration((context, conf) =>
                    {
                        conf.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.test.json"));
                    });
                });


            var settings = (IAnimalDatabaseSettings)_factory.Services.GetService(typeof(IAnimalDatabaseSettings));
            _animalService = (AnimalService)_factory.Services.GetService(typeof(IAnimalService));
        }

        [TearDown]
        public void TestTearDown()
        {
            _animalService.MongoClient.DropDatabase("animal_tst");
        }

        [Test]
        public async Task GetCats_EndpointReturnsSomeData()
        {
            // Arrange
            var catsToCreate = new List<Cat> {
                new Cat { Hisses = true, Name = "Loki" },
                new Cat { Hisses = false, Name = "Felix" },
                new Cat { Hisses = true, Name = "Mario" },
                new Cat { Hisses = true, Name = "Esper" }
            };
            await _animalService.Animals.InsertManyAsync(catsToCreate);
            // since we don't need to check this data, throw it on the pile.
            var dogsToCreate = new List<Dog>
            {
                new Dog { Name = "Thor" },
                new Dog { Name = "Vadem" },
                new Dog { Name = "Vork" }
            };
            await _animalService.Animals.InsertManyAsync(dogsToCreate);
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("cats");
            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            var body = await response.Content.ReadAsStringAsync();
            var deserializedResponse = JsonConvert.DeserializeObject<List<Cat>>(body).OrderBy(x => x.Id).ToList();
            deserializedResponse.Should().BeEquivalentTo(catsToCreate.OrderBy(x => x.Id));
        }

        [Test]
        public async Task GetDogs_EndpointReturnsSomeData()
        {
            // Arrange
            var catsToCreate = new List<Cat> {
                new Cat { Hisses = true, Name = "Loki" },
                new Cat { Hisses = false, Name = "Felix" },
                new Cat { Hisses = true, Name = "Mario" },
                new Cat { Hisses = true, Name = "Esper" }
            };
            await _animalService.Animals.InsertManyAsync(catsToCreate);
            var dogsToCreate = new List<Dog>
            {
                new Dog { Name = "Thor" },
                new Dog { Name = "Vadem" },
                new Dog { Name = "Vork" }
            };
            await _animalService.Animals.InsertManyAsync(dogsToCreate);

            var client = _factory.CreateClient();
            // Act
            var response = await client.GetAsync("dogs");
            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            var body = await response.Content.ReadAsStringAsync();
            var deserializedResponse = JsonConvert.DeserializeObject<List<Dog>>(body).OrderBy(x => x.Id).ToList();
            deserializedResponse.Should().BeEquivalentTo(dogsToCreate.OrderBy(x => x.Id));
        }

        [Test]
        public async Task PersistDog()
        {
            var dog = new Dog()
            {
                Barks = true,
                Name = "Barbkbark",
                PottyTrained = false
            };

            var buffer = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dog));
            var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            var client = _factory.CreateClient();

            // Act
            var response = await client.PostAsync("dogs", byteContent);
            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            var body = await response.Content.ReadAsStringAsync();
            var deserializedResponse = JsonConvert.DeserializeObject<Dog>(body);
            deserializedResponse.Name.Should().Be(dog.Name);
            deserializedResponse.PottyTrained.Should().Be(dog.PottyTrained);
            deserializedResponse.Barks.Should().Be(dog.Barks);
            deserializedResponse.Id.Should().NotBeNullOrWhiteSpace();

            response.Headers.Location.Should().Be($"/dogs/{deserializedResponse.Id}");
        }

        [Test]
        public async Task GetSingleDog()
        {
            var insertDog = new Dog()
            {
                Barks = true,
                Name = "Barbkbark",
                PottyTrained = false
            };

            var buffer = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(insertDog));
            var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            var client = _factory.CreateClient();

            // Act
            var responsePost = await client.PostAsync("dogs", byteContent);
            var response = await client.GetAsync(responsePost.Headers.Location);
            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            var body = await response.Content.ReadAsStringAsync();
            var deserializedResponse = JsonConvert.DeserializeObject<Dog>(body);
            deserializedResponse.Name.Should().Be(insertDog.Name);
            deserializedResponse.PottyTrained.Should().Be(insertDog.PottyTrained);
            deserializedResponse.Barks.Should().Be(insertDog.Barks);
            deserializedResponse.Id.Should().NotBeNullOrWhiteSpace();
        }

        [Test]
        public async Task GetSingleDog_IdDoesNotExist()
        {
            // Arrange
            var client = _factory.CreateClient();
            // Act
            var response = await client.GetAsync($"/dogs/blabla");
            // Assert
            response.StatusCode.Should().Be(404);
        }
    }
}
