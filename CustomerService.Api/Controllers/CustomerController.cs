using CustomerService.Application.Features.Customers.Commands.CreateCustomer;
using CustomerService.Application.Features.Customers.Commands.DeleteCustomer;
using CustomerService.Application.Features.Customers.Commands.UpdateCustomer;
using CustomerService.Application.Features.Customers.Commands.UploadProfilePicture;
using CustomerService.Application.Features.Customers.Queries.GetAllCustomers;
using CustomerService.Application.Features.Customers.Queries.GetCustomerById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Api
{
    namespace CustomerService.Api.Controllers
    {
        [ApiController]
        [Route("api/[controller]")]
        public class CustomerController : ControllerBase
        {
            private readonly IMediator _mediator;

            public CustomerController(IMediator mediator)
            {
                _mediator = mediator;
            }

            // POST: api/customer
            [HttpPost]
            public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerCommand command)
            {
                var customerId = await _mediator.Send(command);
                return CreatedAtAction(nameof(GetCustomerById), new { id = customerId }, customerId);
            }

            // GET: api/customer/{id}
            [HttpGet("{id}")]
            public async Task<IActionResult> GetCustomerById(Guid id)
            {
                var query = new GetCustomerByIdQuery(id);
                var customer = await _mediator.Send(query);

                if (customer == null)
                    return NotFound($"Customer with Id {id} not found.");

                return Ok(customer);
            }

            // GET: api/customer
            [HttpGet]
            public async Task<IActionResult> GetAllCustomers()
            {
                var query = new GetAllCustomersQuery();
                var customers = await _mediator.Send(query);
                return Ok(customers);
            }

            // PUT: api/customer/{id}
            [HttpPut("{id}")]
            public async Task<IActionResult> UpdateCustomer(Guid id, [FromBody] UpdateCustomerCommand command)
            {
                if (id != command.Id)
                    return BadRequest("Id mismatch between route and body.");

                var result = await _mediator.Send(command);

                if (!result)
                    return NotFound($"Customer with Id {id} not found.");

                return NoContent();
            }

            // DELETE: api/customer/{id}
            [HttpDelete("{id}")]
            public async Task<IActionResult> DeleteCustomer(Guid id)
            {
                var command = new DeleteCustomerCommand(id);
                var result = await _mediator.Send(command);

                if (!result)
                    return NotFound($"Customer with Id {id} not found.");

                return NoContent();
            }
            [HttpPost("{id}/profile-picture")]
            public async Task<IActionResult> UploadProfilePicture(Guid id, IFormFile file)
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded.");

                using var stream = file.OpenReadStream();

                var command = new UploadProfilePictureCommand
                {
                    CustomerId = id,
                    FileStream = stream,
                    FileName = $"{id}_{file.FileName}",
                    ContentType = file.ContentType
                };

                var blobUrl = await _mediator.Send(command);

                return Ok(new { ProfilePictureUrl = blobUrl });
            }
        }
    }
}
