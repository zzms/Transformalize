using System.Collections.Generic;
using System.Globalization;
using Transformalize.Libs.Rhino.Etl;

namespace Transformalize.Operations.Transform {
    public class ToTitleCaseOperation : TflOperation {
        private readonly TextInfo _textInfo;

        public ToTitleCaseOperation(string inKey, string outKey)
            : base(inKey, outKey) {
            _textInfo = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo;
        }

        public override IEnumerable<Row> Execute(IEnumerable<Row> rows) {
            foreach (var row in rows) {
                if (ShouldRun(row)) {
                    row[OutKey] = _textInfo.ToTitleCase(row[InKey].ToString().ToLower());
                }
                yield return row;
            }
        }
    }
}