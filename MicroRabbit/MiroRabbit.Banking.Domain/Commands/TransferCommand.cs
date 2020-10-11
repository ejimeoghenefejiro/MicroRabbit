using MicroRabbit.Domain.Core.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiroRabbit.Banking.Domain.Commands
{
    public abstract class TransferCommand : Command // abstract means only those who can extend it can reset
    {
        public int From { get; set; }
        public int To { get; protected set; }
        public decimal Amount { get; protected set; }

    }
}
