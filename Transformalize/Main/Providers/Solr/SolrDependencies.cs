namespace Transformalize.Main.Providers.Solr {
    public class SolrDependencies : AbstractConnectionDependencies {
        public SolrDependencies()
            : base(
                new FalseTableQueryWriter(),
                new SolrConnectionChecker(),
                new SolrEntityRecordsExist(),
                new SolrEntityDropper(),
                new SolrEntityCreator(),
                new FalseViewWriter(),
                new SolrTflWriter(),
                new FalseScriptRunner()) { }
    }
}