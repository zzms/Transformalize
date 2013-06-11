﻿using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using RazorEngine.Text;

namespace Transformalize.Test {
    [TestFixture]
    public class TestRazor {

        internal class Person {
            public string Forename { get; set; }
        }

        /// <summary>
        /// Tests that a simple template without a model can be parsed.
        /// </summary>
        [Test]
        public void TemplateService_CanParseSimpleTemplate_WithNoModel()
        {
            using (var service = new TemplateService())
            {
                const string template = "<h1>Hello World</h1>";
                const string expected = template;

                string result = service.Parse(template, null, null, null);

                Assert.That(result == expected, "Result does not match expected: " + result);
            }
        }

        /// <summary>
        /// Tests that a simple template with a model can be parsed.
        /// </summary>
        [Test]
        public void TemplateService_CanParseSimpleTemplate_WithComplexModel()
        {
            using (var service = new TemplateService())
            {
                const string template = "<h1>Hello @Model.Forename</h1>";
                const string expected = "<h1>Hello Matt</h1>";

                var model = new { Forename = "Matt" };
                string result = service.Parse(template, model, null, null);

                Assert.That(result == expected, "Result does not match expected: " + result);
            }
        }

        /// <summary>
        /// Tests that a simple template with an anonymous model can be parsed.
        /// </summary>
        [Test]
        public void TemplateService_CanParseSimpleTemplate_WithAnonymousModel()
        {
            using (var service = new TemplateService())
            {
                const string template = "<h1>Hello @Model.Forename</h1>";
                const string expected = "<h1>Hello Matt</h1>";

                var model = new { Forename = "Matt" };
                string result = service.Parse(template, model, null, null);

                Assert.That(result == expected, "Result does not match expected: " + result);
            }
        }

        /// <summary>
        /// Tests that a simple template with a expando model can be parsed.
        /// </summary>
        [Test]
        public void TemplateService_CanParseSimpleTemplate_WithExpandoModel()
        {
            using (var service = new TemplateService())
            {
                const string template = "<h1>Hello @Model.Forename</h1>";
                const string expected = "<h1>Hello Matt</h1>";

                dynamic model = new ExpandoObject();
                model.Forename = "Matt";

                string result = service.Parse(template, model, null, null);

                Assert.That(result == expected, "Result does not match expected: " + result);
            }
        }

        /// <summary>
        /// Tests that a simple template with a dynamic model can be parsed.
        /// </summary>
        [Test]
        public void TemplateService_CanParseSimpleTemplate_WithDynamicModel()
        {
            using (var service = new TemplateService())
            {
                const string template = "<h1>Hello @Model.Forename</h1>";
                const string expected = "<h1>Hello Matt</h1>";

                var model = new { Forename= "Matt" };
                string result = service.Parse(template, model, null, null);

                Assert.That(result == expected, "Result does not match expected: " + result);
            }
        }

        /// <summary>
        /// Tests that a simple template with an iterator model can be parsed.
        /// </summary>
        [Test]
        public void TemplateService_CanParseSimpleTemplate_WithIteratorModel()
        {
            using (var service = new TemplateService())
            {
                const string template = "@foreach (var i in Model) { @i }";
                const string expected = "One Two Three";

                var model = CreateIterator("One ", "Two ", "Three");
                string result = service.Parse(template, model, null, null);

                Assert.That(result == expected, "Result does not match expected: " + result);
            }
        }

        private static IEnumerable<T> CreateIterator<T>(params T[] items)
        {
            foreach (var item in items) yield return item;
        }

        /// <summary>
        /// Tests that a simple template with html-encoding can be parsed.
        /// </summary>
        /// <remarks>
        /// Text encoding is performed when writing objects to the template result (not literals). This test should 
        /// show that the template service is correctly providing the appropriate encoding factory to process
        /// the object's .ToString() and automatically encode it.
        /// </remarks>
        [Test]
        public void TemplateService_CanParseSimpleTemplate_UsingHtmlEncoding()
        {

            using (var service = new TemplateService())
            {
                const string template = "<h1>Hello @Model.String</h1>";
                const string expected = "<h1>Hello Matt &amp; World</h1>";

                var model = new { String = "Matt & World" };
                string result = service.Parse(template, model, null, null);

                Assert.That(result == expected, "Result does not match expected: " + result);
            }
        }

        /// <summary>
        /// Tests that a simple template with no-encoding can be parsed.
        /// </summary>
        /// <remarks>
        /// Text encoding is performed when writing objects to the template result (not literals). This test should 
        /// show that the template service is correctly providing the appropriate encoding factory to process
        /// the object's .ToString() and automatically encode it.
        /// </remarks>
        [Test]
        public void TemplateService_CanParseSimpleTemplate_UsingRawEncoding()
        {
            var config = new TemplateServiceConfiguration()
            {
                EncodedStringFactory = new RawStringFactory()
            };

            using (var service = new TemplateService(config))
            {
                const string template = "<h1>Hello @Model.String</h1>";
                const string expected = "<h1>Hello Matt & World</h1>";

                var model = new { String = "Matt & World" };
                string result = service.Parse(template, model, null, null);

                Assert.That(result == expected, "Result does not match expected: " + result);
            }
        }

        /// <summary>
        /// Tests that the template service can parse multiple templates in sequence.
        /// </summary>
        [Test]
        public void TemplateService_CanParseMultipleTemplatesInSequence_WitNoModels()
        {
            using (var service = new TemplateService())
            {
                const string template = "<h1>Hello World</h1>";
                var templates = Enumerable.Repeat(template, 10);

                var results = service.ParseMany(templates, null, null, null, false);

                Assert.That(templates.SequenceEqual(results), "Rendered templates do not match expected.");
            }
        }

        /// <summary>
        /// Tests that the template service can parse multiple templates in parallel.
        /// </summary>
        [Test]
        public void TemplateService_CanParseMultipleTemplatesInParallel_WitNoModels()
        {
            using (var service = new TemplateService())
            {
                const string template = "<h1>Hello World</h1>";
                var templates = Enumerable.Repeat(template, 10);

                var results = service.ParseMany(templates, null, null, null, true);

                Assert.That(templates.SequenceEqual(results), "Rendered templates do not match expected."); 
            }
        }

        /// <summary>
        /// Tests that the template service can parse multiple templates in sequence with complex models.
        /// </summary>
        [Test]
        public void TemplateService_CanParseMultipleTemplatesInSequence_WithComplexModels()
        {
            const int maxTemplates = 10;

            using (var service = new TemplateService())
            {
                const string template = "<h1>Age: @Model.Age</h1>";
                var expected = Enumerable.Range(1, maxTemplates).Select(i => string.Format("<h1>Age: {0}</h1>", i));
                var templates = Enumerable.Repeat(template, maxTemplates);
                var models = Enumerable.Range(1, maxTemplates).Select(i => new { Age = i });

                var results = service.ParseMany(templates, models, null, null, false);
                Assert.That(expected.SequenceEqual(results), "Parsed templates do not match expected results.");
            }
        }

        /// <summary>
        /// Tests that the template service can parse multiple templates in parallel with complex models.
        /// </summary>
        [Test]
        public void TemplateService_CanParseMultipleTemplatesInParallel_WithComplexModels()
        {
            const int maxTemplates = 10;

            using (var service = new TemplateService())
            {
                const string template = "<h1>Age: @Model.Age</h1>";
                var expected = Enumerable.Range(1, maxTemplates).Select(i => string.Format("<h1>Age: {0}</h1>", i));
                var templates = Enumerable.Repeat(template, maxTemplates);
                var models = Enumerable.Range(1, maxTemplates).Select(i => new { Age = i });

                var results = service.ParseMany(templates, models, null, null, true);
                Assert.That(expected.SequenceEqual(results), "Parsed templates do not match expected results.");
            }
        }

        /// <summary>
        /// Tests that the template service can parse templates when using a manual threading model (i.e. manually creating <see cref="Thread"/>
        /// instances and maintaining their lifetime.
        /// </summary>
        [Test]
        public void TemplateService_CanParseTemplatesInParallel_WithManualThreadModel()
        {
            var service = new TemplateService();

            const int threadCount = 10;
            const string template = "<h1>Hello you are @Model.Age</h1>";

            var threads = new List<Thread>();
            for (int i = 0; i < threadCount; i++)
            {
                // Capture enumerating index here to avoid closure issues.
                int index = i;

                var thread = new Thread(() =>
                {
                    var model = new { Age = index };
                    string expected = "<h1>Hello you are " + index + "</h1>";
                    string result = service.Parse(template, model, null, null);

                    Assert.That(result == expected, "Result does not match expected: " + result);
                });

                threads.Add(thread);
                thread.Start();
            }

            // Block until all threads have joined.
            threads.ForEach(t => t.Join());

            service.Dispose();
        }

        /// <summary>
        /// Tests that a template service can precompile a template for later execution.
        /// </summary>
        [Test]
        public void TemplateService_CanPrecompileTemplate_WithNoModel()
        {
            using (var service = new TemplateService())
            {
                const string template = "Hello World";
                const string expected = "Hello World";

                service.Compile(template, null, "test");

                string result = service.Run("test", null, null);

                Assert.That(result == expected, "Result does not match expected.");
            }
        }

        /// <summary>
        /// Tests that a simple helper template with html-encoding can be parsed.
        /// </summary>
        [Test]
        public void TemplateService_CanParseSimpleHelperTemplate_UsingHtmlEncoding()
        {
            using (var service = new TemplateService())
            {
                const string template = "<h1>Hello @NameHelper()</h1>@helper NameHelper() { @Model.String }";
                const string expected = "<h1>Hello Matt &amp; World</h1>";

                var model = new { String = "Matt & World" };
                string result = service.Parse(template, model, null, null);

                Assert.That(result == expected, "Result does not match expected: " + result);
            }
        }

        /// <summary>
        /// Tests that a simple helper template with no-encoding can be parsed.
        /// </summary>
        [Test]
        public void TemplateService_CanParseSimpleHelperTemplate_UsingRawEncoding()
        {
            var config = new TemplateServiceConfiguration()
            {
                EncodedStringFactory = new RawStringFactory()
            };

            using (var service = new TemplateService(config))
            {
                const string template = "<h1>Hello @NameHelper()</h1>@helper NameHelper() { @Model.String }";
                const string expected = "<h1>Hello Matt & World</h1>";

                var model = new { String = "Matt & World" };
                string result = service.Parse(template, model, null, null);

                Assert.That(result == expected, "Result does not match expected: " + result);
            }
        }
        
    }
}