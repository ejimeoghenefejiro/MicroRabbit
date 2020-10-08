using Microsoft.EntityFrameworkCore;
using MiroRabbit.Banking.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroRabbit.Banking.Data.Context
{
    public class BankingDbContext : DbContext 
    {
        public BankingDbContext( DbContextOptions options) : base(options)
        {
             
        }
        public DbSet<Account> accounts { get; set; }
    }
}
