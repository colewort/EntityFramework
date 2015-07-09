// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using EntityFramework.Microbenchmarks.Core;
using EntityFramework.Microbenchmarks.EF6.Models.Orders;
using Xunit;

namespace EntityFramework.Microbenchmarks.Query
{
    public class FuncletizationTests : IClassFixture<FuncletizationTests.DatabaseFixture>
    {
        private readonly DatabaseFixture _databaseFixture;
        private static readonly int _funcletizationIterationCount = 100;

        public FuncletizationTests(DatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture;
        }

        [Benchmark(Iterations = 50, WarmupIterations = 5)]
        public void NewQueryInstance(MetricCollector collector)
        {
            using (var context = new OrdersContext(_databaseFixture.ConnectionString))
            {
                using (collector.StartCollection())
                {
                    var val = 11;
                    for (var i = 0; i < _funcletizationIterationCount; i++)
                    {
                        var result = context.Products.Where(p => p.ProductId < val).ToList();

                        Assert.Equal(10, result.Count);
                    }
                }
            }
        }

        [Benchmark(Iterations = 50, WarmupIterations = 5)]
        public void SameQueryInstance(MetricCollector collector)
        {
            using (var context = new OrdersContext(_databaseFixture.ConnectionString))
            {
                using (collector.StartCollection())
                {
                    var val = 11;
                    var query = context.Products.Where(p => p.ProductId < val);

                    for (var i = 0; i < _funcletizationIterationCount; i++)
                    {
                        var result = query.ToList();

                        Assert.Equal(10, result.Count);
                    }
                }
            }
        }

        [Benchmark(Iterations = 50, WarmupIterations = 5)]
        public void ValueFromObject(MetricCollector collector)
        {
            using (var context = new OrdersContext(_databaseFixture.ConnectionString))
            {
                using (collector.StartCollection())
                {
                    var valueHolder = new ValueHolder();
                    for (var i = 0; i < _funcletizationIterationCount; i++)
                    {
                        var result = context.Products.Where(p => p.ProductId < valueHolder.SecondLevelProperty).ToList();

                        Assert.Equal(10, result.Count);
                    }
                }
            }
        }

        public class ValueHolder
        {
            public int FirstLevelProperty { get; } = 11;

            public int SecondLevelProperty
            {
                get { return FirstLevelProperty; }
            }
        }

        public class DatabaseFixture
        {
            public DatabaseFixture()
            {
                new OrdersSeedData().EnsureCreated(
                    ConnectionString,
                    productCount: 100,
                    customerCount: 0,
                    ordersPerCustomer: 0,
                    linesPerOrder: 0);
            }

            public string ConnectionString { get; } = $@"Server={BenchmarkConfig.Instance.BenchmarkDatabaseInstance};Database=Perf_Query_Funcletization_EF6;Integrated Security=True;MultipleActiveResultSets=true;";
        }
    }
}
