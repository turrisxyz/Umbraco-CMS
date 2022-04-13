using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Extensions.Logging;
using NPoco;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Persistence.Querying;
using Umbraco.Cms.Core.Persistence.Repositories;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Infrastructure.Persistence.Dtos;
using Umbraco.Cms.Infrastructure.Persistence.Factories;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

namespace Umbraco.Cms.Infrastructure.Persistence.Repositories.Implement
{
    internal class DomainRepository : EntityRepositoryBase<int, IDomain>, IDomainRepository
    {
        public DomainRepository(IScopeAccessor scopeAccessor, AppCaches cache, ILogger<DomainRepository> logger)
            : base(scopeAccessor, cache, logger)
        { }

        protected override IRepositoryCachePolicy<IDomain, int> CreateCachePolicy()
            => new FullDataSetRepositoryCachePolicy<IDomain, int>(GlobalIsolatedCache, ScopeAccessor, GetEntityId, false);

        protected override IDomain PerformGet(int id)
            // Use the underlying GetAll which will force cache all domains
            => GetMany().FirstOrDefault(x => x.Id == id);

        protected override IEnumerable<IDomain> PerformGetAll(params int[] ids)
        {
            var sql = GetBaseQuery(false).Where<DomainDto>(x => x.Id > 0);
            if (ids.Any())
            {
                sql.WhereIn<DomainDto>(x => x.Id, ids);
            }
            sql.OrderBy<DomainDto>(dto => dto.SortOrder);

            return Database.Fetch<DomainDto>(sql).Select(DomainFactory.BuildEntity);
        }

        protected override IEnumerable<IDomain> PerformGetByQuery(IQuery<IDomain> query)
            => throw new NotSupportedException("This repository does not support this method");

        protected override Sql<ISqlContext> GetBaseQuery(bool isCount)
        {
            var sql = Sql();
            if (isCount)
            {
                sql.SelectCount().From<DomainDto>();
            }
            else
            {
                sql.Select("umbracoDomain.*, umbracoLanguage.languageISOCode")
                    .From<DomainDto>()
                    .LeftJoin<LanguageDto>()
                    .On<DomainDto, LanguageDto>(dto => dto.DefaultLanguage, dto => dto.Id);
            }

            return sql;
        }

        protected override string GetBaseWhereClause()
            => $"{Constants.DatabaseSchema.Tables.Domain}.id = @id";

        protected override IEnumerable<string> GetDeleteClauses()
            => new[]
            {
                "DELETE FROM umbracoDomain WHERE id = @id"
            };

        protected override void PersistNewItem(IDomain entity)
        {
            var exists = Database.ExecuteScalar<int>("SELECT COUNT(*) FROM umbracoDomain WHERE domainName = @domainName", new { domainName = entity.DomainName });
            if (exists > 0) throw new DuplicateNameException(string.Format("The domain name {0} is already assigned", entity.DomainName));

            if (entity.RootContentId.HasValue)
            {
                var contentExists = Database.ExecuteScalar<int>($"SELECT COUNT(*) FROM {Cms.Core.Constants.DatabaseSchema.Tables.Content} WHERE nodeId = @id", new { id = entity.RootContentId.Value });
                if (contentExists == 0) throw new NullReferenceException("No content exists with id " + entity.RootContentId.Value);
            }

            if (entity.LanguageId.HasValue)
            {
                var languageExists = Database.ExecuteScalar<int>("SELECT COUNT(*) FROM umbracoLanguage WHERE id = @id", new { id = entity.LanguageId.Value });
                if (languageExists == 0) throw new NullReferenceException("No language exists with id " + entity.LanguageId.Value);
            }

            entity.AddingEntity();

            // Get sort order
            entity.SortOrder = GetNewSortOrder(entity.RootContentId, entity.IsWildcard);

            var dto = DomainFactory.BuildDto(entity);

            var id = Convert.ToInt32(Database.Insert(dto));
            entity.Id = id;

            // If the language changed, we need to resolve the ISO code
            if (entity.LanguageId.HasValue)
            {
                ((UmbracoDomain)entity).LanguageIsoCode = Database.ExecuteScalar<string>("SELECT languageISOCode FROM umbracoLanguage WHERE id = @langId", new { langId = entity.LanguageId });
            }

            entity.ResetDirtyProperties();
        }

        protected override void PersistUpdatedItem(IDomain entity)
        {
            entity.UpdatingEntity();

            // Ensure there is no other domain with the same name on another entity
            var exists = Database.ExecuteScalar<int>("SELECT COUNT(*) FROM umbracoDomain WHERE domainName = @domainName AND umbracoDomain.id <> @id", new { domainName = entity.DomainName, id = entity.Id });
            if (exists > 0) throw new DuplicateNameException(string.Format("The domain name {0} is already assigned", entity.DomainName));

            if (entity.RootContentId.HasValue)
            {
                var contentExists = Database.ExecuteScalar<int>($"SELECT COUNT(*) FROM {Cms.Core.Constants.DatabaseSchema.Tables.Content} WHERE nodeId = @id", new { id = entity.RootContentId.Value });
                if (contentExists == 0) throw new NullReferenceException("No content exists with id " + entity.RootContentId.Value);
            }

            if (entity.LanguageId.HasValue)
            {
                var languageExists = Database.ExecuteScalar<int>("SELECT COUNT(*) FROM umbracoLanguage WHERE id = @id", new { id = entity.LanguageId.Value });
                if (languageExists == 0) throw new NullReferenceException("No language exists with id " + entity.LanguageId.Value);
            }

            var dto = DomainFactory.BuildDto(entity);

            Database.Update(dto);

            // If the language changed, we need to resolve the ISO code
            if (entity.WasPropertyDirty("LanguageId"))
            {
                ((UmbracoDomain)entity).LanguageIsoCode = Database.ExecuteScalar<string>("SELECT languageISOCode FROM umbracoLanguage WHERE id = @langId", new { langId = entity.LanguageId });
            }

            entity.ResetDirtyProperties();
        }

        protected int GetNewSortOrder(int? rootContentId, bool isWildcard)
            => isWildcard ? -1 : Database.ExecuteScalar<int>("SELECT COALESCE(MAX(sortOrder), -1) + 1 FROM umbracoDomain WHERE domainRootStructureID = @rootContentId AND NOT (LEN(domainName) = 0 OR CHARINDEX('*', domainName) = 1)", new { rootContentId });

        public IDomain GetByName(string domainName)
            => GetMany().FirstOrDefault(x => x.DomainName.InvariantEquals(domainName));

        public bool Exists(string domainName)
            => GetMany().Any(x => x.DomainName.InvariantEquals(domainName));

        public IEnumerable<IDomain> GetAll(bool includeWildcards)
            => GetMany().Where(x => includeWildcards || x.IsWildcard == false);

        public IEnumerable<IDomain> GetAssignedDomains(int contentId, bool includeWildcards)
            => GetMany().Where(x => x.RootContentId == contentId).Where(x => includeWildcards || x.IsWildcard == false);
    }
}
