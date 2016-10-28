﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Humanizer;
using Pipeline.Configuration;
using Pipeline.Contracts;
using Pipeline.Extensions;
using Pipeline.Transforms;

namespace Pipeline.Transform.Humanizer {
    public class FromRomanTransform : BaseTransform {
        private static readonly Regex ValidRomanNumeral = new Regex("^(?i:(?=[MDCLXVI])((M{0,3})((C[DM])|(D?C{0,3}))?((X[LC])|(L?XX{0,2})|L)?((I[VX])|(V?(II{0,2}))|V)?))$", RegexOptions.Compiled);
        private readonly Func<IRow, object> _transform;
        private readonly Field _input;
        private readonly HashSet<string> _warnings = new HashSet<string>();

        public FromRomanTransform(IContext context) : base(context, context.Field.Type) {
            _input = SingleInput();
            switch (_input.Type) {
                case "string":
                    _transform = (row) => {
                        var input = ((string)row[_input]).Trim();

                        if (input.Length == 0 || IsInvalidRomanNumeral(input)) {
                            var warning = $"The input {input} is an invalid roman numeral";
                            if (_warnings.Add(warning)) {
                                context.Warn(warning);
                            }
                            return Context.Field.Convert("0");
                        }
                        return Context.Field.Convert(input.FromRoman());
                    };
                    break;
                default:
                    _transform = (row) => row[_input];
                    break;
            }
        }

        public override IRow Transform(IRow row) {
            row[Context.Field] = _transform(row);
            Increment();
            return row;
        }


        // The following code is from Humanizer
        // Humanizer is by Alois de Gouvello https://github.com/aloisdg
        // The MIT License (MIT)
        // Copyright (c) 2015 Alois de Gouvello

        private static bool IsInvalidRomanNumeral(string input) {
            return !ValidRomanNumeral.IsMatch(input);
        }

    }
}