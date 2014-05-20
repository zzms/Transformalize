using System.Collections.Generic;
using System.Threading;
using Transformalize.Libs.Rhino.Etl;
using Transformalize.Operations.Transform;

namespace Transformalize.Main {
    public class ElipseOperation : ShouldRunOperation {
        private readonly int _length;
        private readonly string _elipse;

        public ElipseOperation(string inKey, string outKey, int length, string elipse)
            : base(inKey, outKey) {
            _length = length;
            _elipse = elipse;
            Name = string.Format("Elipse ({0})", outKey);
        }

        public override IEnumerable<Row> Execute(IEnumerable<Row> rows) {
            foreach (var row in rows) {
                if (ShouldRun(row)) {
                    var value = row[InKey].ToString();
                    if (value.Length > _length) {
                        row[OutKey] = value.Substring(0, _length) + _elipse;
                    }
                } else {
                    Interlocked.Increment(ref SkipCount);
                }
                yield return row;
            }
        }
    }
}