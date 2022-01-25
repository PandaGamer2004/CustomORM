using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using CustomORM.Exceptions;
using CustomORM.Interfaces;

namespace CustomORM.OrmLogic
{
    public class SimpleCommandBuilder<T> : ICommandBuilder<T> where T : class, new()
    {
        private readonly EntityInfoCollector _entityInfoCollector = EntityInfoCollector.Instance;
        private readonly EntityInfo _entityInfo;
        private const String DefaultSelectPattern = "{0} from [{1}]";
        private const String DefaultSelectAllString = "SELECT " + DefaultSelectPattern;
        private const String DefaultSelectTopString = "SELECT TOP{2} " + DefaultSelectPattern;
        private const String DefaultDeleteString = "DELETE FROM [{0}]";
        private const String DefaultInsertString = "INSERT INTO [{0}]({1})";
        private const String DefaultInnerJoinString = "INNER JOIN [{0}] ON";
        private const String WherePattern = "[{0}].[{1}] = {2}";
        private const String InnerJoinOnPattern = "[{0}].[{1}] = [{2}].[{3}]";
        private const String DefaultUpdateString = "UPDATE [dbo].[{0}]";


        public SimpleCommandBuilder()
        {
            _entityInfo = _entityInfoCollector.GetEntityInfoForType(typeof(T));
        }

        private String GetInsertTemplate()
        {
            var sb = new StringBuilder();
            var declaredEntityProperties = _entityInfo.EntityProperties;
            var dbColumnNames = _entityInfo.GetDbColumnNamesFromPropertyInfos(declaredEntityProperties);
            sb.AppendFormat(DefaultInsertString, _entityInfo.TableName, String.Join(", ", dbColumnNames));
            sb.AppendLine();
            sb.Append("VALUES");
            return sb.ToString();
        }

        private IEnumerable<SqlParameter> GetSqlParamsForEntityProperties(IEnumerable<PropertyInfo> entityProperties,
            T entity)
        {
            var dbColumnNames = _entityInfo.GetDbColumnNamesFromPropertyInfos(entityProperties);
            var paramsNames = dbColumnNames.Select(name => "@" + name);

            var sqlParameters = entityProperties.Zip(paramsNames,
                (property, paramName) => new SqlParameter
                {
                    SqlDbType = _entityInfo.GetDbTypeForGivenProperty(property),
                    Value = _entityInfo.GetDbColumnValueForEntity(property, entity),
                    ParameterName = paramName
                });

            return sqlParameters;
        }

        private String GetValueSection(IEnumerable<SqlParameter> sqlParams)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("({0})", String.Join(",", sqlParams
                .Select(parameter => parameter.ParameterName)));
            return sb.ToString();
        }

        private SqlParameter GetPkSqlParameterForEntity<T2>(T2 pkValue)
        {
            var pkColumnName = _entityInfo.GetDbColumnNameFromPropertyInfo(_entityInfo.PrimaryKey);
            var paramName = $"@{pkColumnName}";
            var resultParam = new SqlParameter()
            {
                ParameterName = paramName,
                Value = pkValue,
                SqlDbType = _entityInfo.GetDbTypeForGivenProperty(_entityInfo.PrimaryKey)
            };
            return resultParam;
        }

        private String GetQueryPartFilteredOnPrimaryKey(SqlParameter primaryKeyParam)
        {
            var sb = new StringBuilder();
            sb.Append("WHERE ");
            sb.AppendFormat(WherePattern,
                _entityInfo.TableName,
                primaryKeyParam.ParameterName[1..],
                primaryKeyParam.ParameterName);

            return sb.ToString();
        }

        private QueryEntity GenerateSelectCommandIdSearch<T2>(T2 id)
        {
            var sb = new StringBuilder();
            var columnNames = _entityInfo.GetDbColumnNamesFromPropertyInfos(_entityInfo.EntityProperties);
            sb.AppendFormat(DefaultSelectTopString, String.Join(", ", columnNames), _entityInfo.TableName, 1);

            var pkParam = GetPkSqlParameterForEntity(id);
            var whereQueryPart = GetQueryPartFilteredOnPrimaryKey(pkParam);
            sb.AppendLine(whereQueryPart);

            return new QueryEntity(sb.ToString(), new[] {pkParam});
        }

        private String GetJoinOnClause(PropertyInfo propertyToInclude, EntityInfo navPropertyEntityInfo)
        {
            String? innerJoinOnString;
            if(typeof(IEnumerable).IsAssignableFrom(propertyToInclude.PropertyType))
            {
                var fkNavProperty = navPropertyEntityInfo.NavigationalProperties.
                    FirstOrDefault(info => info.PropertyType == typeof(T));

                if (fkNavProperty is null)
                    throw new DbIntegrityException("No navigational Property for related enities" + nameof(T));
                
                var fkProperty = navPropertyEntityInfo.GetForeignKeyForNavigationProperty(fkNavProperty);
                
                innerJoinOnString = String.Format(InnerJoinOnPattern,
                    _entityInfo.TableName,
                    _entityInfo.GetDbColumnNameFromPropertyInfo(_entityInfo.PrimaryKey),
                    navPropertyEntityInfo.TableName,
                    navPropertyEntityInfo.GetDbColumnNameFromPropertyInfo(fkProperty));
            }
            else
            {
                var fkProperty = _entityInfo.GetForeignKeyForNavigationProperty(propertyToInclude);
                innerJoinOnString = String.Format(InnerJoinOnPattern,
                    _entityInfo.TableName,
                    _entityInfo.GetDbColumnNameFromPropertyInfo(fkProperty),
                    navPropertyEntityInfo.TableName,
                    navPropertyEntityInfo.GetDbColumnNameFromPropertyInfo(navPropertyEntityInfo.PrimaryKey));
            }

            return innerJoinOnString;
        }

        public QueryEntity GenerateSelectCommand()
        {
            var sb = new StringBuilder();
            sb.AppendFormat(DefaultSelectAllString, String.Join(",",
                _entityInfo.GetDbColumnNamesFromPropertyInfos(_entityInfo.EntityProperties)),
                _entityInfo.TableName);
            return new QueryEntity(sb.ToString(), null);
        }

        public QueryEntity GenerateSelectCommand(Guid id)
        {
            return GenerateSelectCommandIdSearch(id);
        }

        public QueryEntity GenerateSelectCommand(int id)
        {
            return GenerateSelectCommandIdSearch(id);
        }


        public QueryEntity GenerateInsertCommand(T entity)
        {
            var sb = new StringBuilder();
            var insertTemplate = GetInsertTemplate();
            sb.AppendLine(insertTemplate);

            var entityProps = _entityInfo.EntityProperties;
            var insertParams = GetSqlParamsForEntityProperties(entityProps, entity);
            var insertSection = GetValueSection(insertParams);
            sb.AppendLine(insertSection);

            return new QueryEntity(sb.ToString(), insertParams);
        }


        public QueryEntity GenerateUpdateCommand(T entity)
        {
            var sb = new StringBuilder();
            sb.AppendFormat(DefaultUpdateString, _entityInfo.TableName);
            sb.AppendLine();
            sb.Append("SET ");

            var propertiesToUpdate =
                _entityInfo.EntityProperties.Except(new[] {_entityInfo.PrimaryKey});
            var updateSqlParams = GetSqlParamsForEntityProperties(propertiesToUpdate, entity);
            var updateSqlParamsList = updateSqlParams.ToList();
            foreach (var sqlParam in updateSqlParamsList)
            {
                sb.AppendLine($"{sqlParam.ParameterName[1..]} = " +
                              $"{sqlParam.ParameterName},");
            }

            var pkValue = _entityInfo.GetPropertyValueForEntity(_entityInfo.PrimaryKey, entity);
            var pkSqlParam = GetPkSqlParameterForEntity(pkValue);
            updateSqlParamsList.Add(pkSqlParam);
            var queryPartFilteredOnPrimaryKey = GetQueryPartFilteredOnPrimaryKey(pkSqlParam);
            sb.AppendLine(queryPartFilteredOnPrimaryKey);

            return new QueryEntity(sb.ToString(),
                updateSqlParamsList);
        }


        public QueryEntity GenerateDeleteCommand(T entity)
        {
            var sb = new StringBuilder();
            sb.AppendFormat(DefaultDeleteString, _entityInfo.TableName);

            var pkParam = GetPkSqlParameterForEntity(entity);
            var wherePart = GetQueryPartFilteredOnPrimaryKey(pkParam);
            sb.AppendLine();
            sb.Append(wherePart);

            return new QueryEntity(sb.ToString(), new[] {pkParam});
        }

        public QueryEntity GenerateNavigationalPropertyIncludeQuery(PropertyInfo propertyToInclude)
        {
            Type propertyType;
            propertyType = typeof(IEnumerable).IsAssignableFrom(propertyToInclude.PropertyType) ?
                propertyToInclude.PropertyType.GetGenericArguments()[0] : propertyToInclude.PropertyType;
            
            var navPropertyEntityInfo = _entityInfoCollector.GetEntityInfoForType(propertyType);
            var innerJoinOnClause = GetJoinOnClause(propertyToInclude, navPropertyEntityInfo);

            var sb = new StringBuilder();

            var selectColumnNamesForIncludingProperty = navPropertyEntityInfo.GetDbColumnNamesFromPropertyInfos(
                navPropertyEntityInfo.EntityProperties);
            sb.AppendFormat(DefaultSelectAllString,String.Join(",", selectColumnNamesForIncludingProperty), _entityInfo.TableName);
            sb.AppendLine();
            sb.AppendFormat(DefaultInnerJoinString, navPropertyEntityInfo.TableName);
            sb.Append(" ");
            sb.AppendLine(innerJoinOnClause);

            return new QueryEntity(sb.ToString(), null);
        }
    }
}