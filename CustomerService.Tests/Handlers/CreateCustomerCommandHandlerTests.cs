using CustomerService.Application.Contracts;
using CustomerService.Application.Features.Customers.Commands.CreateCustomer;
using CustomerService.Application.Features.Customers.Commands.CreateCustomer.CustomerService.Application.Features.Customers.Commands.CreateCustomer;
using CustomerService.Domain.Entities;
using CustomerService.Domain.ValueObjects;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerService.Tests.Handlers
{
    public class CreateCustomerCommandHandlerTests
    {
        private readonly Mock<ICustomerRepository> _mockRepository;
        private readonly CreateCustomerCommandHandler _handler;
        private readonly Mock<IServiceBusSender> _mockServiceBusSender;

        public CreateCustomerCommandHandlerTests()
        {
            // Arrange (common setup, To use for every Test)
            _mockRepository = new Mock<ICustomerRepository>();
            _mockServiceBusSender = new Mock<IServiceBusSender>();
            _handler = new CreateCustomerCommandHandler(_mockRepository.Object, _mockServiceBusSender.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_CreatesCustomerAndReturnsId()
        {
            // Arrange
            var command = new CreateCustomerCommand
            {
                Name = "Chandu",
                Email = "chandu@gmail.com",
                Phone = "9848022338",
                Location = new Address("MVP", "Vizag", "Andhra Pradesh", "530017", "India")
            };

            _mockRepository
                .Setup(repo => repo.ExistsByEmailAsync(command.Email))
                .ReturnsAsync(false);   // "we are telling no one is there by suing this email".

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotEqual(Guid.Empty, result);   // Id Should not Empty
            _mockRepository.Verify(repo => repo.AddAsync(It.IsAny<Customer>()), Times.Once);
            // "verifying AddAsync called exactly one time or not? 
        }

        [Fact]
        public async Task Handle_DuplicateEmail_ThrowsInvalidOperationException()
        {
            // Arrange
            var command = new CreateCustomerCommand
            {
                Name = "Chandu",
                Email = "chandu@gmail.com",
                Phone = "9848022338",
                Location = new Address("MVP", "Vizag", "Andhra Pradesh", "530017", "India")
            };

            _mockRepository
                .Setup(repo => repo.ExistsByEmailAsync(command.Email))
                .ReturnsAsync(true);   // "we are telling this email ios already there

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _handler.Handle(command, CancellationToken.None));

            _mockRepository.Verify(repo => repo.AddAsync(It.IsAny<Customer>()), Times.Never);
            // If Email duplicate , Don't call AddAsync
        }
    }
}
