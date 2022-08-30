﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace HSMServer.Core.Model
{
    public readonly struct ValidationResult
    {
        internal static ValidationResult Ok { get; } = new();


        private HashSet<string> Messages { get; init; } = new();

        private HashSet<string> Warnings { get; init; } = new();

        private HashSet<string> Errors { get; init; } = new();


        public SensorStatus Result
        {
            get
            {
                if (Messages.Count != 0)
                    return SensorStatus.Unknown;
                else if (Errors.Count != 0)
                    return SensorStatus.Error;
                else if (Warnings.Count != 0)
                    return SensorStatus.Warning;

                return SensorStatus.Ok;
            }
        }


        public string Message
        {
            get
            {
                var messageParts = new List<string>(3)
                {
                    JoinStrings(Messages),
                    JoinStrings(Warnings),
                    JoinStrings(Errors),
                };

                return JoinStrings(messageParts);
            }
        }


        public bool IsSuccess => Result == SensorStatus.Ok;

        public bool IsWarning => Warnings.Count > 0;

        public bool IsError => Errors.Count > 0;


        public ValidationResult()
        {
            Messages = new HashSet<string>();
            Warnings = new HashSet<string>();
            Errors = new HashSet<string>();
        }

        internal ValidationResult(string message, SensorStatus result) : this()
        {
            switch (result)
            {
                case SensorStatus.Warning:
                    Warnings.Add(message);
                    break;
                case SensorStatus.Error:
                    Errors.Add(message);
                    break;
                default:
                    Messages.Add(message);
                    break;
            }
        }


        private static string JoinStrings(IEnumerable<string> items)
        {
            return string.Join(Environment.NewLine, items.Where(u => !string.IsNullOrEmpty(u)));
        }

        public static ValidationResult operator +(ValidationResult result1, ValidationResult result2)
        {
            static HashSet<string> GetUnionErrors(HashSet<string> errors1, HashSet<string> errors2)
            {
                var errors = new HashSet<string>(errors1);
                errors.UnionWith(errors2);

                return errors;
            }

            return new()
            {
                Messages = GetUnionErrors(result1.Messages, result2.Messages),
                Warnings = GetUnionErrors(result1.Warnings, result2.Warnings),
                Errors = GetUnionErrors(result1.Errors, result2.Errors),
            };
        }

        public static ValidationResult operator -(ValidationResult result1, ValidationResult result2)
        {
            static HashSet<string> GetExceptErrors(HashSet<string> errors1, HashSet<string> errors2)
            {
                var errors = new HashSet<string>(errors1);
                errors.ExceptWith(errors2);

                return errors;
            }

            return new()
            {
                Warnings = GetExceptErrors(result1.Warnings, result2.Warnings),
                Errors = GetExceptErrors(result1.Errors, result2.Errors),
                Messages = GetExceptErrors(result1.Messages, result2.Messages),
            };
        }

        public static bool operator ==(ValidationResult result1, ValidationResult result2) =>
            result1.Message == result2.Message && result1.Result == result2.Result;

        public static bool operator !=(ValidationResult result1, ValidationResult result2)
            => !(result1 == result2);
    }
}
