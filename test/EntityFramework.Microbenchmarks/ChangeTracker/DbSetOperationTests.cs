// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using EntityFramework.Microbenchmarks.Core;
using EntityFramework.Microbenchmarks.Core.Models.Orders;
using EntityFramework.Microbenchmarks.Models.Orders;
using Xunit;

namespace EntityFramework.Microbenchmarks.ChangeTracker
{
    public class DbSetOperationTests : IClassFixture<DbSetOperationTests.DatabaseFixture>
    {
        private readonly DatabaseFixture _databaseFixture;

        public DbSetOperationTests(DatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture;
        }

        [Benchmark(Iterations = 100, WarmupIterations = 5)]
        public void Add(MetricCollector collector)
        {
            using (var context = new OrdersContext(_databaseFixture.ConnectionString))
            {
                var customers = new Customer[1000];
                for (var i = 0; i < customers.Length; i++)
                {
                    customers[i] = new Customer { Name = "Customer " + i };
                }

                using (collector.StartCollection())
                {
                    foreach (var customer in customers)
                    {
                        context.Customers.Add(customer);
                    }
                }
            }
        }

        [Benchmark(Iterations = 100, WarmupIterations = 5)]
        public void AddCollection(MetricCollector collector)
        {
            using (var context = new OrdersContext(_databaseFixture.ConnectionString))
            {
                var customers = new Customer[1000];
                for (var i = 0; i < customers.Length; i++)
                {
                    customers[i] = new Customer { Name = "Customer " + i };
                }

                using (collector.StartCollection())
                {
                    context.Customers.AddRange(customers);
                }
            }

        }

        [Benchmark(Iterations = 100, WarmupIterations = 5)]
        public void Attach(MetricCollector collector)
        {
            using (var context = new OrdersContext(_databaseFixture.ConnectionString))
            {
                var customers = GetAllCustomersFromDatabase();
                Assert.Equal(1000, customers.Length);

                using (collector.StartCollection())
                {
                    foreach (var customer in customers)
                    {
                        context.Customers.Attach(customer);
                    }
                }
            }

        }

        [Benchmark(Iterations = 100, WarmupIterations = 5)]
        public void AttachCollection(MetricCollector collector)
        {
            using (var context = new OrdersContext(_databaseFixture.ConnectionString))
            {
                var customers = GetAllCustomersFromDatabase();
                Assert.Equal(1000, customers.Length);

                using (collector.StartCollection())
                {
                    context.Customers.AttachRange(customers);
                }
            }
        }

        [Benchmark(Iterations = 100, WarmupIterations = 5)]
        public void Remove(MetricCollector collector)
        {
            using (var context = new OrdersContext(_databaseFixture.ConnectionString))
            {
                var customers = context.Customers.ToArray();
                Assert.Equal(1000, customers.Length);

                using (collector.StartCollection())
                {
                    foreach (var customer in customers)
                    {
                        context.Customers.Remove(customer);
                    }
                }
            }
        }

        [Benchmark(Iterations = 100, WarmupIterations = 5)]
        public void RemoveCollection(MetricCollector collector)
        {
            using (var context = new OrdersContext(_databaseFixture.ConnectionString))
            {
                var customers = context.Customers.ToArray();
                Assert.Equal(1000, customers.Length);

                using (collector.StartCollection())
                {
                    context.Customers.RemoveRange(customers);
                }
            }
        }

        [Benchmark(Iterations = 100, WarmupIterations = 5)]
        public void Update(MetricCollector collector)
        {
            using (var context = new OrdersContext(_databaseFixture.ConnectionString))
            {
                var customers = GetAllCustomersFromDatabase();
                Assert.Equal(1000, customers.Length);

                using (collector.StartCollection())
                {
                    foreach (var customer in customers)
                    {
                        context.Customers.Update(customer);
                    }
                }
            }
        }

        [Benchmark(Iterations = 100, WarmupIterations = 5)]
        public void UpdateCollection(MetricCollector collector)
        {
            using (var context = new OrdersContext(_databaseFixture.ConnectionString))
            {
                var customers = GetAllCustomersFromDatabase();
                Assert.Equal(1000, customers.Length);

                using (collector.StartCollection())
                {
                    context.Customers.UpdateRange(customers);
                }
            }
        }

        private Customer[] GetAllCustomersFromDatabase()
        {
            using (var context = new OrdersContext(_databaseFixture.ConnectionString))
            {
                return context.Customers.ToArray();
            }
        }

        public class DatabaseFixture
        {
            public DatabaseFixture()
            {
                new OrdersSeedData().EnsureCreated(
                    ConnectionString,
                    productCount: 0,
                    customerCount: 1000,
                    ordersPerCustomer: 0,
                    linesPerOrder: 0);
            }

            public string ConnectionString { get; } = $@"Server={BenchmarkConfig.Instance.BenchmarkDatabaseInstance};Database=Perf_ChangeTracker_DbSetOperation;Integrated Security=True;MultipleActiveResultSets=true;";
        }
    }
}
