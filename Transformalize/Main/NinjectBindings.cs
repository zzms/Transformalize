﻿using System.Collections.Generic;
using Transformalize.Libs.Ninject.Modules;
using Transformalize.Libs.Ninject.Syntax;
using Transformalize.Libs.SolrNet;
using Transformalize.Libs.SolrNet.Impl;
using Transformalize.Libs.SolrNet.Impl.DocumentPropertyVisitors;
using Transformalize.Libs.SolrNet.Impl.FacetQuerySerializers;
using Transformalize.Libs.SolrNet.Impl.FieldParsers;
using Transformalize.Libs.SolrNet.Impl.FieldSerializers;
using Transformalize.Libs.SolrNet.Impl.QuerySerializers;
using Transformalize.Libs.SolrNet.Impl.ResponseParsers;
using Transformalize.Libs.SolrNet.Mapping;
using Transformalize.Libs.SolrNet.Mapping.Validation;
using Transformalize.Libs.SolrNet.Mapping.Validation.Rules;
using Transformalize.Libs.SolrNet.Schema;
using Transformalize.Main.Providers;
using Transformalize.Main.Providers.AnalysisServices;
using Transformalize.Main.Providers.Console;
using Transformalize.Main.Providers.ElasticSearch;
using Transformalize.Main.Providers.File;
using Transformalize.Main.Providers.Folder;
using Transformalize.Main.Providers.Html;
using Transformalize.Main.Providers.Internal;
using Transformalize.Main.Providers.Log;
using Transformalize.Main.Providers.Mail;
using Transformalize.Main.Providers.MySql;
using Transformalize.Main.Providers.PostgreSql;
using Transformalize.Main.Providers.Solr;
using Transformalize.Main.Providers.SqlCe4;
using Transformalize.Main.Providers.SqlServer;

namespace Transformalize.Main {

    public class NinjectBindings : NinjectModule {

        public override void Load() {
            // databases
            Bind<AbstractConnectionDependencies>().To<SqlServerDependencies>().WhenInjectedInto<SqlServerConnection>();
            Bind<AbstractConnectionDependencies>().To<MySqlDependencies>().WhenInjectedInto<MySqlConnection>();
            Bind<AbstractConnectionDependencies>().To<PostgreSqlDependencies>().WhenInjectedInto<PostgreSqlConnection>();
            Bind<AbstractConnectionDependencies>().To<SqlCe4Dependencies>().WhenInjectedInto<SqlCe4Connection>();

            // others
            Bind<AbstractConnectionDependencies>().To<AnalysisServicesDependencies>().WhenInjectedInto<AnalysisServicesConnection>();
            Bind<AbstractConnectionDependencies>().To<FileDependencies>().WhenInjectedInto<FileConnection>();
            Bind<AbstractConnectionDependencies>().To<FolderDependencies>().WhenInjectedInto<FolderConnection>();
            Bind<AbstractConnectionDependencies>().To<InternalDependencies>().WhenInjectedInto<InternalConnection>();
            Bind<AbstractConnectionDependencies>().To<ConsoleDependencies>().WhenInjectedInto<ConsoleConnection>();
            Bind<AbstractConnectionDependencies>().To<LogDependencies>().WhenInjectedInto<LogConnection>();
            Bind<AbstractConnectionDependencies>().To<MailDependencies>().WhenInjectedInto<MailConnection>();
            Bind<AbstractConnectionDependencies>().To<HtmlDependencies>().WhenInjectedInto<HtmlConnection>();
            Bind<AbstractConnectionDependencies>().To<ElasticSearchDependencies>().WhenInjectedInto<ElasticSearchConnection>();
            Bind<AbstractConnectionDependencies>().To<SolrDependencies>().WhenInjectedInto<Providers.Solr.SolrConnection>();

            //solrnet
            var mapper = new MemoizingMappingManager(new AttributesMappingManager());
            Bind<IReadOnlyMappingManager>().ToConstant(mapper);
            //Bind<ISolrCache>().To<HttpRuntimeCache>();
            Bind<ISolrDocumentPropertyVisitor>().To<DefaultDocumentVisitor>();
            Bind<ISolrFieldParser>().To<DefaultFieldParser>();
            Bind(typeof(ISolrDocumentActivator<>)).To(typeof(SolrDocumentActivator<>));
            Bind(typeof(ISolrDocumentResponseParser<>)).To(typeof(SolrDocumentResponseParser<>));
            Bind<ISolrDocumentResponseParser<Dictionary<string, object>>>().To<SolrDictionaryDocumentResponseParser>();
            Bind<ISolrFieldSerializer>().To<DefaultFieldSerializer>();
            Bind<ISolrQuerySerializer>().To<DefaultQuerySerializer>();
            Bind<ISolrFacetQuerySerializer>().To<DefaultFacetQuerySerializer>();
            Bind(typeof(ISolrAbstractResponseParser<>)).To(typeof(DefaultResponseParser<>));
            Bind<ISolrHeaderResponseParser>().To<HeaderResponseParser<string>>();
            Bind<ISolrExtractResponseParser>().To<ExtractResponseParser>();
            foreach (var p in new[] {
                typeof(MappedPropertiesIsInSolrSchemaRule),
                typeof(RequiredFieldsAreMappedRule),
                typeof(UniqueKeyMatchesMappingRule),
                typeof(MultivaluedMappedToCollectionRule),
            })
                Bind<IValidationRule>().To(p);
            Bind(typeof(ISolrMoreLikeThisHandlerQueryResultsParser<>)).To(typeof(SolrMoreLikeThisHandlerQueryResultsParser<>));
            Bind(typeof(ISolrDocumentSerializer<>)).To(typeof(SolrDocumentSerializer<>));
            Bind(typeof(ISolrDocumentSerializer<Dictionary<string, object>>)).To(typeof(SolrDictionarySerializer));

            Bind<ISolrSchemaParser>().To<SolrSchemaParser>();
            Bind<ISolrDIHStatusParser>().To<SolrDIHStatusParser>();
            Bind<IMappingValidator>().To<MappingValidator>();
            Bind<ISolrStatusResponseParser>().To<SolrStatusResponseParser>();
            Bind<ISolrCoreAdmin>().To<SolrCoreAdmin>();
        }

    }
}
