// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Commands.Utilities;
using Microsoft.Data.Entity.Internal;
using Microsoft.Data.Entity.Relational.Design.CodeGeneration;
using Microsoft.Data.Entity.Relational.Design.ReverseEngineering;
using Microsoft.Data.Entity.Relational.Design.Templating;
using Microsoft.Data.Entity.Relational.Design.Templating.Compilation;
using Microsoft.Data.Entity.Relational.Design.Utilities;
using Microsoft.Data.Entity.Storage;
using Microsoft.Data.Entity.Utilities;
using Microsoft.Framework.Logging;

namespace Microsoft.Data.Entity.Commands
{
    public class DatabaseTool
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly LazyRef<ILogger> _logger;

        public DatabaseTool(
            [CanBeNull] IServiceProvider serviceProvider,
            [NotNull] ILoggerProvider loggerProvider)
        {
            Check.NotNull(loggerProvider, nameof(loggerProvider));

            _serviceProvider = new ServiceProvider(serviceProvider);
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(loggerProvider);
            _logger = new LazyRef<ILogger>(() => loggerFactory.CreateLogger<DatabaseTool>());
            _serviceProvider.AddService(typeof(ILogger), _logger.Value);
            _serviceProvider.AddService(typeof(IFileService), new FileSystemFileService());
            _serviceProvider.AddService(typeof(CSharpCodeGeneratorHelper), new CSharpCodeGeneratorHelper());
            _serviceProvider.AddService(typeof(ModelUtilities), new ModelUtilities());
            var metadataReferencesProvider = new MetadataReferencesProvider(_serviceProvider);
            _serviceProvider.AddService(typeof(MetadataReferencesProvider), metadataReferencesProvider);
            var compilationService = new RoslynCompilationService();
            _serviceProvider.AddService(typeof(ITemplating), new RazorTemplating(compilationService, metadataReferencesProvider));
        }

        public virtual Task<IReadOnlyList<string>> ReverseEngineerAsync(
            [NotNull] string runtimeProviderAssemblyName,
            [NotNull] string connectionString,
            [NotNull] string rootNamespace,
            [NotNull] string projectDir,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Check.NotNull(runtimeProviderAssemblyName, nameof(runtimeProviderAssemblyName));
            Check.NotEmpty(connectionString, nameof(connectionString));
            Check.NotEmpty(rootNamespace, nameof(rootNamespace));
            Check.NotEmpty(projectDir, nameof(projectDir));

            Assembly runtimeProviderAssembly = null;
            try
            {
                runtimeProviderAssembly = Assembly.Load(new AssemblyName(runtimeProviderAssemblyName));
            }
            catch(Exception exception)
            {
                throw new InvalidOperationException(
                    Strings.CannotFindRuntimeProviderAssembly(runtimeProviderAssemblyName), exception);
            }

            var designTimeServicesTypeAttribute = (ProviderDesignTimeServicesAttribute)runtimeProviderAssembly
                .GetCustomAttribute(typeof(ProviderDesignTimeServicesAttribute));
            if (designTimeServicesTypeAttribute == null)
            {
                throw new InvalidOperationException(
                    Strings.CannotFindDesignTimeProviderAssemblyAttribute(
                        nameof(ProviderDesignTimeServicesAttribute), runtimeProviderAssemblyName));
            }

            var designTimeTypeName = designTimeServicesTypeAttribute.TypeName;
            var designTimeAssemblyName =
                designTimeServicesTypeAttribute.AssemblyName ?? runtimeProviderAssemblyName;

            var designTimeProvider = GetDesignTimeProvider(designTimeTypeName, designTimeAssemblyName);

            var configuration = new ReverseEngineeringConfiguration
            {
                Provider = designTimeProvider,
                ConnectionString = connectionString,
                Namespace = rootNamespace,
                OutputPath = projectDir
            };

            var generator = new ReverseEngineeringGenerator(_serviceProvider);
            return generator.GenerateAsync(configuration, cancellationToken);
        }

        public virtual IDatabaseMetadataModelProvider GetDesignTimeProvider(
            [NotNull] string providerTypeFullName, [NotNull] string providerAssemblyName)
        {
            Check.NotNull(providerTypeFullName, nameof(providerTypeFullName));
            Check.NotNull(providerAssemblyName, nameof(providerAssemblyName));

            Assembly designTimeProviderAssembly = null;
            try
            {
                designTimeProviderAssembly = Assembly.Load(new AssemblyName(providerAssemblyName));
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    Strings.CannotFindDesignTimeProviderAssembly(providerAssemblyName), exception);
            }

            var designTimeMetadataProviderFactoryType =
                designTimeProviderAssembly.GetType(providerTypeFullName);
            if (designTimeMetadataProviderFactoryType == null)
            {
                throw new InvalidOperationException(
                    Strings.DesignTimeAssemblyProviderDoesNotContainSpecifiedType(
                        designTimeProviderAssembly.FullName,
                        providerTypeFullName));
            }

            var designTimeMetadataProviderFactory =
                (IDesignTimeMetadataProviderFactory)Activator
                    .CreateInstance(designTimeMetadataProviderFactoryType);
            return designTimeMetadataProviderFactory.Create(_serviceProvider);
        }
    }
}
