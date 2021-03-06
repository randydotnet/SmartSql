﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SmartSql.ConfigBuilder;
using SmartSql.Configuration;
using SmartSql.DataSource;
using SmartSql.Exceptions;
using SmartSql.Reflection;
using System;
using System.Linq;

namespace SmartSql.Options
{
    public class OptionConfigBuilder : AbstractConfigBuilder
    {
        private readonly SmartSqlConfigOptions _configOptions;
        public OptionConfigBuilder(SmartSqlConfigOptions configOptions, ILoggerFactory loggerFactory = null)
        {
            loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            Logger = loggerFactory.CreateLogger<XmlConfigBuilder>();
            _configOptions = configOptions;
        }
        protected override void OnBeforeBuild()
        {
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug($"OptionConfigBuilder Build  Starting.");
            }
        }
        protected override void OnAfterBuild()
        {
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug($"OptionConfigBuilder Build  End.");
            }
        }

        protected override void BuildSqlMaps()
        {
            foreach (var sqlMapSource in _configOptions.SmartSqlMaps)
            {
                var resourceType = sqlMapSource.Type;
                var path = sqlMapSource.Path;
                if (Logger.IsEnabled(LogLevel.Debug))
                {
                    Logger.LogDebug($"XmlConfigBuilder BuildSqlMap ->> ResourceType:[{resourceType}] , Path :[{path}] Starting.");
                }
                BuildSqlMap(resourceType, path);
                if (Logger.IsEnabled(LogLevel.Debug))
                {
                    Logger.LogDebug($"XmlConfigBuilder BuildSqlMap ->> ResourceType:[{resourceType}] , Path :[{path}] End.");
                }
            }
        }

        protected override void BuildTagBuilders()
        {
            foreach (var tagBuilder in _configOptions.TagBuilders)
            {
                RegisterTagBuilder(tagBuilder.Name, tagBuilder.Type);
            }
        }

        protected override void BuildTypeHandlers()
        {
            foreach (var typeHandler in _configOptions.TypeHandlers)
            {
                var typeHandlerConfig = new Configuration.TypeHandler
                {
                    Name = typeHandler.Name,
                    Properties = typeHandler.Properties,
                    HandlerType = TypeUtils.GetType(typeHandler.Type)
                };

                if (typeHandlerConfig.HandlerType.IsGenericType)
                {
                    if (!String.IsNullOrEmpty(typeHandler.PropertyType))
                    {
                        typeHandlerConfig.PropertyType = TypeUtils.GetType(typeHandler.PropertyType);
                    }
                    if (!String.IsNullOrEmpty(typeHandler.FieldType))
                    {
                        typeHandlerConfig.FieldType = TypeUtils.GetType(typeHandler.FieldType);
                    }
                }
                RegisterTypeHandler(typeHandlerConfig);
            }
        }

        protected override void BuildProperties()
        {
            SmartSqlConfig.Properties.Import(_configOptions.Properties);
        }

        protected override void BuildIdGenerator()
        {
            if (_configOptions.IdGenerator == null) { return; }
            SmartSqlConfig.IdGenerator = IdGeneratorBuilder.Build(_configOptions.IdGenerator.Type, _configOptions.IdGenerator.Properties);
        }
        protected override void BuildDatabase()
        {
            var dbProvider = _configOptions.Database.DbProvider;
            DbProviderManager.Instance.TryInit(ref dbProvider);
            SmartSqlConfig.Database = new SmartSql.DataSource.Database
            {
                DbProvider = dbProvider,
                Write = new WriteDataSource
                {
                    Name = _configOptions.Database.Write.Name,
                    DbProvider = dbProvider,
                    ConnectionString = _configOptions.Database.Write.ConnectionString
                },
                Reads = _configOptions.Database.Reads.ToDictionary(r => r.Name, r => new ReadDataSource
                {
                    Name = r.Name,
                    ConnectionString = r.ConnectionString,
                    DbProvider = dbProvider,
                    Weight = r.Weight
                })
            };
        }
        private string GetExpString(string expStr)
        {
            return SmartSqlConfig.Properties.GetPropertyValue(expStr);
        }

        public override void Dispose()
        {

        }

        protected override void BuildSettings()
        {
            SmartSqlConfig.Settings = _configOptions.Settings;
        }
    }
}
