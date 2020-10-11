using MediatR;
using MicroRabbit.Domain.Core.Bus;
using MiroRabbit.Banking.Domain.Commands;
using MiroRabbit.Banking.Domain.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MiroRabbit.Banking.Domain.CommandHandlers
{
    public class TransferCommandHandler : IRequestHandler<CreateTransferCommand, bool>
    {
        private readonly IEventBus _bus;
        public TransferCommandHandler(IEventBus bus)
        {
            _bus = bus;
        }
        public Task<bool> Handle(CreateTransferCommand request, CancellationToken cancellationToken)
        {
            // Publish event to RabbitMQ
            _bus.Publish(new TransferCreatedEvent(request.From, request.To, request.Amount));
            return Task.FromResult(true);
        }

        //public async Task<bool> Handle(CreateTransferCommand request, CancellationToken cancellationToken)
        //{
        //    return await Task.FromResult(true);
        //}
    }
}
