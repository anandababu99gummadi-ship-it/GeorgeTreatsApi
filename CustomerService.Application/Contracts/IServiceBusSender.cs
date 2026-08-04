using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerService.Application.Contracts
{
    public interface IServiceBusSender
    {
        Task SendMessageAsync(string message, CancellationToken cancellationToken = default);
    }
}
