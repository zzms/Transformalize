﻿#region License

// /*
// Transformalize - Replicate, Transform, and Denormalize Your Data...
// Copyright (C) 2013 Dale Newman
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// */

#endregion

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Transformalize.Main;
using Transformalize.Operations.Transform;

namespace Transformalize.Test.Unit {
    [TestFixture]
    public class TestTransforms : EtlProcessHelper {
        [Test]
        public void ConcatStrings() {
            var input = new RowsBuilder().Row().Field("f1", "v1").Field("f2", "v2").ToOperation();
            var parameters = new ParametersBuilder().Parameters("f1", "f2").ToParameters();
            var concat = new ConcatOperation("o1", parameters);

            var rows = TestOperation(input, concat);

            Assert.AreEqual("v1v2", rows[0]["o1"]);
        }

        [Test]
        public void ConcatNumbers() {
            var input = new RowsBuilder().Row().Field("f1", 1).Field("f2", 2).ToOperation();
            var parameters = new ParametersBuilder().Parameter("f1").Type("int32").Parameter("f2").Type("int32").ToParameters();
            var concat = new ConcatOperation("o1", parameters);

            var rows = TestOperation(input, concat);

            Assert.AreEqual("12", rows[0]["o1"]);
        }

        [Test]
        public void ConvertStringToNumber() {
            var input = new RowsBuilder().Row().Field("f1", "1").ToOperation();
            var convert = new ConvertOperation("f1", "string", "o1", "int32");

            var rows = TestOperation(input, convert);

            Assert.AreEqual(1, rows[0]["o1"]);
        }

        [Test]
        public void ConvertStringToDateTime() {
            var input = new RowsBuilder().Row().Field("f1", "2013-11-07").ToOperation();
            var convert = new ConvertOperation("f1", "string", "o1", "datetime", "yyyy-MM-dd");

            var rows = TestOperation(input, convert);

            Assert.AreEqual(new DateTime(2013, 11, 7), rows[0]["o1"]);
        }

        [Test]
        public void Distance() {

            var input = new RowsBuilder().Row().Field("toLat", 28.419385d).Field("toLong", -81.581234d).ToOperation();
            var parameters = new ParametersBuilder()
                .Parameter("fromLat").Type("double").Value(42.101025d).Parameter("fromLong").Type("double").Value(-86.48423d)
                .Parameter("toLat").Type("double").Parameter("toLong").Type("double")
                .ToParameters();

            var distance = new DistanceOperation("o1", "miles", parameters);

            var rows = TestOperation(input, distance);

            Assert.AreEqual(985.32863773508757d, rows[0]["o1"]);
        }

        [Test]
        public void ExpressionFunction() {
            var input = new RowsBuilder().Row().Field("f1", 4).ToOperation();
            var parameters = new ParametersBuilder().Parameter("f1").ToParameters();
            var expression = new ExpressionOperation("o1", "Sqrt(f1)", parameters);

            var rows = TestOperation(input, expression);

            Assert.AreEqual(2d, rows[0]["o1"]);
        }

        [Test]
        public void ExpressionIf() {
            var input = new RowsBuilder().Row().Field("f1", 4).ToOperation();
            var parameters = new ParametersBuilder().Parameter("f1").ToParameters();
            var expression = new ExpressionOperation("o1", "if(f1 = 4, true, false)", parameters);

            var rows = TestOperation(input, expression);

            Assert.AreEqual(true, rows[0]["o1"]);
        }

        [Test]
        public void Format() {
            var input = new RowsBuilder().Row().Field("f1", true).Field("f2", 8).ToOperation();
            var parameters = new ParametersBuilder().Parameters("f1", "f2").ToParameters();
            var expression = new FormatOperation("o1", "{0} and {1}.", parameters);

            var rows = TestOperation(input, expression);

            Assert.AreEqual("True and 8.", rows[0]["o1"]);
        }

        [Test]
        public void FromJson() {
            var input = new RowsBuilder().Row().Field("f1", "{ \"j1\":\"v1\", \"j2\":7, \"array\":[{\"x\":1}] }").ToOperation();
            var outParameters = new ParametersBuilder().Parameter("j1").Parameter("j2").Type("int32").Parameter("array").ToParameters();
            var expression = new FromJsonOperation("f1", false, false, outParameters);

            var rows = TestOperation(input, expression);

            Assert.AreEqual("v1", rows[0]["j1"]);
            Assert.AreEqual(7, rows[0]["j2"]);
        }

        [Test]
        public void FromJsonWithExtraDoubleQuotes() {
            var input = new RowsBuilder().Row().Field("f1", "{ \"j1\":\"v\"1\", \"j2\"\":7 }").ToOperation();
            var outParameters = new ParametersBuilder().Parameter("j1").Parameter("j2").Type("int32").ToParameters();
            var expression = new FromJsonOperation("f1", true, false, outParameters);

            var rows = TestOperation(input, expression);

            Assert.AreEqual("v1", rows[0]["j1"]);
            Assert.AreEqual(7, rows[0]["j2"]);
        }

        [Test]
        public void FromRegex() {
            var input = new RowsBuilder().Row().Field("f1", "991.1 #Something INFO and a rambling message.").ToOperation();
            var outParameters = new ParametersBuilder().Parameter("p1").Type("decimal").Parameter("p2").Parameter("p3").ToParameters();
            var fromRegex = new FromRegexOperation("f1", @"(?<p1>^[\d\.]+).*(?<p2> [A-Z]{4,5} )(?<p3>.*$)", outParameters);

            var rows = TestOperation(input, fromRegex);

            Assert.AreEqual(991.1M, rows[0]["p1"]);
            Assert.AreEqual(" INFO ", rows[0]["p2"]);
            Assert.AreEqual("and a rambling message.", rows[0]["p3"]);
        }

        [Test]
        public void FromXml() {
            var input = new RowsBuilder().Row().Field("f1", "<order><id>1</id><total>7.25</total><lines><line product=\"1\"/><line product=\"2\"/></lines></order>").ToOperation();

            var outFields = new FieldsBuilder()
                .Field("id").Type("int32")
                .Field("total").Type("decimal")
                .Field("lines")
                .ToFields();
            
            var fromXml = new FromXmlOperation("f1", outFields);

            var rows = TestOperation(input, fromXml);

            Assert.AreEqual(1, rows[0]["id"]);
            Assert.AreEqual(7.25M, rows[0]["total"]);
            Assert.AreEqual("<line product=\"1\" /><line product=\"2\" />", rows[0]["lines"]);
        }

        //[Test]
        //public void FromDeeperXml() {
        //    var input = new RowsBuilder().Row().Field("f1", "<order><id>1</id><total>7.25</total><lines><line>1</line><line>2</line></lines></order>").ToOperation();
        //    var outParameters = new ParametersBuilder().Parameter("id").Type("int32").Parameter("total").Type("decimal").Parameter("lines").ToParameters();
        //    var fromXml = new FromXmlOperation("f1", outParameters);

        //    var outParametersDeeper = new ParametersBuilder().Parameter("line").ToParameters();
        //    var format = new FormatOperation("lines", "<lines>{0}</lines>", new ParametersBuilder().Parameter("lines").ToParameters());
        //    var fromXmlDeeper = new FromXmlOperation("lines", outParametersDeeper);


        //    var rows = TestOperation(input, fromXml, format, fromXmlDeeper);

        //    Assert.AreEqual(1, rows[0]["id"]);
        //    Assert.AreEqual(7.25M, rows[0]["total"]);
        //    Assert.AreEqual("<line product=\"1\" /><line product=\"2\" />", rows[0]["lines"]);
        //}

        [Test]
        public void GetHashCodeTest() {
            var expected = "test".GetHashCode();

            var input = new RowsBuilder().Row("f1", "test").ToOperation();
            var getHashCode = new GetHashCodeOperation("f1", "o1");
            var output = TestOperation(input, getHashCode);

            Assert.AreEqual(expected, output[0]["o1"]);
        }

        [Test]
        public void Insert() {
            const string expected = "InsertHere";

            var input = new RowsBuilder().Row("f1", "Insertere").ToOperation();
            var insert = new InsertOperation("f1", "o1", 6, "H");
            var output = TestOperation(input, insert);

            Assert.AreEqual(expected, output[0]["o1"]);
        }

        [Test]
        public void Javascript() {
            const int expected = 12;

            var input = new RowsBuilder().Row("x", 3).Field("y", 4).ToOperation();
            var scripts = new Dictionary<string, Script>() { { "script", new Script("script", "function multiply(x,y) { return x*y; }", "") } };
            var parameters = new ParametersBuilder().Parameters("x", "y").ToParameters();
            var javascript = new JavascriptOperation("o1", "multiply(x,y)", scripts, parameters);
            var output = TestOperation(input, javascript);

            Assert.AreEqual(expected, output[0]["o1"]);
        }

        [Test]
        public void Join() {
            var input = new RowsBuilder().Row("x", "X").Field("y", "Y").ToOperation();
            var parameters = new ParametersBuilder().Parameters("x", "y").ToParameters();
            var join = new JoinOperation("o1", "|", parameters);
            var output = TestOperation(input, join);
            Assert.AreEqual("X|Y", output[0]["o1"]);
        }

        [Test]
        public void Left() {
            var input = new RowsBuilder().Row("left", "left").ToOperation();
            var parameters = new ParametersBuilder().Parameters("left").ToParameters();
            var left = new LeftOperation("left", "o1", 3);
            var output = TestOperation(input, left);
            Assert.AreEqual("lef", output[0]["o1"]);
        }

        [Test]
        public void Length() {
            var input = new RowsBuilder().Row("left", "left").ToOperation();
            var parameters = new ParametersBuilder().Parameters("left").ToParameters();
            var length = new LengthOperation("left", "o1");
            var output = TestOperation(input, length);
            Assert.AreEqual(4, output[0]["o1"]);
        }

        [Test]
        public void MapEquals() {
            var input = new RowsBuilder().Row("f1", "x").Row("f1", "a").Row("f1", "d").ToOperation();
            var maps = new MapsBuilder()
                .Equals().Item("x", "y").Item("a", "b")
                .StartsWith()
                .EndsWith().ToMaps();
            var map = new MapOperation("f1", "o1", "string", maps);
            var output = TestOperation(input, map);
            Assert.AreEqual("y", output[0]["o1"], "x maps to y");
            Assert.AreEqual("b", output[1]["o1"], "a maps to b");
            Assert.AreEqual("d", output[2]["o1"], "d stays d");
        }

        [Test]
        public void MapStartsWith() {
            var input = new RowsBuilder().Row("f1", "test1").Row("f1", "test2").Row("f1", "tes").ToOperation();
            var maps = new MapsBuilder()
                .Equals().Item("*", "no")
                .StartsWith().Item("test", "yes")
                .EndsWith().ToMaps();
            var map = new MapOperation("f1", "o1", "string", maps);
            var output = TestOperation(input, map);
            Assert.AreEqual("yes", output[0]["o1"], "test1 maps to yes");
            Assert.AreEqual("yes", output[1]["o1"], "test2 maps to yes");
            Assert.AreEqual("no", output[2]["o1"], "test maps to no (via catch-all)");
        }

        [Test]
        public void MapEndsWith() {
            var input = new RowsBuilder()
                .Row("f1", "1end")
                .Row("f1", "2end")
                .Row("f1", "start").ToOperation();
            var maps = new MapsBuilder()
                .Equals().Item("*", "no")
                .StartsWith()
                .EndsWith().Item("end", "yes").ToMaps();
            var mapTransform = new MapOperation("f1", "o1", "string", maps);
            var output = TestOperation(input, mapTransform);

            Assert.AreEqual("yes", output[0]["o1"]);
            Assert.AreEqual("yes", output[1]["o1"]);
            Assert.AreEqual("no", output[2]["o1"]);

        }

        [Test]
        public void MapEqualsWithParameter() {
            
            var input = new RowsBuilder()
                .Row("f1", "v1").Field("p1", 1).Field("p2", 2).Field("p3", 3)
                .Row("f1", "v2").Field("p1", 1).Field("p2", 2).Field("p3", 3)
                .Row("f1", "v3").Field("p1", 1).Field("p2", 2).Field("p3", 3)
                .ToOperation();

            var parameters = new ParametersBuilder()
                .Parameters("p1","p2","p3")
                .ToParameters();

            var maps = new MapsBuilder()
                .Equals().Item("v1", null, "p1").Item("v2", null, "p2").Item("*", null, "p3")
                .StartsWith()
                .EndsWith()
                .ToMaps();

            var mapTransform = new MapOperation("f1", "o1", "int32", maps);
            
            var output = TestOperation(input, mapTransform);

            Assert.AreEqual(1, output[0]["o1"], "v1 maps to 1");
            Assert.AreEqual(2, output[1]["o1"], "v2 maps to 2");
            Assert.AreEqual(3, output[2]["o1"], "v3 maps to 3 (via catch-all)");
        }

        [Test]
        public void FromXmlWithMultipleRecords() {

            const string xml = @"
                <order>
                    <detail product-id=""1"" quantity=""2"" color=""red"" />
                    <detail product-id=""3"" quantity=""1"" color=""pink"" />
                </order>
            ";
            
            var input = new RowsBuilder().Row().Field("xml", xml).ToOperation();

            var outFields = new FieldsBuilder()
                .Field("product-id").Type("int32").NodeType("attribute")
                .Field("quantity").Type("int32").NodeType("attribute")
                .Field("color").Type("string").NodeType("attribute")
                .ToFields();

            var fromXmlTransform = new FromXmlOperation("xml", outFields);

            var output = TestOperation(input, fromXmlTransform);

            Assert.AreEqual(1, output[0]["product-id"]);
            Assert.AreEqual(2, output[0]["quantity"]);
            Assert.AreEqual("red", output[0]["color"]);

            Assert.AreEqual(3, output[1]["product-id"]);
            Assert.AreEqual(1, output[1]["quantity"]);
            Assert.AreEqual("pink", output[1]["color"]);

        }



    }
}