using System;
using System.IO;
using Transformalize.Configuration;
using Transformalize.Libs.Rhino.Etl.Operations;
using Transformalize.Operations.Load;
using Transformalize.Operations.Transform;

namespace Transformalize.Main.Providers.File {

    public class FileConnection : AbstractConnection {

        public override string UserProperty { get { return string.Empty; } }
        public override string PasswordProperty { get { return string.Empty; } }
        public override string PortProperty { get { return string.Empty; } }
        public override string DatabaseProperty { get { return string.Empty; } }
        public override string ServerProperty { get { return string.Empty; } }
        public override string TrustedProperty { get { return string.Empty; } }
        public override string PersistSecurityInfoProperty { get { return string.Empty; } }
        public override int NextBatchId(string processName) {
            return 1;
        }

        public override void WriteEndVersion(AbstractConnection input, Entity entity, bool force = false) {
            //do nothing
        }

        public override IOperation EntityOutputKeysExtract(Entity entity) {
            return new EmptyOperation();
        }

        public override IOperation EntityOutputKeysExtractAll(Entity entity) {
            return new EmptyOperation();
        }

        public override IOperation EntityBulkLoad(Entity entity) {
            return new FileLoadOperation(this, entity);
        }

        public override IOperation EntityBatchUpdate(Entity entity) {
            return new EmptyOperation();
        }

        public override void LoadBeginVersion(Entity entity) {
            throw new System.NotImplementedException();
        }

        public override void LoadEndVersion(Entity entity) {
            throw new System.NotImplementedException();
        }

        public override EntitySchema GetEntitySchema(string name, string schema = "", bool isMaster = false) {
            var entitySchema = new EntitySchema();
            var fileFields = new FieldInspector().Inspect(File);
            foreach (var fileField in fileFields) {
                var field = new Field(fileField.Type, fileField.Length, FieldType.NonKey, true, string.Empty) {
                    Name = fileField.Name,
                    QuotedWith = fileField.QuoteString()
                };
                entitySchema.Fields.Add(field);
            }
            return entitySchema;
        }

        public FileConnection(ConnectionConfigurationElement element, AbstractConnectionDependencies dependencies)
            : base(element, dependencies) {
            Type = ProviderType.File;
        }
    }
}